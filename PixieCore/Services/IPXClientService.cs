using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Services {
    public interface IPXClientService {
        string Id { get; }
        void Subscribe(int subscriptionId);
        void Unsubscribe(int subscriptionId);
    }
}
