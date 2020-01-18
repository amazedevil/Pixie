using System;
using System.Collections.Generic;
using System.Text;

namespace PixieTests.Common
{
    internal static class PortProvider
    {
        private static int counter = 7777;

        public static int ProviderPort() {
            return counter++;
        }
    }
}
