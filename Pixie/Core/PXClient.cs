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
    internal class PXClient : IPXClientService
    {
        public string Id { get; private set; }
        public PXMessageReader StreamReader { get; private set; }
        public PXMessageWriter StreamWriter { get; private set; }

        private Stream stream;
        private TcpClient client;
        private Container container;
        private HashSet<int> subscriptions = new HashSet<int>();

        private IPXMessageHandlerService messageHandlerService;

        public event Action<PXClient> OnDisconnect;

        public bool IsClosed { get; private set; }

        public PXClient(TcpClient tcpClient, Container container) {
            Id = Guid.NewGuid().ToString();
            client = tcpClient;
            stream = container.Resolve<PXStreamWrapperService>().WrapStream(client.GetStream());

            messageHandlerService = container.Resolve<IPXMessageHandlerService>();

            StreamReader = new PXMessageReader(stream, messageHandlerService.GetMessageTypes());
            StreamWriter = new PXMessageWriter(stream);

            this.container = container;

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
                        this.container.Middlewares().HandleOverMiddlewares(
                            delegate (IResolverContext ctx) { CreateMessageHandler(StreamReader.DequeueMessage()).Handle(ctx); },
                            messageContext,
                            PXMiddlewareService.Scope.Message
                        );
                    });
                }
            } catch (Exception e) {
                Close();

                this.container.Errors().Handle(e, PXErrorHandlingService.Scope.ClientMessage);
            }
        }

        private void OnClientError(Exception e) {
            this.container.Errors().Handle(e, PXErrorHandlingService.Scope.Client);
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
            using (var messageContext = this.container.OpenScope()) {
                messageContext.Use<IPXClientService>(this);

                action(messageContext);
            }
        }

        private void LogRawMessage(string rawMessage) {
            this.container.Logger().Debug(delegate { return $"Message received: {rawMessage}"; });
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
