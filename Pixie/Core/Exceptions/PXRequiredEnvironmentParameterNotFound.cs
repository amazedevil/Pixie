using System;

namespace Pixie.Core.Exceptions
{
    public class PXRequiredEnvironmentParameterNotFound : Exception
    {
        internal PXRequiredEnvironmentParameterNotFound(string name) : base($"Required environment parameter {name} not found") { }
    }
}
