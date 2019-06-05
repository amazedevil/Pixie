using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Pixie.Core.Messages {
    internal class PXMessageWriter {

        private NetworkStream stream;

        public PXMessageWriter(NetworkStream stream) {
            this.stream = stream;
        }

        public void Send(object message) {
            JObject obj = JObject.FromObject(new {
                message = message.GetType().GetField(PXMessageInfo.MESSAGE_CLASS_FIELD_NAME).GetValue(null) as string,
                body = message
            });

            string objAsString = obj.ToString(Newtonsoft.Json.Formatting.None);

            byte[] buffer = new byte[Encoding.UTF8.GetByteCount(objAsString) + 1];
            Encoding.UTF8.GetBytes(objAsString, 0, objAsString.Length, buffer, 0);
            buffer[objAsString.Length] = 0;

            stream.Write(buffer, 0, buffer.Length);
        }

    }
}
