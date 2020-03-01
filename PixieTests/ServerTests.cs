using DryIoc;
using Moq;
using NUnit.Framework;
using Pixie.Core;
using Pixie.Core.Messages;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services;
using Pixie.Core.Services.Internal;
using Pixie.Core.StreamWrappers;
using Pixie.Toolbox.StreamWrappers;
using PixieCoreTests.Client;
using PixieTests.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace PixieTests
{
    class ServerTests
    {
        //Simple server test

        [Test]
        public void ClientToServerMessagePassingTest() {
            ServerTester.PlayCommonServerTest(new ServerTester.TestServer("0.0.0.0", PortProvider.ProviderPort()));
        }

        //SSL test

        private class SslTestServer : ServerTester.TestServer
        {
            internal SslTestServer(string address, int port) : base(address, port) { }

            protected override void HandlerServerDescription(PXEndpointService.SocketServer server) {
                base.HandlerServerDescription(server);

                server.StreamWrapper(new PXSSLStreamWrapper("Resources/certificate.p12"));
            }
        }

        [Test]
        public void SslServerMessagePassingTest() {
            ServerTester.PlayCommonServerTest(new SslTestServer("127.0.0.1", PortProvider.ProviderPort()), delegate(TestClient.Builder builder) {
                builder.SslEnabled(true);
            });
        }

        //Disconnection message handler test

        private class DisconnectTestServer : ServerTester.TestServer, IPXServiceProvider
        {
            private class ActionExecutorMessageHandler : PXMessageHandlerBase<PXMessageVoid>
            {
                private Action disconnectAction;

                public ActionExecutorMessageHandler(Action disconnectAction) {
                    this.disconnectAction = disconnectAction;
                }

                public override void Handle() {
                    disconnectAction();
                }
            }

            private ActionExecutorMessageHandler handler;

            internal DisconnectTestServer(Action disconnectAction, string host, int port) : base(host, port) {
                this.handler = new ActionExecutorMessageHandler(disconnectAction);
            }

            public override void OnInitialize(IContainer container) {
                base.OnInitialize(container);

                container.Handlers().RegisterProviderForClientDisconnect(delegate { return handler; });
            }
        }

        [Test]
        public void ServerDisconnectMessageTest() {
            var port = PortProvider.ProviderPort();

            ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);
            DisconnectTestServer server = new DisconnectTestServer(delegate {
                dataReceivedEvent.Set();
            }, "127.0.0.1", port);

            server.StartAsync();
            var client = TestClient.Builder.Create("127.0.0.1", port).Build();

            client.Run();
            client.Stop();

            if (!dataReceivedEvent.WaitOne(5000)) {
                Assert.Fail("Message timeout");
            }

            dataReceivedEvent.Reset();
        }
    }
}
