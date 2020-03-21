using Pixie.Core.Common.Streams;
using Pixie.Core.Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Pixie.Toolbox.Protocols
{
    //Protocol: 1 byte type,
    //for type 1 (data):
    //2 bytes message id
    //2 bytes content length
    //N bytes content
    //for type 2 (acknowledgment):
    //2 bytes message id
    class PXReliableDeliveryProtocol : IPXProtocol
    {
        private struct MessageData
        {
            public DateTime time;
            public byte[] data;
        }

        private class MessageTimeoutException : Exception {
        }

        private IPXProtocolContact contact;
        private Stream stream;

        private Dictionary<ushort, MessageData> messages = new Dictionary<ushort, MessageData>();

        private const byte MESSAGE_TYPE_DATA = 1;
        private const byte MESSAGE_TYPE_ACK = 2;

        private const ushort DEFAULT_MESSAGE_ID = 0;
        private const int MESSAGE_ACK_TIMEOUT = 5 * 1000;

        private ushort messageId = DEFAULT_MESSAGE_ID;

        private bool reconnect;

        public PXReliableDeliveryProtocol(bool reconnect = false) {
            this.reconnect = reconnect;
        }

        public void Initialize(IPXProtocolContact feedbackReceiver) {
            this.contact = feedbackReceiver;
        }

        public void SetupStreams(Stream stream) {
            this.stream = stream;

            ReadAsync();
        }

        public void Close() {
            this.reconnect = false;
        }

        private async void ReadAsync() {
            try {
                while (true) {
                    var reader = new PXBinaryReaderAsync(this.stream);

                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(MESSAGE_ACK_TIMEOUT);

                    try {
                        switch (await reader.ReadByte(cts.Token)) {
                            case MESSAGE_TYPE_DATA:
                                var id = await reader.ReadUInt16();
                                var length = await reader.ReadInt16();

                                OnMessageReceived(id, await reader.ReadBytes(length));
                                break;
                            case MESSAGE_TYPE_ACK:
                                OnAcknowledgementReceived(await reader.ReadUInt16());
                                break;
                        }

                        CheckMessagesTimeout();
                    } catch (OperationCanceledException) {
                        CheckMessagesTimeout();
                    }
                }
            } catch (MessageTimeoutException) {
                //possibly connection is already lost
            } catch (PXBinaryReaderAsync.EmptyStreamException) {
                //seems like connection is closed
            } catch (ObjectDisposedException) {
                //network stream seems to be closed, so we get this error,
                //we excpect it, so do nothing
            } catch (IOException) {
                //that happens sometimes, if user closes connection
            } catch (Exception e) {
                contact.ClientException(e);
            } finally {
                OnConnectionLost();
            }
        }

        private void OnConnectionLost() {
            this.stream.Close();

            if (reconnect) {
                contact.RequestReconnect();
            } else {
                contact.ClientDisconnected();
            }
        }

        private void CheckMessagesTimeout() {
            foreach (var message in messages) {
                if (DateTime.Now.Subtract(message.Value.time).TotalMilliseconds > MESSAGE_ACK_TIMEOUT) {
                    throw new MessageTimeoutException();
                }
            }
        }

        private void OnMessageReceived(ushort id, byte[] data) {
            SendAckMessage(id);
            this.contact.ReceivedMessage(data);
        }

        private void OnAcknowledgementReceived(ushort id) {
            messages.Remove(id);
        }

        public async void SendMessage(byte[] message) {
            if (messageId == ushort.MaxValue) {
                messageId = DEFAULT_MESSAGE_ID;
            }

            messageId++;

            messages[messageId] = new MessageData() {
                data = message,
                time = DateTime.Now
            };

            var writer = new PXBinaryWriterAsync(this.stream);
            writer.Write(MESSAGE_TYPE_DATA);
            writer.Write(messageId);
            writer.Write((short)message.Length);
            writer.Write(message);
            await writer.FlushAsync();
        }

        private async void SendAckMessage(ushort id) {
            var writer = new PXBinaryWriterAsync(this.stream);
            writer.Write(MESSAGE_TYPE_ACK);
            writer.Write(id);
            await writer.FlushAsync();
        }
    }
}
