using DryIoc;
using Pixie.Core.Cli;
using Pixie.Core.Exceptions;
using Pixie.Core.Sockets;
using Pixie.Core.StreamWrappers;
using Pixie.Toolbox.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pixie.Core.Services
{
    public class PXEndpointService
    {
        public interface IEndpoint
        {
            Task Start();
            void Stop();
        }

        public class EndpointDescription { }

        public class SocketServer : EndpointDescription
        {
            internal string Address { get; private set; }
            internal int Port { get; private set; }
            internal int SenderId { get; private set; }

            internal List<IPXStreamWrapper> StreamWrappers { get; private set; }
            internal Func<IPXProtocol> ProtocolProvider { get; private set; }

            public SocketServer(string address, int port) {
                this.Address = address;
                this.Port = port;
                this.SenderId = PXSenderDispatcherService.DEFAULT_SERVER_SENDER_ID;
                this.StreamWrappers = new List<IPXStreamWrapper>();
                this.ProtocolProvider = delegate { return new PXReliableDeliveryProtocol(); };
            }

            public SocketServer Sender(int id) {
                this.SenderId = id;
                return this;
            }

            public SocketServer StreamWrapper(IPXStreamWrapper wrapper) {
                this.StreamWrappers.Add(wrapper);
                return this;
            }

            public SocketServer Protocol(Func<IPXProtocol> provider) {
                this.ProtocolProvider = provider;
                return this;
            }
        }

        public class CliServer : EndpointDescription
        {
            internal string PipeName { get; private set; }

            public CliServer(string pipeName) {
                this.PipeName = pipeName;
            }
        }

        private List<EndpointDescription> descriptions = new List<EndpointDescription>();
        private IEndpoint[] endpoints;
        private IContainer container;

        private bool registrationsAllowed = true;

        public IEnumerable<IEndpoint> Endpoints
        {
            get { return this.endpoints; }
        }

        internal PXEndpointService(IContainer container) {
            this.container = container;
        }

        public SocketServer RegisterSocketServer(string address, int port) {
            return AddDescription(new SocketServer(address, port)) as SocketServer;
        }

        public CliServer RegisterCliServer(string pipeName = PXCliConsts.PX_CLI_PIPE_NAME_DEFAULT) {
            return AddDescription(new CliServer(pipeName)) as CliServer;
        }

        private EndpointDescription AddDescription(EndpointDescription desc) {
            if (!registrationsAllowed) {
                throw new PXRegistrationOutOfTime();
            }

            descriptions.Add(desc);

            return desc;
        }

        public void BuildEndpoints() {
            endpoints = descriptions.Select(d => BuildEndpoint(d)).ToArray();
        }

        private IEndpoint BuildEndpoint(EndpointDescription description) {
            switch (description) {
                case SocketServer sockServ:
                    return new PXSocketServer(
                        sockServ.Address, 
                        sockServ.Port,
                        this.container,
                        sockServ.SenderId, 
                        sockServ.StreamWrappers,
                        sockServ.ProtocolProvider
                    );
                case CliServer cliServ:
                    return new PXCliServer(
                        cliServ.PipeName,
                        this.container
                    );
            }

            return null;
        }

        public async Task StartEndpoints() {
            foreach (var ep in this.endpoints) {
                await ep.Start();
            }
        }

        public void StopEndpoints() {
            foreach (var ep in this.endpoints) {
                ep.Stop();
            }
        }

        internal void CloseRegistration() {
            registrationsAllowed = false;
        }
    }
}
