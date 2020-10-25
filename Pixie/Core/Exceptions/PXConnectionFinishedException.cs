using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Exceptions
{
    public class PXConnectionFinishedException : Exception
    {
        public PXConnectionFinishedException() : base("Client closed connection") { }
    }
}
