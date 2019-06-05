﻿using Newtonsoft.Json.Linq;
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

        private Dictionary<string, Type> messageTypes;

        public event Action<PXMessageReader> OnDataAvailable;
        public event Action<PXMessageReader> OnStreamClose;

        private byte[] buffer = new byte[READ_BUFFER_SIZE];

        public bool HasMessages
        {
            get { return messages.Count > 0; }
        }

        public PXMessageReader(NetworkStream stream, Type[] messageTypes) {
            this.stream = stream;

            this.messageTypes = new Dictionary<string, Type>();
            foreach (var t in messageTypes) {
                this.messageTypes[(string)t.GetField(PXMessageInfo.MESSAGE_CLASS_FIELD_NAME).GetValue(null)] = t;
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
                            CommandFinished();
                        } else {
                            accumulator.Add(buffer[i]);
                        }
                    }

                    if (messages.Count > 0) {
                        OnDataAvailable?.Invoke(this);
                    }
                }
            } finally {
                OnStreamClose?.Invoke(this);
            }
        }

        public object DequeueMessage() {
            return messages.Dequeue();
        }

        private void CommandFinished() {
            var obj = JObject.Parse(Encoding.UTF8.GetString(this.accumulator.ToArray()));

            messages.Enqueue(CreateMessage(
                obj[PXMessageInfo.MESSAGE_SERIALIZATION_FIELD_NAME].ToString(),
                obj[PXMessageInfo.MESSAGE_SERIALIZATION_FIELD_BODY] as JObject
            ));

            accumulator.Clear();
        }

        private object CreateMessage(string name, JObject body) {
            return body.ToObject(messageTypes[name]);
        }
    }
}
