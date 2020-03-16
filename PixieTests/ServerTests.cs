﻿using DryIoc;
using Moq;
using NUnit.Framework;
using Pixie.Core;
using Pixie.Core.Messages;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services;
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
            ServerTester.PlayCommonServerTest(new SslTestServer("0.0.0.0", PortProvider.ProviderPort()), delegate(TestClient.Builder builder) {
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

            public override void OnRegister(IContainer container) {
                base.OnRegister(container);

                container.Handlers().Register(PXHandlerService.SpecialMessageHandlerItem.ClientDisconnectMessageHandlerProvider(delegate { return handler; }));
            }
        }

        [Test]
        public void ServerDisconnectMessageTest() {
            var port = PortProvider.ProviderPort();

            ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);
            DisconnectTestServer server = new DisconnectTestServer(delegate {
                dataReceivedEvent.Set();
            }, "0.0.0.0", port);

            server.StartAsync();
            var client = TestClient.Builder.Create("127.0.0.1", port).Build();

            client.Run();
            client.Stop();

            if (!dataReceivedEvent.WaitOne(5000)) {
                Assert.Fail("Message timeout");
            }

            dataReceivedEvent.Reset();
        }

        //Agent message passing test
        private class AgentPassingTestServer : PXServer, IPXServiceProvider
        {
            private class ActionExecutorMessageHandler : PXMessageHandlerBase<TestMessages.TestMessageType1>
            {
                private Action<TestMessages.TestMessageType1> action;

                public ActionExecutorMessageHandler(Action<TestMessages.TestMessageType1> action) {
                    this.action = action;
                }

                public override void Handle(TestMessages.TestMessageType1 data) {
                    this.action(data);
                }
            }

            private ActionExecutorMessageHandler handler;
            private string address;
            private int port;
            private string remoteAddress;
            private int remotePort;

            internal AgentPassingTestServer(Action<TestMessages.TestMessageType1> action, string address, int port, string remoteAddress, int remotePort) : base() {
                if (action != null) {
                    this.handler = new ActionExecutorMessageHandler(action);
                } else {
                    this.handler = null;
                }
                this.address = address;
                this.port = port;
                this.remoteAddress = remoteAddress;
                this.remotePort = remotePort;
            }

            protected override IPXServiceProvider[] GetServiceProviders() {
                return new IPXServiceProvider[] {
                    this,
                };
            }

            public void Send(object message) {
                this.container.AgentSender().Send(null, message);
            }

            public void OnRegister(IContainer container) {
                container.Endpoints().RegisterSockerServer(this.address, this.port);

                if (this.remoteAddress != default) {
                    container.Agents().RegisterAgent(remoteAddress, remotePort);
                }

                if (handler != null) {
                    container.Handlers().Register(PXHandlerService.MessageHandlerItem.CreateWithProvider(delegate { return handler; }));
                }
            }

            public void OnInitialize(IContainer container) {
            }
        }


        [Test]
        public void AgentMessageTest() {
            var receiverServerPort = PortProvider.ProviderPort();
            var senderServerPort = PortProvider.ProviderPort();

            var messageToSend = TestMessages.TestMessageType1Sample1();

            ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);
            AgentPassingTestServer receiverServer = new AgentPassingTestServer(delegate(TestMessages.TestMessageType1 message) {
                Assert.AreEqual(messageToSend, message);
                dataReceivedEvent.Set();
            }, "0.0.0.0", receiverServerPort, default, 0);

            AgentPassingTestServer senderServer = new AgentPassingTestServer(
                null, 
                "0.0.0.0", 
                senderServerPort, 
                "127.0.0.1", 
                receiverServerPort
            );

            receiverServer.StartAsync();
            senderServer.StartAsync();
            senderServer.Send(messageToSend);

            if (!dataReceivedEvent.WaitOne(5000)) {
                Assert.Fail("Message timeout");
            }

            dataReceivedEvent.Reset();
        }
    }
}
