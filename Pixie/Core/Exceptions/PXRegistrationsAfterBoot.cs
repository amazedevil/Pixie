using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Exceptions
{
    class PXRegistrationsAfterBoot : Exception
    {
        internal PXRegistrationsAfterBoot() : base($"Trying to register some resource after boot") { }
    }
}
