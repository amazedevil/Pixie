using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Pixie.Core.Services
{
    public interface IPXMessageSenderService
    {
        int SenderId { get; }
        Task Send<M>(IEnumerable<string> clientIds, M data) where M: struct;
        Task<R> SendRequest<M, R>(string clientId, M data) where M: struct where R: struct;
        IEnumerable<string> GetClientIds();
    }
}
