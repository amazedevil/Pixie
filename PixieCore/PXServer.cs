using DryIoc;
using Pixie.Core.Cli;
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
        private Container container;
        private PXCliServer server;

        protected virtual IPXServiceProvider[] GetServiceProviders() {
            return new IPXServiceProvider[] {};
        }

        protected virtual Type[] GetMessageHandlerTypes() {
            return new Type[] {};
        }

        protected virtual Type GetDisconnectMessageHandlerType() {
            return null;
        }

        protected virtual Type[] GetCliCommandTypes() {
            return new Type[] {};
        }

        public PXServer(IPXInitialOptionsService options) {
            this.container = CreateContainer(options);
        }

        public void Start() {
            foreach (var module in GetServiceProviders()) {
                module.OnBoot(this.container);
            }

            foreach (var module in GetServiceProviders()) {
                module.OnPostBoot(this.container);
            }

            StartCliServer();

            this.container.Logger().Info("Starting socket server");

            try {
                tcpListener = new TcpListener(IPAddress.Any, this.container.InitialOptions().Port);
                tcpListener.Start();

                while (true) {
                    PXClient client = new PXClient(
                        tcpListener.AcceptTcpClientAsync().GetAwaiter().GetResult(),
                        this.container,
                        this.GetMessageHandlerTypes()
                    );

                    client.Start();

                    this.container.Logger().Info("Connecting client id: " + client.Id);

                    client.OnDisconnect += delegate (PXClient c) {
                        DisconnectClient(c);
                    };

                    clients[client.Id] = client;
                }
            } catch (Exception ex) {
                this.container.Logger().Exception(ex);
                Disconnect();
            }
        }

        private void DisconnectClient(PXClient client) {
            client.ProcessClosingMessage(GetDisconnectMessageHandlerType());
            clients.Remove(client.Id);
        }

        private void StartCliServer() {
            var commandTypes = GetCliCommandTypes();

            if (commandTypes.Length == 0) {
                return;
            }

            server = new PXCliServer(commandTypes, this.container);
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

            container.Use<IPXMessageSenderService>(this);
            container.Use(options);

            container.RegisterDelegate(r => new PXSchedulerService(r.Resolve<IContainer>()), Reuse.Singleton);
            container.Register<PXMiddlewareService>(Reuse.Singleton);
            container.Register<PXEnvironmentService>(Reuse.Singleton);

            //Initialize in place some dependencies
            container.Resolve<PXSchedulerService>().Launch();

            return container;
        }

        //IPXMessageSenderService

        public void Send(IEnumerable<string> clientIds, object message) {
            this.container.Logger().Info(
                "Command sent to clients: " + 
                message.GetType()
                    .GetField(PXMessageInfo.MESSAGE_CLASS_FIELD_NAME)
                    .GetValue(null) as string
            );

            foreach (var id in clientIds) {
                if (clients.ContainsKey(id)) {
                    clients[id].Send(message);
                }
            }
        }

        /////////////////////////
    }
}
