using DryIoc;
using Pixie.Core.Cli;
using Pixie.Core.Exceptions;
using Pixie.Core.Sockets;
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

            public SocketServer(string address, int port, int senderId = PXSenderDispatcherService.DEFAULT_SENDER_ID) {
                this.Address = address;
                this.Port = port;
                this.SenderId = senderId;
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

        public void RegisterSockerServer(string address, int port) {
            AddDescription(new SocketServer(address, port));
        }

        public void RegisterCliServer(string pipeName = "default") {
            AddDescription(new CliServer(pipeName));
        }

        private void AddDescription(EndpointDescription desc) {
            if (!registrationsAllowed) {
                throw new PXRegistrationOutOfTime();
            }

            descriptions.Add(desc);
        }

        public void BuildEndpoints() {
            endpoints = descriptions.Select(d => BuildEndpoint(d)).ToArray();
        }

        private IEndpoint BuildEndpoint(EndpointDescription description) {
            switch (description) {
                case SocketServer sockServ:
                    return new PXSocketServer(sockServ.Address, sockServ.Port, this.container, sockServ.SenderId);
                case CliServer cliServ:
                    return new PXCliServer(cliServ.PipeName, this.container);
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
