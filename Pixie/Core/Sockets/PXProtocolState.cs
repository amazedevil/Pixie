using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Sockets
{
    public enum PXProtocolState
    {
        None,
        Working,
        WaitingForConnection,
        Disposed
    }
}
