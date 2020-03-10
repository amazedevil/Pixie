using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Exceptions
{
    public class PXJobBaseTypeInvalid : Exception
    {
        internal PXJobBaseTypeInvalid(Type t) : base($"Job should be inherited from PXJobBase, type {t.Name} doesn't") {}
    }
}
