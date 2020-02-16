﻿using NUnit.Framework;
using Pixie.Core;
using Pixie.Core.Messages;
using Pixie.Core.ServiceProviders;
using PixieCoreTests.Client;
using System;
using System.Threading;
using DryIoc;
using Pixie.Core.Services;

namespace PixieTests.Common
{
    internal class ServerTester
    {
        private class MessageHandler : PXMessageHandlerBase<TestMessages.TestMessageType1>
        {
            public override void Handle(TestMessages.TestMessageType1 data) {
                base.Handle(data);

                var serverToClientMessage = (this.context.Resolve<PXServer>() as TestServer).Action(data);

                this.context.Sender().Send(this.context.Sender().GetClientIds(), serverToClientMessage);
            }
        }

        public class TestServer : PXServer, IPXServiceProvider
        {
            internal Func<object, object> Action;

            public string Host { get; private set; }
            public int Port { get; private set; }

            internal TestServer(string host, int port) {
                this.Host = host;
                this.Port = port;
            }

            protected override IPXServiceProvider[] GetServiceProviders() {
                return new IPXServiceProvider[] {
                    CreateEnvServiceProvider(),
                    this,
                };
            }

            protected virtual EnvironmentDefaultsServiceProvider CreateEnvServiceProvider() {
                return new EnvironmentDefaultsServiceProvider(this.Host, this.Port);
            }

            public virtual void OnRegister(IContainer container) {
                container.Handlers().RegisterHandlerType<MessageHandler>();
            }

            public virtual void OnInitialize(IContainer container) {
            }
        }

        public static void PlayCommonServerTest(TestServer server, Action<TestClient.Builder> builderConfigurator = null) {
            var clientToServerMessage = TestMessages.TestMessageType1Sample1();
            var serverToClientMessage = TestMessages.TestMessageType2Sample1();
            ManualResetEvent serverReceivedDataEvent = new ManualResetEvent(false);
            ManualResetEvent clientReceivedDataEvent = new ManualResetEvent(false);

            server.Action = delegate (object receivedMessage) {
                Assert.AreEqual(receivedMessage, clientToServerMessage);
                serverReceivedDataEvent.Set();

                return serverToClientMessage;
            };

            var clientBuilder = TestClient.Builder.Create(
                server.Host,
                server.Port
            ).EventTypes(
                new Type[] {
                    typeof(TestMessages.TestMessageType2)
                }
            ).OnMessageReceived(delegate(object receivedMessage) {
                Assert.AreEqual(receivedMessage, serverToClientMessage);
                clientReceivedDataEvent.Set();
            });

            builderConfigurator?.Invoke(clientBuilder);

            TestClient client = clientBuilder.Build();

            server.StartAsync();
            client.Run();

            client.SendMessage(clientToServerMessage);

            if (!serverReceivedDataEvent.WaitOne(5000)) {
                Assert.Fail("Server message timeout");
            }

            if (!clientReceivedDataEvent.WaitOne(5000)) {
                Assert.Fail("Client message timeout");
            }

            serverReceivedDataEvent.Reset();
            clientReceivedDataEvent.Reset();
        }
    }
}
