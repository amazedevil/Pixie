using DryIoc;
using Moq;
using NUnit.Framework;
using Pixie.Core;
using Pixie.Core.Messages;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services;
using Pixie.Core.Services.Internal;
using PixieCoreTests.Client;
using PixieTests.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PixieTests
{
    class ServerTests
    {

        private struct TestMessage
        {
            public string testString;
        }

        private class MessageHandler : PXMessageHandlerBase<TestMessage>
        {
            public override void Handle(TestMessage data) {
                base.Handle(data);

                (this.context.Resolve<PXServer>() as TestServer).action(data);
            }
        }

        private class TestServer : PXServer
        {
            public Action<object> action;

            public TestServer(Action<object> action) : base() {
                this.action = action;
            }

            protected override IPXServiceProvider[] GetServiceProviders() {
                return new IPXServiceProvider[] {
                    new EnvironmentDefaultsServiceProvider()
                };
            }

            protected override Type[] GetMessageHandlerTypes() {
                return new Type[] {
                    typeof(MessageHandler)
                };
            }
        }

        [Test]
        public void ClientToServerMessagePassingTest() {
            var message = new TestMessage() { testString = "test" };
            ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);

            TestServer server = new TestServer(delegate (object receivedMessage) {
                Assert.AreEqual(receivedMessage, message);
                dataReceivedEvent.Set();
            });

            TestClient client = new TestClient(
                EnvironmentDefaultsServiceProvider.HOST,
                EnvironmentDefaultsServiceProvider.PORT,
                new Type[] {
                    typeof(MessageHandler)
                }, delegate (object receivedMessage) { }
            );

            new Thread(new ThreadStart(
                delegate () {
                    server.Start();
                }
            )).Start();

            Thread.Sleep(1000); //take time to start server
            client.Run();

            client.SendMessage(message);

            if (!dataReceivedEvent.WaitOne(5000)) {
                Assert.Fail("Message timeout");
            }

            dataReceivedEvent.Reset();
        }
    }
}
