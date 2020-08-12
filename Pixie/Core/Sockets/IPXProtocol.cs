using System.IO;
using System.Threading.Tasks;

namespace Pixie.Core.Sockets
{
    public interface IPXProtocol
    {
        void Initialize(IPXProtocolContact contact);

        void SetupStream(Stream stream);

        void SendMessage(byte[] message);

        void SendResponse(ushort id, byte[] response);

        Task<byte[]> SendRequestMessage(byte[] message);
    }
}
