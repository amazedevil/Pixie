using DryIoc;
using Pixie.Core.Common.Streams;
using Pixie.Core.Exceptions;
using Pixie.Core.Services;
using Pixie.Core.StreamWrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Pixie.Core.PXSocketClient;

namespace Pixie.Core.Sockets
{
    internal class PXSocketServer : PXEndpointService.IEndpoint, IPXMessageSenderService
    {
        private TcpListener listener;
        private IContainer container;
        private IEnumerable<IPXStreamWrapper> wrappers;
        private Func<IPXProtocol> protocolProvider;

        private IPAddress address;
        private int port;
        private int senderId;
        private bool reconnectAllowed;

        private IDictionary<string, PXSocketClient> clients = new ConcurrentDictionary<string, PXSocketClient>();
        private PXLLProtocol pllProtocol = new PXLLProtocol();
        private CancellationTokenSource serverStopToken = new CancellationTokenSource();
        private object clientRegistrationLock = new object();

        public PXSocketServer(string address, int port, IContainer container, int senderId, IEnumerable<IPXStreamWrapper> wrappers, Func<IPXProtocol> protocolProvider, bool reconnectAllowed) {
            this.address = IPAddress.Parse(address);
            this.port = port;
            this.listener = new TcpListener(this.address, this.port);
            this.senderId = senderId;
            this.container = container;
            this.wrappers = wrappers;
            this.protocolProvider = protocolProvider;
            this.reconnectAllowed = reconnectAllowed;
        }

        async public Task Start() {
            container.Logger().Info($"Starting socket server address: {this.address} port: {this.port}");

            this.container.SenderDispatcher().Register(this);

            using (var socketServerContext = this.container.OpenScope()) {
                try {
                    this.listener.Start();

                    while (true) {
                        ProcessClient(await Task.Run(() => this.listener.AcceptTcpClientAsync(), serverStopToken.Token), socketServerContext);
                    }
                } catch (TaskCanceledException cex) when (cex.CancellationToken == serverStopToken.Token) {
                    //server has been stopped, it's ok
                } catch (Exception ex) {
                    this.container.Errors().Handle(ex, PXErrorHandlingService.Scope.SocketServer);
                } finally {
                    listener?.Stop();
                    
                    lock (clientRegistrationLock) {
                        foreach (var client in clients) {
                            client.Value.Stop();
                        }

                        socketServerContext.Dispose();
                    }

                    this.container.Logger().Info("Socket server stopped");
                }
            }
        }

        private async void ProcessClient(TcpClient tcpClient, IResolverContext socketServerContext) {
            try {
                this.container.Logger().Info($"Incoming connection from: {((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address}");

                var stream = WrapStream(new TcpClientAttachedStream(tcpClient), this.wrappers);

                var clientId = await pllProtocol.WelcomeFromReceiver(stream);

                lock (clientRegistrationLock) {
                    if (socketServerContext.IsDisposed) {
                        return;
                    }

                    if (clients.TryGetValue(clientId, out PXSocketClient existingClient)) {
                        existingClient.SetupStream(stream);

                        this.container.Logger().Info($"Reconnected client id: {clientId}");
                    } else {
                        PXSocketClient client = new PXSocketClient(
                            clientId,
                            socketServerContext,
                            this.protocolProvider
                        );

                        clients[client.Id] = client;

                        if (!this.reconnectAllowed) {
                            client.OnDisconnected += delegate (PXSocketClient c) {
                                c.Stop();
                            };
                        }

                        client.OnDisposed += delegate (PXSocketClient c) {
                            RemoveClient(c);
                        };

                        client.SetupStream(stream);

                        this.container.Logger().Info($"Connected client id: {client.Id}");
                    }
                }
            } catch (PXConnectionClosedRemoteException) {
                this.container.Logger().Info("Connection closed by client");
            } catch (StreamSetupInWrongProtocolStateException e) {
                this.container.Logger().Exception(e);
            } catch (PXLLProtocol.PLLPVersionIncorrectException) {
                this.container.Logger().Error("Client with incorrect PLLP version tried to connect");
            } catch (PXLLProtocol.PLLPUknownException e) {
                this.container.Logger().Exception(e);
            }
        }

        private Stream WrapStream(Stream stream, IEnumerable<IPXStreamWrapper> wrappers) {
            var result = stream;

            foreach (var wrapper in wrappers) {
                result = wrapper.Wrap(result);
            }

            return result;
        }

        private void RemoveClient(PXSocketClient client) {
            clients.Remove(client.Id);
        }

        private void Disconnect() {
            if (serverStopToken.IsCancellationRequested) {
                return;
            }

            this.container.Logger().Info("Stopping socket server");

            serverStopToken.Cancel();
        }

        public void Stop() {
            this.Disconnect();
        }

        //IPXMessageSenderService

        public int SenderId { get => this.senderId; }

        public async Task Send<M>(IEnumerable<string> clientIds, M message) where M: struct {
            this.container.Logger().Info(
                "Command sent to clients: " + message.GetType().ToString()
            );

            foreach (var id in clientIds) {
                if (clients.TryGetValue(id, out var client)) {
                    await client.Send(message);
                }
            }
        }

        public IEnumerable<string> GetClientIds() {
            return clients.Keys;
        }

        public Task<R> SendRequest<M, R>(string clientId, M data) where R: struct where M: struct {
            return clients[clientId].SendRequest<M, R>(data);
        }

        /////////////////////////
    }
}
