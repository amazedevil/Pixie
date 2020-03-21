using NUnit.Framework;
using Pixie.Core.Exceptions;
using Pixie.Core.Messages;
using PixieTests.Common;
using System;
using System.IO;
using System.Threading;

namespace PixieTests
{
    public class EncoderTests
    {
        [Test]
        public void MessagePassingTest() {
            var encoder = new PXMessageEncoder(new Type[] { typeof(TestMessages.TestMessageType1) });
            var message = TestMessages.TestMessageType1Sample1();

            Assert.AreEqual(encoder.DecodeMessage(encoder.EncodeMessage(message)), message);
        }

        [Test]
        public void FailPassingUnregisteredMessageTest() {
            var encoder = new PXMessageEncoder(new Type[] {});
            var message = TestMessages.TestMessageType1Sample1();

            PXUnregisteredMessageReceived received = Assert.Throws<PXUnregisteredMessageReceived>(delegate {
                encoder.DecodeMessage(encoder.EncodeMessage(message));
            });

            Assert.AreEqual(received.Message, $"Unregistered message with hash 155806286 received");
        }
    }
}