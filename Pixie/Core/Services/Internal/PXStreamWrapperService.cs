using Pixie.Core.StreamWrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pixie.Core.Services.Internal
{
    internal class PXStreamWrapperService : IPXStreamWrapperService
    {
        private List<IPXStreamWrapper> wrappers = new List<IPXStreamWrapper>();

        public PXStreamWrapperService() { }

        public void AddWrapper(IPXStreamWrapper wrapper) {
            this.wrappers.Add(wrapper);
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
