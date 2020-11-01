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

        private Func<IPXProtocol> protocolFactory;

        public event Action<PXSocketClient> OnDisposed;
        public event Action<PXSocketClient> OnDisconnected;

        public PXSocketClient(string clientId, IResolverContext context, Func<IPXProtocol> protocolFactory) {
            Id = clientId;

            this.protocolFactory = protocolFactory;

            this.encoder = new PXMessageEncoder(context.Handlers().GetHandlableMessageTypes());
            this.context = context;

            ResetProtocol();
        }

        public void SetupStream(Stream stream) {
            switch (this.protocol.GetState()) {
                case PXProtocolState.None:
                case PXProtocolState.WaitingForConnection:
                    this.protocol.SetupStream(new PXExceptionsFilterStream(this.stream = stream));
                    break;
                default:
                    throw new Exception("protocol wrong state to setup connection"); //TODO: make specific exception
            }
        }

        public void Stop() {
            if (this.stream == null) {
                return;
            }

            this.stream.Close();
            this.stream = null;

            ResetProtocol();

            ProcessClosingMessage();

            this.OnDisposed?.Invoke(this);
            this.OnDisposed = null;
        }

        private void ResetProtocol() {
            this.protocol?.Dispose();
            this.protocol = protocolFactory();
            this.protocol.Initialize(this);

            async void StartProtocolReading() {
                await HandleProtocolExceptionsAction(async delegate {
                    await protocol.StartReading();
                });
            }

            StartProtocolReading();
        }

        public async Task Send<M>(M message) where M : struct {
            await HandleProtocolExceptionsAction(async delegate {
                await this.protocol.SendMessage(LogData(
                    this.encoder.EncodeMessage(message), 
                    "Sending message: {0}"
                ));
            });
        }

        public async Task<R> SendRequest<M, R>(M message) where M: struct where R: struct {
            this.encoder.RegisterMessageTypeIfNotRegistered(typeof(R));

            R result = default;

            await HandleProtocolExceptionsAction(async delegate {
                result = (R)this.encoder.DecodeMessage(LogData(
                    await this.protocol.SendRequestMessage(LogData(
                        this.encoder.EncodeMessage(message),
                        "Sending request message: {0}"
                    )),
                    "Received request response: {0}"
                ));
            }, true);

            return result;
        }

        private async Task HandleProtocolExceptionsAction(Func<Task> action, bool rethrow = false) {
            try {
                await action();
            } catch (PXConnectionClosedLocalException) {
                if (rethrow) {
                    throw;
                }
            } catch (PXConnectionClosedRemoteException) {
                this.Stop(); //if connection was closed by remote, closing it locally

                if (rethrow) {
                    throw;
                }
            } catch (Exception e) {
                this.context.Errors().Handle(e, PXErrorHandlingService.Scope.SocketClientMessage);

                if (rethrow) {
                    throw new PXConnectionUnknownErrorException(e);
                }
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

        private byte[] LogData(byte[] data, string message) {
            this.context.Logger().Debug(delegate { return string.Format(message, Encoding.UTF8.GetString(data)); });

            return data;
        }

        //IPXProtocolFeedbackReceiver

        public void ReceivedMessage(byte[] message) {
            try {
                this.context.Handlers().HandleMessage(
                    encoder.DecodeMessage(LogData(message, "Message received: {0}")),
                    this.MessageContextProvider
                );
            } catch (Exception e) {
                this.context.Errors().Handle(e, PXErrorHandlingService.Scope.SocketClientMessage);
            }
        }

        public void ReceivedRequestMessage(ushort id, byte[] message) {
            try {
                this.protocol.SendResponse(id, LogData(
                    this.encoder.EncodeMessage(
                        this.context.Handlers().HandleRequestMessage(
                        encoder.DecodeMessage(LogData(
                            message, 
                            "Request message received: {0}"
                        )),
                        this.MessageContextProvider
                    )),
                    "Sending request response: {0}"
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
