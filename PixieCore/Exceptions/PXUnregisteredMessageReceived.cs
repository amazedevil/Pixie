using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Exceptions
{
    public class PXUnregisteredMessageReceived : Exception
    {
        internal PXUnregisteredMessageReceived(int messageHash) : base($"Unregistered message with hash {messageHash} received") { }
    }
}
