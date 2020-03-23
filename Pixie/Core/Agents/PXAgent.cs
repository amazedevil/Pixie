using DryIoc;
using Pixie.Core.Services;
using Pixie.Core.Sockets;
using Pixie.Core.StreamWrappers;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Pixie.Core.Agents
{
    internal class PXAgent : IPXMessageSenderService
    {
        private PXSocketClient client;

        public PXAgent(string address, int port, IResolverContext context, IEnumerable<IPXStreamWrapper> wrappers, IPXProtocol protocol, int senderId) {
            this.SenderId = senderId;

            this.client = new PXSocketClient(null, context, wrappers, protocol, delegate {
                return new TcpClient(address, port);
            });

            context.SenderDispatcher().Register(this);

            this.client.Start();
        }

        public void Stop() {
            client.Stop();
        }

        //IPXMessageSenderService

        public int SenderId { get; }

        public IEnumerable<string> GetClientIds() {
            return null;
        }

        public void Send(IEnumerable<string> clientIds, object data) {
            this.client.Send(data);
        }

        public void Send(IEnumerable<string> clientIds, object data, int subscriptionId) {
            Send(clientIds, data);
        }

        public Task<object> SendRequest(string clientId, object data) {
            return this.client.SendRequest(clientId);
        }

        /////////////////////////
    }
}
