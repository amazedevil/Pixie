using System;
using System.Net.Sockets;
using Pixie.Core.Messages;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

namespace PixieCoreTests.Client
{
    public class TestClient
    {
        public class Builder
        {
            private string host;
            private int port;
            private bool ssl = false;
            private Type[] eventTypes = new Type[] { };
            private Action<object> onMessageRecived = null;

            private Builder() { }

            public Builder Host(string host) { this.host = host; return this; }
            public Builder Port(int port) { this.port = port; return this; }
            public Builder SslEnabled(bool ssl) { this.ssl = ssl; return this; }
            public Builder EventTypes(Type[] types) { this.eventTypes = types; return this; }
            public Builder OnMessageReceived(Action<object> onMessageRecived) { this.onMessageRecived = onMessageRecived; return this; }

            //required parameters
            public static Builder Create(string host, int port) {
                return new Builder().Host(host).Port(port);
            }

            public TestClient Build() {
                return new TestClient(
                    this.host, 
                    this.port, 
                    this.ssl, 
                    this.eventTypes, 
                    this.onMessageRecived
                );
            }
        }

        private TcpClient connection = null;
        private Stream stream = null;
        private PXMessageReader reader = null;
        private PXMessageWriter writer = null;
        private bool ssl = false;

        private Type[] eventTypes;
        private string host;
        private int port;
        private Action<object> onMessageRecived;

        public TestClient(string host, int port, bool ssl, Type[] eventTypes, Action<object> onMessageRecived) {
            this.eventTypes = eventTypes;
            this.host = host;
            this.port = port;
            this.ssl = ssl;
            this.onMessageRecived = onMessageRecived;
        }

        public void Run() {
            try {
                connection = new TcpClient(this.host, this.port);
                stream = WrapSslIfNeeded(connection.GetStream());
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

        public void Stop() {
            connection.Close();
        }

        private static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors) {
            return true;
        }

        private Stream WrapSslIfNeeded(Stream underlyingStream) {
            if (!ssl) {
                return underlyingStream;
            }

            SslStream sslStream = new SslStream(
                underlyingStream,
                false,
                new RemoteCertificateValidationCallback(ValidateServerCertificate),
                null
            );

            // The server name must match the name on the server certificate.
            try {
                sslStream.AuthenticateAsClient(this.host);
            } catch (AuthenticationException e) {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null) {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                this.connection.Close();

                throw e;
            }

            return sslStream;
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