﻿using DryIoc;
using Pixie.Core.Cli;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services.Internal;
using Pixie.Core.Services;
using Pixie.Core.StreamWrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Pixie.Core
{
    public class PXServer : IPXMessageSenderService
    {
        private TcpListener tcpListener;
        private IDictionary<string, PXClient> clients = new ConcurrentDictionary<string, PXClient>();
        private Container container;
        private PXCliServer cliServer = null;

        protected virtual IPXServiceProvider[] GetServiceProviders() {
            return new IPXServiceProvider[] { };
        }

        protected virtual Type[] GetMessageHandlerTypes() {
            return new Type[] { };
        }

        protected virtual Type GetDisconnectMessageHandlerType() {
            return null;
        }

        protected virtual IPXStreamWrapper[] GetStreamWrappers(IResolverContext context) {
            return new IPXStreamWrapper[] { };
        }

        protected virtual bool IsCliServerActive
        {
            get { return false; }
        }

        public PXServer() {
            this.container = CreateContainer();
        }

        public void StartAsync() {
            _ = Start();
        }

        public void StartSync() {
            Start().Wait();
        }

        public async Task Start() {
            try {
                foreach (var module in GetServiceProviders()) {
                    module.OnBoot(this.container);
                }

                SetupWrappers();

                foreach (var module in GetServiceProviders()) {
                    module.OnPostBoot(this.container);
                }

                StartScheduler();

                StartCliServer();

                this.container.Logger().Info("Starting socket server");

                tcpListener = new TcpListener(IPAddress.Any, this.container.Env().Port());
                tcpListener.Start();

                while (true) {
                    PXClient client = new PXClient(
                        await tcpListener.AcceptTcpClientAsync(),
                        this.container,
                        this.GetMessageHandlerTypes()
                    );

                    clients[client.Id] = client;

                    client.OnDisconnect += delegate (PXClient c) {
                        DisconnectClient(c);
                    };

                    client.Start();

                    this.container.Logger().Info("Connected client id: " + client.Id);
                }
            } catch (Exception ex) {
                Disconnect();

                this.container.Errors().Handle(ex, PXErrorHandlingService.Scope.Server);
            }
        }

        public void Stop() {
            tcpListener?.Stop();
        }

        private void DisconnectClient(PXClient client) {
            client.ProcessClosingMessage(GetDisconnectMessageHandlerType());
            clients.Remove(client.Id);
        }

        private void StartCliServer() {
            if (!IsCliServerActive) {
                return;
            }

            cliServer = new PXCliServer(this.container);
        }

        protected internal void Disconnect() {
            this.container.Logger().Info("Stopping server");

            tcpListener?.Stop();

            foreach (var client in clients) {
                client.Value.Close();
            }

            cliServer?.Stop();
        }

        private Container CreateContainer() {
            var container = new Container();

            container.Use(this);
            container.Use<IPXMessageSenderService>(this);

            container.RegisterDelegate(r => new PXSchedulerService(r.Resolve<IContainer>()), Reuse.Singleton);
            container.Register<PXMiddlewareService>(Reuse.Singleton);
            container.Register<PXStreamWrapperService>(Reuse.Singleton);
            container.Register<PXErrorHandlingService>();
            container.RegisterDelegate<IPXEnvironmentService>(r => new PXEnvironmentService());
            container.RegisterDelegate(r => new PXLoggerService(r.Resolve<IContainer>()));

            return container;
        }

        private void StartScheduler() {
            container.Resolve<PXSchedulerService>().Launch();
        }

        private void SetupWrappers() {
            container.Resolve<PXStreamWrapperService>().SetupWrappers(this.GetStreamWrappers(container));
        }

        //IPXMessageSenderService

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
