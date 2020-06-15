using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Pixie.Core.Services
{
    public interface IPXMessageSenderService
    {
        int SenderId { get; }
        void Send(IEnumerable<string> clientIds, object data);
        Task<object> SendRequest(string clientId, object data);
        IEnumerable<string> GetClientIds();
    }
}
