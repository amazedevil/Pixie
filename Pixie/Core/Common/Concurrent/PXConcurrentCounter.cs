using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Common.Concurrent
{
    internal class PXConcurrentCounter
    {
        private const ushort DEFAULT_VALUE = 1;

        private ushort counter = DEFAULT_VALUE;

        public ushort Pop() {
            lock(this) {
                counter++;

                if (counter == ushort.MaxValue) {
                    counter = DEFAULT_VALUE;
                }

                return counter;
            }
        }
    }
}
