using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Exceptions
{
    public class PXConnectionClosedException : Exception
    {
        public PXConnectionClosedException() : base("Connection closed") { }
    }
}
