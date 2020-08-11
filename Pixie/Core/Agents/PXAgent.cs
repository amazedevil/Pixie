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

        public void Send<M>(IEnumerable<string> clientIds, M data) where M : struct {
            this.client.Send(data);
        }

        public void Send<M>(IEnumerable<string> clientIds, M data, int subscriptionId) where M : struct {
            Send(clientIds, data);
        }

        public Task<R> SendRequest<M, R>(string clientId, M data) where M : struct where R : struct {
            return this.client.SendRequest<M, R>(data);
        }

        /////////////////////////
    }
}
