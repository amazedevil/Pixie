using Pixie.Core.StreamWrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pixie.Core.Services
{
    internal class PXStreamWrapperService
    {
        IPXStreamWrapper[] wrappers;

        public PXStreamWrapperService() { }

        public void SetupWrappers(IPXStreamWrapper[] wrappers) {
            this.wrappers = wrappers;
        }

        public Stream WrapStream(Stream stream) {
            var result = stream;
            
            foreach (var wrapper in wrappers) {
                result = wrapper.Wrap(result);
            }

            return result;
        }
    }
}
