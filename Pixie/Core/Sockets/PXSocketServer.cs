using DryIoc;
using Pixie.Core.Services;
using Pixie.Core.StreamWrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

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

        private IDictionary<string, PXSocketClient> clients = new ConcurrentDictionary<string, PXSocketClient>();

        public PXSocketServer(string address, int port, IContainer container, int senderId, IEnumerable<IPXStreamWrapper> wrappers, Func<IPXProtocol> protocolProvider) {
            this.address = IPAddress.Parse(address);
            this.port = port;
            this.listener = new TcpListener(this.address, this.port);
            this.senderId = senderId;
            this.container = container;
            this.wrappers = wrappers;
            this.protocolProvider = protocolProvider;
        }

        async public Task Start() {
            container.Logger().Info($"Starting socket server address: {this.address} port: {this.port}");

            this.container.SenderDispatcher().Register(this);

            try {
                using (var socketServerContext = this.container.OpenScope()) {
                    this.listener.Start();

                    while (true) {
                        PXSocketClient client = new PXSocketClient(
                            await this.listener.AcceptTcpClientAsync(),
                            socketServerContext,
                            this.wrappers,
                            this.protocolProvider()
                        );

                        clients[client.Id] = client;

                        client.OnDisconnect += delegate (PXSocketClient c) {
                            DisconnectClient(c);
                        };

                        client.Start();

                        this.container.Logger().Info("Connected client id: " + client.Id);
                    }
                }
            } catch (Exception ex) {
                Disconnect();

                this.container.Errors().Handle(ex, PXErrorHandlingService.Scope.SocketServer);
            } finally {
                this.container.SenderDispatcher().Unregister(this);
            }
        }

        private void DisconnectClient(PXSocketClient client) {
            clients.Remove(client.Id);
        }

        private void Disconnect() {
            this.container.Logger().Info("Stopping socket server");

            listener?.Stop();

            foreach (var client in clients) {
                client.Value.Stop();
            }
        }

        public void Stop() {
            this.Disconnect();
        }

        //IPXMessageSenderService

        public int SenderId { get => this.senderId; }

        public void Send<M>(IEnumerable<string> clientIds, M message) where M: struct {
            this.container.Logger().Info(
                "Command sent to clients: " + message.GetType().ToString()
            );

            foreach (var id in clientIds) {
                if (clients.ContainsKey(id)) {
                    clients[id].Send(message);
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
