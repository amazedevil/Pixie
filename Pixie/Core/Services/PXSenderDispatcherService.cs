using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Pixie.Core.Services
{
    public class PXSenderDispatcherService
    {
        public const int DEFAULT_SENDER_ID = 1;

        private IDictionary<int, IPXMessageSenderService> senders = new ConcurrentDictionary<int, IPXMessageSenderService>();

        internal PXSenderDispatcherService() { }

        internal void Register(IPXMessageSenderService sender) {
            senders[sender.SenderId] = sender;
        }

        internal void Unregister(IPXMessageSenderService sender) {
            senders.Remove(sender.SenderId);
        }

        public IPXMessageSenderService GetSender(int senderId) {
            return senders[senderId];
        }

        public IPXMessageSenderService GetDefaultSender() {
            return GetSender(DEFAULT_SENDER_ID);
        }
    }
}
