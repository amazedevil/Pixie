﻿using DryIoc;
using Pixie.Core.Messages;
using Pixie.Core.Services;
using Pixie.Core.Sockets;
using Pixie.Core.StreamWrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;

namespace Pixie.Core
{
    internal class PXSocketClient : IPXClientService, IPXProtocolContact
    {
        public string Id { get; private set; }

        private PXMessageEncoder encoder;
        private IPXProtocol protocol;

        private TcpClient client;
        private Func<TcpClient> clientFactory;
        private IEnumerable<IPXStreamWrapper> wrappers;

        private IResolverContext context;
        private HashSet<int> subscriptions = new HashSet<int>();

        public event Action<PXSocketClient> OnDisconnect;

        public PXSocketClient(TcpClient client, IResolverContext context, IEnumerable<IPXStreamWrapper> wrappers, IPXProtocol protocol, Func<TcpClient> clientFactory = null) {
            Id = Guid.NewGuid().ToString();
            this.client = client;
            this.clientFactory = clientFactory;
            this.wrappers = wrappers.ToArray();
            this.protocol = protocol;
            protocol.Initialize(this);

            this.encoder = new PXMessageEncoder(context.Handlers().GetHandlableMessageTypes());
            this.context = context;
        }

        public void Start() {
            SetupProtocol();
        }

        public void Stop() {
            Close();
        }

        public void Send(object message) {
            this.protocol.SendMessage(this.encoder.EncodeMessage(message));
        }

        private void SetupProtocol() {
            if (clientFactory != null) {
                client?.Close();
                client = clientFactory();
            }

            this.protocol.SetupStreams(WrapStream(client.GetStream(), this.wrappers));
        }

        private Stream WrapStream(Stream stream, IEnumerable<IPXStreamWrapper> wrappers) {
            var result = stream;

            foreach (var wrapper in wrappers) {
                result = wrapper.Wrap(result);
            }

            return result;
        }

        private void OnClientError(Exception e) {
            this.context.Errors().Handle(e, PXErrorHandlingService.Scope.SocketClient);
        }

        //TODO: this method looks like it should be private, handle it
        internal bool IsSubscribed(int subscriptionId) {
            return subscriptions.Contains(subscriptionId);
        }

        private void ProcessClosingMessage() {
            this.context.Handlers().HandleSpecialMessage(
                PXHandlerService.SpecificMessageHandlerType.ClientDisconnect,
                delegate(Action<IResolverContext> handler) {
                    this.ExecuteInMessageScope(delegate (IResolverContext messageContext) {
                        handler(messageContext);
                    });
                }
            );
        }

        private void Close() {
            client.Close();

            OnDisconnect?.Invoke(this);
            OnDisconnect = null;
        }

        private void ExecuteInMessageScope(Action<IResolverContext> action) {
            using (var messageContext = this.context.OpenScope()) {
                messageContext.Use<IPXClientService>(this);

                action(messageContext);
            }
        }

        //IPXClientService

        public void Subscribe(int subscriptionId) {
            this.subscriptions.Add(subscriptionId);
        }

        public void Unsubscribe(int subscriptionId) {
            this.subscriptions.Remove(subscriptionId);
        }

        //////////////////

        //IPXProtocolFeedbackReceiver

        public void RequestReconnect() {
            SetupProtocol();
        }

        public void ReceivedMessage(byte[] message) {
            this.context.Logger().Debug(delegate { return $"Message received: {message}"; });

            try {
                this.context.Handlers().HandleMessage(
                    encoder.DecodeMessage(message),
                    delegate (Action<IResolverContext> handler) {
                        this.ExecuteInMessageScope(delegate (IResolverContext messageContext) {
                            handler(messageContext);
                        });
                    }
                );
            } catch (Exception e) {
                Close();

                this.context.Errors().Handle(e, PXErrorHandlingService.Scope.SocketClientMessage);
            }
        }

        public void ClientDisconnected() {
            ProcessClosingMessage();

            Close();
        }

        public void ClientException(Exception e) {
            OnClientError(e);
        }

        /////////////////////////////
    }
}
