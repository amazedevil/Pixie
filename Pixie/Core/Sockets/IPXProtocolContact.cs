using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Sockets
{
    public interface IPXProtocolContact
    {
        void RequestReconnect();

        void ReceivedMessage(byte[] message);

        void ClientDisconnected();

        void ClientException(Exception e);
    }
}
