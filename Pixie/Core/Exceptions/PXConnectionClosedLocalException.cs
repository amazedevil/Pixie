using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Exceptions
{
    public class PXConnectionClosedLocalException : Exception
    {
        public PXConnectionClosedLocalException() : base("Connection closed localy") { }
    }
}
