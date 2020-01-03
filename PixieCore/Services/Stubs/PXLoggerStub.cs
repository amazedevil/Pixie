using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Services.Stubs
{
    internal class PXLoggerStub : IPXLoggerService
    {
        public void Exception(Exception e) { }

        public void Info(string s) { }
    }
}
