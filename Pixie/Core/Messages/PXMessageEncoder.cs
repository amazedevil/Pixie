using Newtonsoft.Json.Linq;
using Pixie.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Pixie.Core.Messages
{
    internal class PXMessageEncoder
    {
        private Dictionary<int, Type> messageTypes;

        public PXMessageEncoder(IEnumerable<Type> messageTypes) {
            this.messageTypes = new Dictionary<int, Type>();
            foreach (var t in messageTypes) {
                this.messageTypes[PXMessageInfo.GetMessageTypeHashCode(t)] = t;
            }
        }

        public object DecodeMessage(byte[] data) {
            var obj = JObject.Parse(Encoding.UTF8.GetString(data));
            var hash = obj[PXMessageInfo.MESSAGE_SERIALIZATION_FIELD_NAME].Value<int>();

            if (!messageTypes.ContainsKey(hash)) {
                throw new PXUnregisteredMessageReceived(hash);
            }

            return (obj[PXMessageInfo.MESSAGE_SERIALIZATION_FIELD_BODY] as JObject).ToObject(messageTypes[hash]);
        }

        public byte[] EncodeMessage(object message) {
            JObject obj = JObject.FromObject(new Dictionary<string, object> {
                { PXMessageInfo.MESSAGE_SERIALIZATION_FIELD_NAME, PXMessageInfo.GetMessageTypeHashCode(message.GetType()) },
                { PXMessageInfo.MESSAGE_SERIALIZATION_FIELD_BODY, message },
            });

            string objAsString = obj.ToString(Newtonsoft.Json.Formatting.None);

            byte[] buffer = new byte[Encoding.UTF8.GetByteCount(objAsString)];
            Encoding.UTF8.GetBytes(objAsString, 0, objAsString.Length, buffer, 0);

            return buffer;
        }
    }
}
