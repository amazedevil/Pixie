using Pixie.Core.StreamWrappers;
using System.IO;

namespace Pixie.Core.Services
{
    public interface IPXStreamWrapperService
    {
        void AddWrapper(IPXStreamWrapper wrapper);
        Stream WrapStream(Stream stream);
    }
}
