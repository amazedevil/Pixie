using Pixie.Core.Common.Streams;
using Pixie.Core.Exceptions;
using Pixie.Core.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
            public ushort id;
            public DateTime time;
            public byte[] data;
            public TaskCompletionSource<byte[]> responseWaiter;
        }

        private struct MessageProcessedData
        {
            public ushort id;
            public byte[] responseData;
            public DateTime time;
            public bool isSent;

            public void UpdateResponse(byte[] data) {
                this.responseData = data;
                time = DateTime.Now;
                isSent = true;
            }
        }

        private class ConcurrentCounter
        {
            private const ushort DEFAULT_VALUE = 1;

            private ushort counter = DEFAULT_VALUE;

            public ushort Pop() {
                lock (this) {
                    counter++;

                    if (counter == ushort.MaxValue) {
                        counter = DEFAULT_VALUE;
                    }

                    return counter;
                }
            }
        }

        private class MessageTimeoutException : Exception {
        }

        private class ConnectionErrorException : Exception {
        }

        private IPXProtocolContact contact;

        private IDictionary<ushort, MessageData> messages = new ConcurrentDictionary<ushort, MessageData>();
        private IDictionary<ushort, MessageProcessedData> processing = new ConcurrentDictionary<ushort, MessageProcessedData>();

        private const byte MESSAGE_TYPE_DATA = 1;
        private const byte MESSAGE_TYPE_ACK = 2;
        private const byte MESSAGE_TYPE_RESPONSE = 3;

        private const int MESSAGE_ACK_TIMEOUT = 5 * 1000;

        private const byte FLAG_REQUEST = 1;

        private const float processedTimeoutSeconds = 10f;

        private ConcurrentCounter messageCounter = new ConcurrentCounter();

        private TaskCompletionSource<Stream> streamReadyTaskSource = new TaskCompletionSource<Stream>();

        public void Initialize(IPXProtocolContact feedbackReceiver) {
            this.contact = feedbackReceiver;
        }

        public void SetupStream(Stream stream) {
            foreach (var message in messages) {
                _ = SendMessageInternal(message.Value);
            }

            streamReadyTaskSource.SetResult(stream);

            ReadMessages();
        }

        public async Task<byte[]> SendRequestMessage(byte[] message) {
            return await messages[await SendMessageInternal(message, true)].responseWaiter.Task;
        }

        private async void ReadMessages() {
            try {
                await TransformNetworkExceptions(async delegate {
                    try {
                        while (true) {
                            var reader = new PXBinaryReaderAsync(await streamReadyTaskSource.Task);

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

                            RemoveOutdatedProcessing();
                        }
                    } catch (MessageTimeoutException) {
                        //possibly connection lost
                        throw new PXConnectionLostException();
                    }
                });
            } catch (PXConnectionLostException) {
                OnConnectionLost(); //when we reconnect, we'll send message one more time
            } catch (PXConnectionClosedException) {
                OnConnectionClosed();
            } catch (Exception e) {
                this.contact.OnClientError(e);
            }
        }

        private void OnConnectionLost() {
            this.streamReadyTaskSource.Task.GetAwaiter().GetResult().Close();
            this.streamReadyTaskSource = new TaskCompletionSource<Stream>();

            contact.RequestReconnect();
        }

        private void OnConnectionClosed() {
            contact.ClientDisconnected();
        }

        private void RemoveOutdatedProcessing() {
            var currentTime = DateTime.Now;
            var timeDelta = TimeSpan.FromSeconds(processedTimeoutSeconds);
            processing = processing.Where(p => p.Value.time.Subtract(currentTime) < timeDelta).ToDictionary(v => v.Key, v => v.Value);
        }

        private void CheckMessagesTimeout() {
            foreach (var message in messages) {
                if (DateTime.Now.Subtract(message.Value.time).TotalMilliseconds > MESSAGE_ACK_TIMEOUT) {
                    throw new MessageTimeoutException();
                }
            }
        }

        private void OnMessageReceived(ushort id, bool isRequest, byte[] data) {
            if (this.processing.ContainsKey(id)) { //duplicate message
                if (this.processing[id].responseData != null) {
                    SendResponse(id, this.processing[id].responseData);
                } else {
                    SendAckMessage(id);
                }

                return;
            }

            this.processing[id] = new MessageProcessedData() {
                id = id,
                responseData = null,
                time = DateTime.Now,
                isSent = false,
            };

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
            try {
                await TransformNetworkExceptions(async delegate {
                    var writer = new PXBinaryWriterAsync(await streamReadyTaskSource.Task);
                    writer.Write(MESSAGE_TYPE_DATA);
                    writer.Write(message.id);
                    writer.Write(message.responseWaiter != null ? FLAG_REQUEST : (byte)0);
                    writer.Write((short)message.data.Length);
                    writer.Write(message.data);
                    await writer.FlushAsync();
                });
            } catch (ConnectionErrorException) {
                OnConnectionLost(); //when we reconnect, we'll send message again
            } catch (Exception e) {
                this.contact.OnClientError(e);
            }

            return message.id;
        }

        public async void SendResponse(ushort messageId, byte[] response) {
            this.processing[messageId].UpdateResponse(response);

            try {
                await TransformNetworkExceptions(async delegate {
                    var writer = new PXBinaryWriterAsync(await streamReadyTaskSource.Task);
                    writer.Write(MESSAGE_TYPE_RESPONSE);
                    writer.Write(messageId);
                    writer.Write((short)response.Length);
                    writer.Write(response);
                    await writer.FlushAsync();
                });
            } catch (ConnectionErrorException) {
                OnConnectionLost(); //when we reconnect, we'll send message again
            }
        }

        private async Task<ushort> SendMessageInternal(byte[] message, bool isRequest) {
            var messageId = messageCounter.Pop();

            var messageObj = new MessageData() {
                id = messageId,
                data = message,
                time = DateTime.Now,
                responseWaiter = isRequest ? new TaskCompletionSource<byte[]>() : null
            };

            messages[messageId] = messageObj;

            return await SendMessageInternal(messageObj);
        }

        private async void SendAckMessage(ushort id) {
            this.processing[id].UpdateResponse(null);

            try {
                await TransformNetworkExceptions(async delegate {
                    var writer = new PXBinaryWriterAsync(await this.streamReadyTaskSource.Task);
                    writer.Write(MESSAGE_TYPE_ACK);
                    writer.Write(id);
                    await writer.FlushAsync();
                });
            } catch (ConnectionErrorException) {
                OnConnectionLost(); //when we reconnect, we'll send message again
            }
        }

        private async Task TransformNetworkExceptions(Func<Task> action) {
            try {
                await action();
            } catch (PXBinaryReaderAsync.EmptyStreamException) {
                //seems like connection is closed
                throw new PXConnectionClosedException();
            } catch (ObjectDisposedException) {
                //network stream seems to be closed, so we get this error,
                //we excpect it, so do nothing
                throw new PXConnectionLostException();
            } catch (IOException) {
                //that happens sometimes, if user closes connection
                throw new PXConnectionLostException();
            }
        }
    }
}
