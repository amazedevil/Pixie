using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Services {
    public interface IPXMessageSenderService {
        void Send(IEnumerable<string> clientIds, object data);
    }
}
