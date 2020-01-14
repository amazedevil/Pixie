using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Services
{
    public interface IPXEnvironmentService
    {
        public string GetString(string key, string defaultValue = null);
        public int GetInt(string key, int defaultValue = 0);
    }
}
