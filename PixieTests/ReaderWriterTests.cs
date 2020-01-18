using NUnit.Framework;
using Pixie.Core.Exceptions;
using Pixie.Core.Messages;
using PixieTests.Common;
using System;
using System.IO;
using System.Threading;

namespace PixieTests
{
    public class ReaderWriterTests
    {
        [Test]
        public void MessagePassingTest() {
            var stream = new MemoryStream();
            var reader = new PXMessageReader(stream, new Type[] { typeof(TestMessages.TestMessageType1) });
            var writer = new PXMessageWriter(stream);

            var message = TestMessages.TestMessageType1Sample1();

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
            var reader = new PXMessageReader(stream, new Type[] {});
            var writer = new PXMessageWriter(stream);

            var message = TestMessages.TestMessageType1Sample1();

            ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);

            reader.OnStreamError += delegate (PXMessageReader r, Exception e) {
                Assert.IsInstanceOf<PXUnregisteredMessageReceived>(e);
                Assert.AreEqual($"Unregistered message with hash 155806286 received", e.Message);

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