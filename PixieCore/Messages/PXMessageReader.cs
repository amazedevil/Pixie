using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Pixie.Core.Messages {
    public class PXMessageReader {
        private const int READ_BUFFER_SIZE = 128;

        private NetworkStream stream;
        private List<byte> accumulator = new List<byte>();
        private Queue<object> messages = new Queue<object>();

        private Dictionary<int, Type> messageTypes;

        public event Action<PXMessageReader> OnDataAvailable;
        public event Action<PXMessageReader> OnStreamClose;
        public event Action<PXMessageReader, Exception> OnStreamError;

        private byte[] buffer = new byte[READ_BUFFER_SIZE];

        public bool HasMessages
        {
            get { return messages.Count > 0; }
        }

        public PXMessageReader(NetworkStream stream, Type[] messageTypes) {
            this.stream = stream;

            this.messageTypes = new Dictionary<int, Type>();
            foreach (var t in messageTypes) {
                this.messageTypes[PXMessageInfo.GetMessageTypeHashCode(t)] = t;
            }
        }

        public void StartReadingCycle() {
            StartReadingCycleInternal();
        }

        private async void StartReadingCycleInternal() {
            try {
                while (true) {
                    var readCount = await this.stream.ReadAsync(this.buffer, 0, this.buffer.Length);

                    if (readCount == 0) {
                        return;
                    }

                    for (int i = 0; i < readCount; i++) {
                        if (buffer[i] == 0) {
                            MessageFinished();
                        } else {
                            accumulator.Add(buffer[i]);
                        }
                    }

                    if (messages.Count > 0) {
                        OnDataAvailable?.Invoke(this);
                    }
                }
            } catch (ObjectDisposedException) {
                //network stream seems to be closed, so we get this error,
                //we excpect it, so do nothing
            } catch (System.IO.IOException) {
                //that happens sometimes, if user closes connection
            } catch (Exception e) {
                OnStreamError?.Invoke(this, e);
            } finally {
                OnStreamClose?.Invoke(this);
            }
        }

        public object DequeueMessage() {
            return messages.Dequeue();
        }

        private void MessageFinished() {
            var obj = JObject.Parse(Encoding.UTF8.GetString(this.accumulator.ToArray()));

            messages.Enqueue(CreateMessage(
                obj[PXMessageInfo.MESSAGE_SERIALIZATION_FIELD_NAME].Value<int>(),
                obj[PXMessageInfo.MESSAGE_SERIALIZATION_FIELD_BODY] as JObject
            ));

            accumulator.Clear();
        }

        private object CreateMessage(int hash, JObject body) {
            return body.ToObject(messageTypes[hash]);
        }
    }
}
