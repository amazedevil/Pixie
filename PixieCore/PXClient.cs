using DryIoc;
using Newtonsoft.Json.Linq;
using Pixie.Core.Messages;
using Pixie.Core.Middlewares;
using Pixie.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Pixie.Core
{
    class PXClient : IPXClientService
    {
        public string Id { get; private set; }
        public PXMessageReader StreamReader { get; private set; }
        public PXMessageWriter StreamWriter { get; private set; }

        private NetworkStream netStream;
        private TcpClient client;
        private Container container;
        private Dictionary<Type, Type> messageHandlerMap = new Dictionary<Type, Type>();
        private HashSet<int> subscriptions = new HashSet<int>();

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
                    this.ExecuteInMessageScope(delegate (IResolverContext messageContext) {
                        this.container.Middlewares().HandleOverMiddlewares(
                            delegate (IResolverContext ctx) { CreateMessageHandler(StreamReader.DequeueMessage()).Handle(ctx); },
                            messageContext,
                            PXMiddlewareService.Scope.Message
                        );
                    });

                }
            } catch (Exception e) {
                this.container.Logger().Exception(e);

                Close();
            }
        }

        public bool IsSubscribed(int subscriptionId) {
            return subscriptions.Contains(subscriptionId);
        }

        public void ProcessClosingMessage(Type closeMessageHandlerType) {
            if (closeMessageHandlerType != null) {
                this.ExecuteInMessageScope(delegate (IResolverContext messageContext) {
                    this.container.Middlewares().HandleOverMiddlewares(
                        delegate (IResolverContext ctx) { (Activator.CreateInstance(closeMessageHandlerType) as PXMessageHandlerRaw).Handle(ctx); },
                        messageContext,
                        PXMiddlewareService.Scope.Message
                    );
                });
            }
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

        private void ExecuteInMessageScope(Action<IResolverContext> action) {
            using (var messageContext = this.container.OpenScope()) {
                messageContext.Use<IPXClientService>(this);

                action(messageContext);
            }
        }

        //IPXClientService

        public void Send(object message) {
            this.StreamWriter.Send(message);
        }

        public void Subscribe(int subscriptionId) {
            this.subscriptions.Add(subscriptionId);
        }

        public void Unsubscribe(int subscriptionId) {
            this.subscriptions.Remove(subscriptionId);
        }

        //////////////////
    }
}
