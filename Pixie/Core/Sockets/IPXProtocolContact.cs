using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Sockets
{
    public interface IPXProtocolContact
    {
        void ReceivedMessage(byte[] message);

        void ReceivedRequestMessage(ushort id, byte[] message);

        void OnProtocolStateChanged();
    }
}
