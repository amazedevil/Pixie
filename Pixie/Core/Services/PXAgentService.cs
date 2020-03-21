using DryIoc;
using Pixie.Core.Agents;
using Pixie.Core.Exceptions;
using Pixie.Core.Sockets;
using Pixie.Core.StreamWrappers;
using Pixie.Toolbox.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Pixie.Core.Services
{
    public class PXAgentService
    {
        //TODO: make wrapper around PXSocketClient to have senderId and be IPXMessageSenderService

        public class Agent {
        
            internal string Address { get; private set; }
            internal int Port { get; private set; }
            internal int SenderId { get; private set; }

            internal List<IPXStreamWrapper> StreamWrappers { get; private set; }
            internal Func<IPXProtocol> ProtocolProvider { get; private set; }

            public Agent(string address, int port) {
                this.Address = address;
                this.Port = port;
                this.SenderId = PXSenderDispatcherService.DEFAULT_AGENT_SENDER_ID;
                this.StreamWrappers = new List<IPXStreamWrapper>();
                this.ProtocolProvider = delegate { return new PXReliableDeliveryProtocol(true); };
            }

            public Agent Sender(int id) {
                this.SenderId = id;
                return this;
            }

            public Agent StreamWrapper(IPXStreamWrapper wrapper) {
                this.StreamWrappers.Add(wrapper);
                return this;
            }

            public Agent Protocol(Func<IPXProtocol> provider) {
                this.ProtocolProvider = provider;
                return this;
            }
        }

        private List<Agent> descriptions = new List<Agent>();
        private List<PXAgent> agents = null;

        private IContainer container;

        private bool registrationsAllowed = true;

        internal PXAgentService(IContainer container) {
            this.container = container;
        }

        public Agent RegisterAgent(string address, int port) {
            if (!registrationsAllowed) {
                throw new PXRegistrationOutOfTime();
            }

            var agent = new Agent(address, port) as Agent;

            descriptions.Add(agent);

            return agent;
        }

        public void BuildAndStartAgents() {
            foreach (var desc in this.descriptions) {
                BuildAndStartAgent(desc);
            }
        }

        public void StopAgents() {
            foreach (var agent in agents) {
                agent.Stop();
            }
        }

        private void BuildAndStartAgent(Agent description) {
            agents.Add(new PXAgent(
                description.Address,
                description.Port,
                this.container,
                description.StreamWrappers,
                description.ProtocolProvider(),
                description.SenderId
            ));
        }

        internal void CloseRegistration() {
            registrationsAllowed = false;
        }
    }
}
