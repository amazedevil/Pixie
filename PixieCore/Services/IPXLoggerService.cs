using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Services
{
    public interface IPXLoggerService
    {
        void Info(string s);
        void Exception(Exception e);
    }
}
