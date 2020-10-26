using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Exceptions
{
    public class PXConnectionClosedRemoteException : Exception
    {
        public PXConnectionClosedRemoteException() : base("Connection closed remotely") { }
    }
}
