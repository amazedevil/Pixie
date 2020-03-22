using Pixie.Core.Common.Streams;
using Pixie.Core.Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pixie.Toolbox.Protocols
{
    //Protocol: 1 byte type,
    //for type 1 (data):
    //2 bytes message id
    //1 byte flags
    //2 bytes content length
    //N bytes content
    //for type 2 (acknowledgment):
    //2 bytes message id
    //for type 3 (response)
    //2 bytes message id
    //2 bytes content length
    //N bytes content
    class PXReliableDeliveryProtocol : IPXProtocol
    {
        private struct MessageData
        {
            public DateTime time;
            public byte[] data;
            public TaskCompletionSource<byte[]> responseWaiter;
        }

        private class MessageTimeoutException : Exception {
        }

        private IPXProtocolContact contact;
        private Stream stream;

        private Dictionary<ushort, MessageData> messages = new Dictionary<ushort, MessageData>();

        private const byte MESSAGE_TYPE_DATA = 1;
        private const byte MESSAGE_TYPE_ACK = 2;
        private const byte MESSAGE_TYPE_RESPONSE = 3;

        private const ushort DEFAULT_MESSAGE_ID = 0;
        private const int MESSAGE_ACK_TIMEOUT = 5 * 1000;

        private const byte FLAG_REQUEST = 1;

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

            foreach (var m in messages) {
                _ = SendMessageInternal(m.Value);
            }

            ReadAsync();
        }

        public async void SendResponse(ushort messageId, byte[] response) {
            var writer = new PXBinaryWriterAsync(this.stream);
            writer.Write(MESSAGE_TYPE_RESPONSE);
            writer.Write(messageId);
            writer.Write((short)response.Length);
            writer.Write(response);
            await writer.FlushAsync();
        }

        public async Task<byte[]> SendRequestMessage(byte[] message) {
            return await messages[await SendMessageInternal(message, true)].responseWaiter.Task;
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
                            case MESSAGE_TYPE_DATA: {
                                    ushort id = await reader.ReadUInt16();
                                    byte flags = await reader.ReadByte();
                                    short length = await reader.ReadInt16();

                                    OnMessageReceived(
                                        id,
                                        (flags & FLAG_REQUEST) != 0,
                                        await reader.ReadBytes(length)
                                    );
                                }
                                break;
                            case MESSAGE_TYPE_ACK:
                                OnAcknowledgementReceived(await reader.ReadUInt16());
                                break;
                            case MESSAGE_TYPE_RESPONSE: {
                                    ushort id = await reader.ReadUInt16();
                                    short length = await reader.ReadInt16();

                                    OnResponseReceived(id, await reader.ReadBytes(length));
                                }
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

        private void OnMessageReceived(ushort id, bool isRequest, byte[] data) {
            if (!isRequest) {
                SendAckMessage(id);
                this.contact.ReceivedMessage(data);
            } else {
                this.contact.ReceivedRequestMessage(id, data);
            }
        }

        private void OnAcknowledgementReceived(ushort id) {
            if (!messages.ContainsKey(id)) {
                return;
            }

            messages.Remove(id);
        }

        private void OnResponseReceived(ushort id, byte[] data) {
            if (!messages.ContainsKey(id)) {
                return;
            }

            var message = messages[id];
            messages.Remove(id);

            message.responseWaiter.SetResult(data);
        }

        public void SendMessage(byte[] message) {
            _ = SendMessageInternal(message, false);
        }

        private async Task<ushort> SendMessageInternal(MessageData message) {
            var writer = new PXBinaryWriterAsync(this.stream);
            writer.Write(MESSAGE_TYPE_DATA);
            writer.Write(messageId);
            writer.Write(message.responseWaiter != null ? FLAG_REQUEST : (byte)0);
            writer.Write((short)message.data.Length);
            writer.Write(message.data);
            await writer.FlushAsync();

            return messageId;
        }

        private async Task<ushort> SendMessageInternal(byte[] message, bool isRequest) {
            if (messageId == ushort.MaxValue) {
                messageId = DEFAULT_MESSAGE_ID;
            }

            messageId++;

            messages[messageId] = new MessageData() {
                data = message,
                time = DateTime.Now,
                responseWaiter = isRequest ? new TaskCompletionSource<byte[]>() : null
            };

            return await SendMessageInternal(messages[messageId]);
        }

        private async void SendAckMessage(ushort id) {
            var writer = new PXBinaryWriterAsync(this.stream);
            writer.Write(MESSAGE_TYPE_ACK);
            writer.Write(id);
            await writer.FlushAsync();
        }
    }
}
