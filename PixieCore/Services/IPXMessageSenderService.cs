using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Services {
    public interface IPXMessageSenderService {
        void Send(ICollection<string> clientIds, object data);
    }
}
