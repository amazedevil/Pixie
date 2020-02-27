using DryIoc;
using Pixie.Core.Services;
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

        private IPAddress address;
        private int port;
        private int senderId;

        private IDictionary<string, PXSocketClient> clients = new ConcurrentDictionary<string, PXSocketClient>();

        public PXSocketServer(string address, int port, IContainer container, int senderId) {
            this.address = IPAddress.Parse(address);
            this.port = port;
            this.listener = new TcpListener(this.address, this.port);
            this.senderId = senderId;
            this.container = container;
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
                            socketServerContext
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
            client.ProcessClosingMessage();
            clients.Remove(client.Id);
        }

        protected internal void Disconnect() {
            this.container.Logger().Info("Stopping socket server");

            listener?.Stop();

            foreach (var client in clients) {
                client.Value.Close();
            }
        }

        public void Stop() {
            this.Disconnect();
        }

        //IPXMessageSenderService

        public int SenderId { get => this.senderId; }

        public void Send(IEnumerable<string> clientIds, object message) {
            this.container.Logger().Info(
                "Command sent to clients: " + message.GetType().ToString()
            );

            foreach (var id in clientIds) {
                if (clients.ContainsKey(id)) {
                    clients[id].Send(message);
                }
            }
        }

        public void Send(IEnumerable<string> clientIds, object data, int subscriptionId) {
            this.Send(clientIds.Where(cid => clients.ContainsKey(cid) && clients[cid].IsSubscribed(subscriptionId)), data);
        }

        public IEnumerable<string> GetClientIds() {
            return clients.Keys;
        }

        /////////////////////////
    }
}
