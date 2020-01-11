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

        protected virtual IPXStreamWrapper[] GetStreamWrappers() {
            return new IPXStreamWrapper[] { };
        }

        protected virtual bool IsCliServerActive
        {
            get { return false; }
        }

        public PXServer(IPXInitialOptionsService options) {
            this.container = CreateContainer(options);
        }

        public void Start() {
            try {
                foreach (var module in GetServiceProviders()) {
                    module.OnBoot(this.container);
                }

                foreach (var module in GetServiceProviders()) {
                    module.OnPostBoot(this.container);
                }

                StartCliServer();

                this.container.Logger().Info("Starting socket server");

                tcpListener = new TcpListener(IPAddress.Any, this.container.InitialOptions().Port);
                tcpListener.Start();

                while (true) {
                    PXClient client = new PXClient(
                        tcpListener.AcceptTcpClientAsync().GetAwaiter().GetResult(),
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
                this.container.Logger().Exception(ex);
                Disconnect();
            }
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

            tcpListener.Stop();

            foreach (var client in clients) {
                client.Value.Close();
            }

            cliServer?.Stop();
        }

        private Container CreateContainer(IPXInitialOptionsService options) {
            var container = new Container();

            container.Use(this);
            container.Use<IPXMessageSenderService>(this);
            container.Use(options);

            container.RegisterDelegate(r => new PXSchedulerService(r.Resolve<IContainer>()), Reuse.Singleton);
            container.Register<PXMiddlewareService>(Reuse.Singleton);
            container.Register<PXStreamWrapperService>(Reuse.Singleton);
            container.RegisterDelegate<IPXEnvironmentService>(r => new PXEnvironmentService());
            container.RegisterDelegate(r => new PXLoggerService(r.Resolve<IContainer>()));

            //Initialize in place some dependencies
            container.Resolve<PXSchedulerService>().Launch();
            container.Resolve<PXStreamWrapperService>().SetupWrappers(this.GetStreamWrappers());

            return container;
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
