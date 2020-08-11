using System;
using System.Net.Sockets;
using Pixie.Core.Messages;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using Pixie.Core.Sockets;
using Pixie.Toolbox.Protocols;
using System.Threading.Tasks;

namespace PixieCoreTests.Client
{
    public class TestClient : IPXProtocolContact
    {
        public class Builder
        {
            private string host;
            private int port;
            private bool ssl = false;
            private Type[] eventTypes = new Type[] { };
            private Action<object> onMessageRecived = null;
            private Func<object, object> onMessageRequestRecived = null;

            private Builder() { }

            public Builder Host(string host) { this.host = host; return this; }
            public Builder Port(int port) { this.port = port; return this; }
            public Builder SslEnabled(bool ssl) { this.ssl = ssl; return this; }
            public Builder EventTypes(Type[] types) { this.eventTypes = types; return this; }
            public Builder OnMessageReceived(Action<object> onMessageRecived) { this.onMessageRecived = onMessageRecived; return this; }
            public Builder OnMessageRequestReceived(Func<object, object> onMessageRequestRecived) { this.onMessageRequestRecived = onMessageRequestRecived; return this; }

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
                    this.onMessageRecived,
                    this.onMessageRequestRecived
                );
            }
        }

        private TcpClient connection = null;
        private IPXProtocol protocol;
        private PXMessageEncoder encoder = null;
        private bool ssl = false;

        private Type[] eventTypes;
        private string host;
        private int port;
        private Action<object> onMessageRecived;
        private Func<object, object> onMessageRequestReceived;

        public TestClient(
            string host, 
            int port, 
            bool ssl, 
            Type[] eventTypes, 
            Action<object> onMessageRecived, 
            Func<object, object> onMessageRequestRecived, 
            IPXProtocol protocol = null
        ) {
            this.eventTypes = eventTypes;
            this.host = host != "0.0.0.0" ? host : "127.0.0.1";
            this.port = port;
            this.ssl = ssl;
            this.onMessageRecived = onMessageRecived;
            this.onMessageRequestReceived = onMessageRequestRecived;
            this.protocol = protocol ?? new PXReliableDeliveryProtocol();
            this.protocol.Initialize(this);
        }

        public void Run() {
            try {
                connection = new TcpClient(this.host, this.port);
                encoder = new PXMessageEncoder(eventTypes);

                protocol.SetupStreams(WrapSslIfNeeded(connection.GetStream()));
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
            connection?.Close();
        }

        public void SendMessage<A>(A message) where A : struct {
            this.protocol.SendMessage(this.encoder.EncodeMessage(message));
        }

        public async Task<R> SendRequest<A, R>(A message) where A : struct where R : struct {
            return (R)this.encoder.DecodeMessage(await this.protocol.SendRequestMessage(this.encoder.EncodeMessage(message)));
        }

        public void RequestReconnect() {
            //Not supposed to be used
        }

        public void ReceivedMessage(byte[] message) {
            this.onMessageRecived(this.encoder.DecodeMessage(message));
        }

        public void ReceivedRequestMessage(ushort id, byte[] message) {
            this.protocol.SendResponse(id, this.encoder.EncodeMessage(
                this.onMessageRequestReceived(this.encoder.DecodeMessage(message))
            ));
        }

        public void ClientDisconnected() {
            Console.WriteLine("Test client disconnected");
        }

        public void ClientException(Exception e) {
            throw e;
        }
    }
}