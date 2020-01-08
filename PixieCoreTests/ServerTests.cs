using DryIoc;
using Moq;
using NUnit.Framework;
using Pixie.Core;
using Pixie.Core.Messages;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services;
using PixieCoreTests.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PixieCoreTests
{
    class ServerTests {

        private struct TestMessage {
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

            public TestServer(IPXInitialOptionsService options, Action<object> action) : base(options) {
                this.action = action;
            }

            protected override IPXServiceProvider[] GetServiceProviders() {
                return base.GetServiceProviders();


            }

            protected override Type[] GetMessageHandlerTypes() {
                return new Type[] { 
                    typeof(MessageHandler)
                };
            }
        }

        private class TestInitialOptions : IPXInitialOptionsService
        {
            public int Port => 7777;

            public bool Debug => false;

            public string Host => "localhost";
        }

        [Test]
        public void ClientToServerMessagePassingTest() {
            var message = new TestMessage() { testString = "test" };
            ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);

            var initials = new TestInitialOptions();
            TestServer server = new TestServer(initials, delegate (object receivedMessage) {
                Assert.AreEqual(receivedMessage, message);
                dataReceivedEvent.Set();
            });
            TestClient client = new TestClient(
                initials.Host, 
                initials.Port, 
                new Type[] {
                    typeof(MessageHandler)
                }, delegate(object receivedMessage) { }
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
