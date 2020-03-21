using System.IO;

namespace Pixie.Core.Sockets
{
    public interface IPXProtocol
    {
        void Initialize(IPXProtocolContact contact);

        void SetupStreams(Stream stream);

        void SendMessage(byte[] message);
    }
}
