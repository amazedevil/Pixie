using System;
using System.Net.Sockets;
using Pixie.Core.Messages;
using System.IO;

namespace PixieCoreTests.Client
{
    public class TestClient
    {
        private TcpClient connection = null;
        private Stream stream = null;
        private PXMessageReader reader = null;
        private PXMessageWriter writer = null;

        private Type[] eventTypes;
        private string host;
        private int port;
        private Action<object> onMessageRecived;

        public TestClient(string host, int port, Type[] eventTypes, Action<object> onMessageRecived) {
            this.eventTypes = eventTypes;
            this.host = host;
            this.port = port;
            this.onMessageRecived = onMessageRecived;
        }

        public void Run() {
            try {
                connection = new TcpClient(this.host, this.port);
                stream = connection.GetStream();
                reader = new PXMessageReader(stream, eventTypes);
                writer = new PXMessageWriter(stream);

                reader.OnDataAvailable += delegate (PXMessageReader r) {
                    while (r.HasMessages) {
                        ProcessMessage(r.DequeueMessage());
                    }
                };

                reader.StartReadingCycle();
            } catch (Exception e) {
                Dismiss();
                throw e;
            }
        }

        private void Dismiss() {
            reader = null;
            writer = null;
            stream?.Dispose();
            connection?.Close();
        }

        private void ProcessMessage(object message) {
            this.onMessageRecived(message);
        }

        public void SendMessage(object message) {
            writer.Send(message);
        }

    }
}