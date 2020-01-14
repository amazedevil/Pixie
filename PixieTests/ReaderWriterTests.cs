using NUnit.Framework;
using Pixie.Core.Exceptions;
using Pixie.Core.Messages;
using System;
using System.IO;
using System.Threading;

namespace PixieTests
{
    public class ReaderWriterTests
    {
        struct InnerTestMessageStruct
        {
            public int testInt;
            public string testString;
        }

        struct TestMessage
        {
            public int testInt;
            public string testString;
            public InnerTestMessageStruct testStruct;
        }

        private TestMessage CreateTestMessage() {
            return new TestMessage() {
                testInt = 1,
                testString = "testMessageString",
                testStruct = new InnerTestMessageStruct() {
                    testInt = 2,
                    testString = "testInternalMessageStructTest"
                }
            };
        }

        [Test]
        public void MessagePassingTest() {
            var stream = new MemoryStream();
            var reader = new PXMessageReader(stream, new Type[] { typeof(TestMessage) });
            var writer = new PXMessageWriter(stream);

            var message = CreateTestMessage();

            ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);

            reader.OnDataAvailable += delegate (PXMessageReader r) {
                Assert.AreEqual(message, r.DequeueMessage());

                dataReceivedEvent.Set();
            };

            writer.Send(message);

            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            reader.StartReadingCycle();

            dataReceivedEvent.WaitOne();
            dataReceivedEvent.Reset();

            stream.Dispose();
        }

        [Test]
        public void FailPassingUnregisteredMessageTest() {
            var stream = new MemoryStream();
            var reader = new PXMessageReader(stream, new Type[] { });
            var writer = new PXMessageWriter(stream);

            var message = CreateTestMessage();

            ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);

            reader.OnStreamError += delegate (PXMessageReader r, Exception e) {
                Assert.IsInstanceOf<PXUnregisteredMessageReceived>(e);
                Assert.AreEqual($"Unregistered message with hash 403381675 received", e.Message);

                dataReceivedEvent.Set();
            };

            writer.Send(message);

            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            reader.StartReadingCycle();

            dataReceivedEvent.WaitOne();
            dataReceivedEvent.Reset();

            stream.Dispose();
        }
    }
}