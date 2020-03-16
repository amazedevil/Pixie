using DryIoc;
using Pixie.Core.Services;
using Pixie.Core.StreamWrappers;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Pixie.Core.Agents
{
    internal class PXAgent : IPXMessageSenderService
    {
        private PXSocketClient client;

        public PXAgent(string address, int port, IResolverContext context, IEnumerable<IPXStreamWrapper> wrappers, int senderId) {
            this.SenderId = senderId;
            
            void SetupClient() {
                this.client = new PXSocketClient(
                    new TcpClient(address, port),
                    context,
                    wrappers
                );
            }

            SetupClient();

            this.client.OnBreakConnection += delegate (PXSocketClient client) {
                SetupClient();
            };

            context.SenderDispatcher().Register(this);
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

        /////////////////////////
    }
}
