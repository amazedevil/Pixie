using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Exceptions
{
    public class PXRegistrationOutOfTime : Exception
    {
        internal PXRegistrationOutOfTime() : base("Trying to register some resource out of registration phase") { }
    }
}
