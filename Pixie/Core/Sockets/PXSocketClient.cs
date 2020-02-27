using DryIoc;
using Pixie.Core.Messages;
using Pixie.Core.Services;
using Pixie.Core.Services.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;

namespace Pixie.Core
{
    internal class PXSocketClient : IPXClientService
    {
        public string Id { get; private set; }
        public PXMessageReader StreamReader { get; private set; }
        public PXMessageWriter StreamWriter { get; private set; }

        private Stream stream;
        private TcpClient client;
        private IResolverContext context;
        private HashSet<int> subscriptions = new HashSet<int>();

        private IPXMessageHandlerService messageHandlerService;

        public event Action<PXSocketClient> OnDisconnect;

        public bool IsClosed { get; private set; }

        public PXSocketClient(TcpClient tcpClient, IResolverContext context) {
            Id = Guid.NewGuid().ToString();
            client = tcpClient;
            stream = context.StreamWrappers().WrapStream(client.GetStream());

            messageHandlerService = context.Handlers();

            StreamReader = new PXMessageReader(stream, messageHandlerService.GetMessageTypes());
            StreamWriter = new PXMessageWriter(stream);

            this.context = context;

            StreamReader.OnDataAvailable += delegate (PXMessageReader reader) {
                Process();
            };

            StreamReader.OnRawMessageReady += delegate (PXMessageReader reader, string rawMessage) {
                LogRawMessage(rawMessage);
            };

            StreamReader.OnStreamClose += delegate {
                Close();
            };

            StreamReader.OnStreamError += delegate (PXMessageReader r, Exception e) {
                OnClientError(e);
            };
        }

        public void Start() {
            StreamReader.StartReadingCycle();
        }

        public void Process() {
            try {
                if (StreamReader.HasMessages) {
                    this.ExecuteInMessageScope(delegate (IResolverContext messageContext) {
                        this.context.Middlewares().HandleOverMiddlewares(
                            delegate (IResolverContext ctx) { CreateMessageHandler(StreamReader.DequeueMessage()).Handle(ctx); },
                            messageContext,
                            PXMiddlewareService.Scope.Message
                        );
                    });
                }
            } catch (Exception e) {
                Close();

                this.context.Errors().Handle(e, PXErrorHandlingService.Scope.SocketClientMessage);
            }
        }

        private void OnClientError(Exception e) {
            this.context.Errors().Handle(e, PXErrorHandlingService.Scope.SocketClient);
        }

        public bool IsSubscribed(int subscriptionId) {
            return subscriptions.Contains(subscriptionId);
        }

        public void ProcessClosingMessage() {
            var handler = this.context.Handlers().InstantiateForClientDisconnect();

            if (handler != null) {
                this.ExecuteInMessageScope(delegate (IResolverContext messageContext) {
                    this.context.Middlewares().HandleOverMiddlewares(
                        delegate (IResolverContext ctx) { handler.Handle(ctx); },
                        messageContext,
                        PXMiddlewareService.Scope.Message
                    );
                });
            }
        }

        protected internal void Close() {
            stream.Close();
            client.Close();

            IsClosed = true;

            OnDisconnect?.Invoke(this);
            OnDisconnect = null;
        }

        private PXMessageHandlerRaw CreateMessageHandler(object message) {
            var handler = messageHandlerService.Instantiate(message.GetType());
            handler.SetupData(message);
            return handler;
        }

        private void ExecuteInMessageScope(Action<IResolverContext> action) {
            using (var messageContext = this.context.OpenScope()) {
                messageContext.Use<IPXClientService>(this);

                action(messageContext);
            }
        }

        private void LogRawMessage(string rawMessage) {
            this.context.Logger().Debug(delegate { return $"Message received: {rawMessage}"; });
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
