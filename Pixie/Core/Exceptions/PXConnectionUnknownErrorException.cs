using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Exceptions
{
    class PXConnectionUnknownErrorException : Exception
    {
        public PXConnectionUnknownErrorException(Exception inner) : base("Unknown connection error", inner) { }
    }
}
