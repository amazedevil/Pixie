using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Services
{
    public interface IPXEnvironmentService
    {
        public string GetString(string key, Func<string> defaultValueProvider);
        public int GetInt(string key, Func<int> defaultValueProvider);
    }
}
