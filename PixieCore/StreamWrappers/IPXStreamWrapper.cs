using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pixie.Core.StreamWrappers
{
    public interface IPXStreamWrapper
    {
        Stream Wrap(Stream stream);
    }
}
