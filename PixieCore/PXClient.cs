﻿using DryIoc;
using Newtonsoft.Json.Linq;
using Pixie.Core.DependencyInjection;
using Pixie.Core.Messages;
using Pixie.Core.Middlewares;
using Pixie.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Pixie.Core {
    class PXClient : IPXClientService {
        public string Id { get; private set; }
        public PXMessageReader StreamReader { get; private set; }
        public PXMessageWriter StreamWriter { get; private set; }

        private NetworkStream netStream;
        private TcpClient client;
        private Container container;
        private Dictionary<Type, Type> messageHandlerMap = new Dictionary<Type, Type>();

        public event Action<PXClient> OnDisconnect;

        public bool IsClosed { get; private set; }

        public PXClient(TcpClient tcpClient, Container container, Type[] messageHandlerTypes) {
            Id = Guid.NewGuid().ToString();
            client = tcpClient;
            netStream = client.GetStream();

            foreach (var t in messageHandlerTypes) {
                messageHandlerMap[t.GetProperty(
                    PXMessageInfo.MESSAGE_CLASS_FIELD_DATA_TYPE, 
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
                ).GetValue(null) as Type] = t;
            }

            StreamReader = new PXMessageReader(netStream, messageHandlerMap.Keys.ToArray());
            StreamWriter = new PXMessageWriter(netStream);

            this.container = container;

            StreamReader.OnDataAvailable += delegate (PXMessageReader reader) {
                Process();
            };

            StreamReader.OnStreamClose += delegate {
                Close();
            };

            StreamReader.OnStreamError += delegate (PXMessageReader r, Exception e) {
                container.Logger().Exception(e);
            };
        }

        public void Start() {
            StreamReader.StartReadingCycle();
        }

        public void Process() {
            try {
                if (StreamReader.HasMessages) {
                    this.container.Middlewares().HandleOverMiddlewares(
                        delegate(IContainer ctr) { CreateMessageHandler(StreamReader.DequeueMessage()).Handle(ctr); },
                        this.CreateMessageContainer(),
                        PXMiddlewareService.Type.Message
                    );
                }
            } catch (Exception e) {
                this.container.Logger().Exception(e);

                Close();
            }
        }

        public void Send(object message) {
            this.StreamWriter.Send(message);
        }

        protected internal void Close() {
            netStream.Close();
            client.Close();

            IsClosed = true;

            OnDisconnect?.Invoke(this);
            OnDisconnect = null;
        }

        private PXMessageHandlerRaw CreateMessageHandler(object message) {
            var handler = (PXMessageHandlerRaw)Activator.CreateInstance(messageHandlerMap[message.GetType()]);
            handler.SetupData(message);
            return handler;
        }

        private IContainer CreateMessageContainer() {
            var commandContainer = this.container.CreateFacade();

            commandContainer.Use<IPXClientService>(this);

            return commandContainer;
        }
    }
}
