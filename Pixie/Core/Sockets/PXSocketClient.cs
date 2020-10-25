using DryIoc;
using Newtonsoft.Json.Serialization;
using Pixie.Core.Common.Streams;
using Pixie.Core.Exceptions;
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
using System.Text;
using System.Threading.Tasks;

namespace Pixie.Core
{
    internal class PXSocketClient : IPXClientService, IPXProtocolContact
    {
        public string Id { get; private set; }

        private PXMessageEncoder encoder;
        private IPXProtocol protocol;

        private Stream stream;
        private IResolverContext context;
        private PXExceptionsFilterStream exceptionFilter;

        public event Action<PXSocketClient> OnDisposed;
        public event Action<PXSocketClient> OnDisconnected;

        public PXSocketClient(string clientId, IResolverContext context, Func<IPXProtocol> protocolFactory) {
            Id = clientId;

            protocol = protocolFactory();
            protocol.Initialize(this);

            this.encoder = new PXMessageEncoder(context.Handlers().GetHandlableMessageTypes());
            this.context = context;

            StartProtocolReading();
        }

        public void SetupStream(Stream stream) {
            this.stream = stream;

            switch (this.protocol.GetState()) {
                case PXProtocolState.None:
                case PXProtocolState.WaitingForConnection:
                    exceptionFilter = new PXExceptionsFilterStream(this.stream);
                    this.protocol.SetupStream(exceptionFilter);
                    break;
                default:
                    throw new Exception("protocol wrong state to setup connection"); //TODO: make specific exception
            }
        }

        public async void StartProtocolReading() {
            await HandleProtocolExceptionsAction(async delegate {
                await protocol.StartReading();
            });
        }

        public void Stop() {
            this.exceptionFilter.Dispose();
            this.stream.Close();
            this.protocol.Dispose();

            ProcessClosingMessage();

            this.OnDisposed?.Invoke(this);
            this.OnDisposed = null;
        }

        public async Task Send<M>(M message) where M : struct {
            await HandleProtocolExceptionsAction(async delegate {
                await this.protocol.SendMessage(this.encoder.EncodeMessage(message));
            });
        }

        public async Task<R> SendRequest<M, R>(M message) where M: struct where R: struct {
            this.encoder.RegisterMessageTypeIfNotRegistered(typeof(R));

            return (R)this.encoder.DecodeMessage(await this.protocol.SendRequestMessage(this.encoder.EncodeMessage(message)));
        }

        private async Task HandleProtocolExceptionsAction(Func<Task> action) {
            try {
                await action();
            } catch (PXConnectionClosedException) {
                //do nothing
            } catch (PXConnectionFinishedException) {
                this.Stop();
            } catch (Exception e) {
                this.context.Errors().Handle(e, PXErrorHandlingService.Scope.SocketClientMessage);
            }
        }

        public void OnClientError(Exception e) {
            this.context.Errors().Handle(e, PXErrorHandlingService.Scope.SocketClient);
        }

        private void ProcessClosingMessage() {
            try {
                this.context.Handlers().HandleSpecialMessage(
                    PXHandlerService.SpecificMessageHandlerType.ClientDisconnect,
                    delegate (Action<IResolverContext> handler) {
                        this.ExecuteInMessageScope(delegate (IResolverContext messageContext) {
                            handler(messageContext);
                        });
                    }
                );
            } catch (Exception e) {
                this.context.Errors().Handle(e, PXErrorHandlingService.Scope.SocketClientMessage);
            }
        }

        private void ExecuteInMessageScope(Action<IResolverContext> action) {
            using (var messageContext = this.context.OpenScope()) {
                messageContext.Use<IPXClientService>(this);

                action(messageContext);
            }
        }

        private void MessageContextProvider(Action<IResolverContext> handler) {
            this.ExecuteInMessageScope(delegate (IResolverContext messageContext) {
                handler(messageContext);
            });
        }

        //IPXProtocolFeedbackReceiver

        public void ReceivedMessage(byte[] message) {
            try {
                this.context.Handlers().HandleMessage(
                    encoder.DecodeMessage(message),
                    this.MessageContextProvider
                );
            } catch (Exception e) {
                this.context.Errors().Handle(e, PXErrorHandlingService.Scope.SocketClientMessage);
            }
        }

        public void ReceivedRequestMessage(ushort id, byte[] message) {
            try {
                this.protocol.SendResponse(id, this.encoder.EncodeMessage(
                    this.context.Handlers().HandleRequestMessage(
                        encoder.DecodeMessage(message),
                        this.MessageContextProvider
                    )
                ));
            } catch (Exception e) {
                this.context.Errors().Handle(e, PXErrorHandlingService.Scope.SocketClientMessage);
            }
        }

        public void OnProtocolStateChanged() {
            if (this.protocol.GetState() == PXProtocolState.WaitingForConnection) {
                this.OnDisconnected?.Invoke(this);
            }
        }

        /////////////////////////////
    }
}
