using DryIoc;
using Pixie.Core.DependencyInjection;
using Pixie.Core.Messages;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Pixie.Core {
    public class PXServer : IPXMessageSenderService {
        private TcpListener tcpListener;
        private IDictionary<string, PXClient> clients = new ConcurrentDictionary<string, PXClient>();
        private PXSchedulerService scheduler;
        private Container container;

        protected virtual IPXServiceProvider[] GetServiceProviders() {
            return new IPXServiceProvider[] {};
        }

        protected virtual Type[] GetMessageHandlerTypes() {
            return new Type[] {};
        }

        protected virtual Type GetDisconnectMessageHandlerType() {
            return null;
        }

        public PXServer(IPXInitialOptionsService options) {
            this.container = CreateContainer(options);
        }

        public async void Start() {
            this.container.Logger().Info("Starting socket server");

            foreach (var module in GetServiceProviders()) {
                module.OnBoot(this.container);
            }

            foreach (var module in GetServiceProviders()) {
                module.OnPostBoot(this.container);
            }

            this.container.SchedulerService().Launch();

            try {
                tcpListener = new TcpListener(IPAddress.Any, this.container.InitialOptions().Port);
                tcpListener.Start();

                while (true) {
                    PXClient client = new PXClient(await tcpListener.AcceptTcpClientAsync(), this.container, this.GetMessageHandlerTypes());

                    this.container.Logger().Info("Connecting client id: " + client.Id);

                    client.OnDisconnect += delegate (PXClient c) {
                        DisconnectClient(c.Id);
                    };

                    clients[client.Id] = client;
                }
            } catch (Exception ex) {
                this.container.Logger().Exception(ex);
                Disconnect();
            }
        }

        private void DisconnectClient(string clientId) {
            Type messageHandlerType = GetDisconnectMessageHandlerType();

            if (messageHandlerType != null) {
                this.container.Middlewares().HandleMessagesOverMiddlewares(
                    Activator.CreateInstance(messageHandlerType) as PXMessageHandlerRaw,
                    this.container.CreateFacade()
                );
            }

            clients.Remove(clientId);
        }

        protected internal void Disconnect() {
            this.container.Logger().Info("Stopping socket server");

            tcpListener.Stop();

            foreach (var client in clients) {
                client.Value.Close();
            }
        }

        private Container CreateContainer(IPXInitialOptionsService options) {
            var container = new Container();

            container.Use(new PXEnvironmentService());
            container.Use<IPXMessageSenderService>(this);
            container.Use(options);
            container.Use(scheduler = new PXSchedulerService(container));

            return container;
        }

        //IPXMessageSenderService

        public void Send(ICollection<string> clientIds, object message) {
            this.container.Logger().Info(
                "Command sent to clients: " + 
                message.GetType()
                .GetField(PXMessageInfo.MESSAGE_CLASS_FIELD_NAME)
                .GetValue(null) as string
            );

            foreach (var id in clientIds) {
                clients[id].Send(message);
            }
        }

        /////////////////////////
    }
}
