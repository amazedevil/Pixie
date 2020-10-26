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

        private IPXProtocolContact contact;
        private PXProtocolState state = PXProtocolState.None;

        private IDictionary<ushort, MessageData> messages = new ConcurrentDictionary<ushort, MessageData>();
        private IDictionary<ushort, MessageProcessedData> processing = new ConcurrentDictionary<ushort, MessageProcessedData>();

        private const byte MESSAGE_TYPE_DATA = 1;
        private const byte MESSAGE_TYPE_ACK = 2;
        private const byte MESSAGE_TYPE_RESPONSE = 3;

        private const byte FLAG_REQUEST = 1;

        private const float processedTimeoutSeconds = 10f;

        private ConcurrentCounter messageCounter = new ConcurrentCounter();

        private TaskCompletionSource<Stream> streamReadyTaskSource = new TaskCompletionSource<Stream>();

        public void Initialize(IPXProtocolContact feedbackReceiver) {
            this.contact = feedbackReceiver;
        }

        public void SetupStream(Stream stream) {
            state = PXProtocolState.Working;
            streamReadyTaskSource.SetResult(stream);
        }

        public async Task<byte[]> SendRequestMessage(byte[] message) {
            return await messages[await SendMessageInternal(message, true)].responseWaiter.Task;
        }

        public async Task StartReading() {
            while (true) {
                try {
                    var reader = new PXBinaryReaderAsync(await streamReadyTaskSource.Task);

                    switch (await reader.ReadByte()) {
                        case MESSAGE_TYPE_DATA: {
                                ushort id = await reader.ReadUInt16();
                                byte flags = await reader.ReadByte();
                                int length = await reader.ReadInt32();

                                await OnMessageReceived(
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
                                int length = await reader.ReadInt32();

                                OnResponseReceived(id, await reader.ReadBytes(length));
                            }
                            break;
                    }
                } catch (PXConnectionLostException) {
                    ReportOnConnectionLost();
                }

                RemoveOutdatedProcessing();
            }
        }

        private void RemoveOutdatedProcessing() {
            var currentTime = DateTime.Now;
            var timeDelta = TimeSpan.FromSeconds(processedTimeoutSeconds);
            processing = processing.Where(p => p.Value.time.Subtract(currentTime) < timeDelta).ToDictionary(v => v.Key, v => v.Value);
        }

        private async Task OnMessageReceived(ushort id, bool isRequest, byte[] data) {
            if (this.processing.ContainsKey(id)) { //duplicate message
                if (this.processing[id].responseData != null) {
                    await SendResponse(id, this.processing[id].responseData);
                } else {
                    await SendAckMessage(id);
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
                await SendAckMessage(id);
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

        public async Task SendMessage(byte[] message) {
            await SendMessageInternal(message, false);
        }

        private async Task<ushort> SendMessageInternal(MessageData message) {
            try {
                var writer = new PXBinaryWriterAsync(await streamReadyTaskSource.Task);
                writer.Write(MESSAGE_TYPE_DATA);
                writer.Write(message.id);
                writer.Write(message.responseWaiter != null ? FLAG_REQUEST : (byte)0);
                writer.Write(message.data.Length);
                writer.Write(message.data);
                await writer.FlushAsync();
            } catch (PXConnectionLostException) {
                ReportOnConnectionLost();

                await SendMessageInternal(message);
            }

            return message.id;
        }

        public async Task SendResponse(ushort messageId, byte[] response) {
            this.processing[messageId].UpdateResponse(response);

            try {
                var writer = new PXBinaryWriterAsync(await streamReadyTaskSource.Task);
                writer.Write(MESSAGE_TYPE_RESPONSE);
                writer.Write(messageId);
                writer.Write(response.Length);
                writer.Write(response);
                await writer.FlushAsync();
            } catch (PXConnectionLostException) {
                ReportOnConnectionLost();

                await SendResponse(messageId, response);
            }
        }

        public PXProtocolState GetState() {
            return state;
        }

        public void Dispose() {
            this.contact = null;

            if (!this.streamReadyTaskSource.Task.IsCompleted) {
                this.streamReadyTaskSource.SetException(new PXConnectionClosedLocalException());
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

        private async Task SendAckMessage(ushort id) {
            this.processing[id].UpdateResponse(null);

            try {
                var writer = new PXBinaryWriterAsync(await this.streamReadyTaskSource.Task);
                writer.Write(MESSAGE_TYPE_ACK);
                writer.Write(id);
                await writer.FlushAsync();
            } catch (PXConnectionLostException) {
                ReportOnConnectionLost();

                await SendAckMessage(id);
            }
        }

        private void ReportOnConnectionLost() {
            lock (this) {
                if (state == PXProtocolState.WaitingForConnection) {
                    return;
                }

                this.streamReadyTaskSource = new TaskCompletionSource<Stream>();
                state = PXProtocolState.WaitingForConnection;
                Task.Run(delegate { contact.OnProtocolStateChanged(); });
            }
        }
    }
}
