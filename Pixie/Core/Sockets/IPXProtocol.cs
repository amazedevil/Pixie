using System.IO;
using System.Threading.Tasks;

namespace Pixie.Core.Sockets
{
    public interface IPXProtocol
    {
        void Initialize(IPXProtocolContact contact);

        void SetupStream(Stream stream);

        Task StartReading();

        Task SendMessage(byte[] message);

        Task SendResponse(ushort id, byte[] response);

        Task<byte[]> SendRequestMessage(byte[] message);

        PXProtocolState GetState();

        void Dispose();
    }
}
