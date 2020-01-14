using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Services
{
    public interface IPXInitialOptionsService
    {
        int Port { get; }
        bool Debug { get; }
        string Host { get; }
    }
}
