using NUnit.Framework;
using Pixie.Core;
using Pixie.Core.Messages;
using Pixie.Core.ServiceProviders;
using PixieCoreTests.Client;
using System;
using System.Threading;
using DryIoc;

namespace PixieTests.Common
{
    internal class ServerTester
    {
        private class MessageHandler : PXMessageHandlerBase<TestMessages.TestMessageType1>
        {
            public override void Handle(TestMessages.TestMessageType1 data) {
                base.Handle(data);

                (this.context.Resolve<PXServer>() as TestServer).Action(data);
            }
        }

        public class TestServer : PXServer
        {
            internal Action<object> Action;

            public string Host { get; private set; }
            public int Port { get; private set; }

            internal TestServer(string host, int port) {
                this.Host = host;
                this.Port = port;
            }

            protected override IPXServiceProvider[] GetServiceProviders() {
                return new IPXServiceProvider[] {
                    CreateEnvServiceProvider()
                };
            }

            protected override Type[] GetMessageHandlerTypes() {
                return new Type[] {
                    typeof(MessageHandler)
                };
            }

            protected virtual EnvironmentDefaultsServiceProvider CreateEnvServiceProvider() {
                return new EnvironmentDefaultsServiceProvider(this.Host, this.Port);
            }
        }

        public static void PlayCommonServerTest(TestServer server, Action<TestClient.Builder> builderConfigurator = null) {
            var message = TestMessages.TestMessageType1Sample1();
            ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);

            server.Action = delegate (object receivedMessage) {
                Assert.AreEqual(receivedMessage, message);
                dataReceivedEvent.Set();
            };

            var clientBuilder = TestClient.Builder.Create(
                server.Host,
                server.Port
            ).EventTypes(
                new Type[] {
                    typeof(TestMessages.TestMessageType1)
                }
            );

            builderConfigurator?.Invoke(clientBuilder);

            TestClient client = clientBuilder.Build();

            server.StartAsync();
            client.Run();

            client.SendMessage(message);

            if (!dataReceivedEvent.WaitOne(5000)) {
                Assert.Fail("Message timeout");
            }

            dataReceivedEvent.Reset();
        }
    }
}
