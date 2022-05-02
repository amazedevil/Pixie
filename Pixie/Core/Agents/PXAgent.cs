using DryIoc;
using Pixie.Core.Services;
using Pixie.Core.Sockets;
using Pixie.Core.StreamWrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Pixie.Core.Agents
{
    internal class PXAgent : IPXMessageSenderService
    {
        private PXSocketClient client;
        private TaskCompletionSource<PXSocketClient> clientReadyTSC = new TaskCompletionSource<PXSocketClient>();

        public PXAgent(string address, int port, IResolverContext context, IEnumerable<IPXStreamWrapper> wrappers, Func<IPXProtocol> protocolProvider, int senderId) {
            var clientId = Guid.NewGuid();

            async void Connect() {
                var stream = WrapStream(new TcpClientAttachedStream(new TcpClient(address, port)), wrappers);

                await (new PXLLProtocol()).WelcomeFromSender(stream, clientId.ToString());

                this.client.SetupStream(stream);
                clientReadyTSC.SetResult(this.client);
            }
            
            this.SenderId = senderId;

            this.client = new PXSocketClient(clientId.ToString(), context, protocolProvider);
            Connect();

            this.client.OnDisconnected += aClient => {
                clientReadyTSC = new TaskCompletionSource<PXSocketClient>();
                Connect();
            };

            context.SenderDispatcher().Register(this);
        }

        //TODO: remove duplication with PXSocketServer
        private Stream WrapStream(Stream stream, IEnumerable<IPXStreamWrapper> wrappers) {
            var result = stream;

            foreach (var wrapper in wrappers) {
                result = wrapper.Wrap(result);
            }

            return result;
        }

        public void Stop() {
            client.Stop();
        }

        //IPXMessageSenderService

        public int SenderId { get; }

        public IEnumerable<string> GetClientIds() {
            return null;
        }

        public async Task Send<M>(IEnumerable<string> clientIds, M data) where M : struct {
            await (await this.clientReadyTSC.Task).Send(data);
        }

        public async Task<R> SendRequest<M, R>(string clientId, M data) where M : struct where R : struct {
            return await (await this.clientReadyTSC.Task).SendRequest<M, R>(data);
        }

        /////////////////////////
    }
}
