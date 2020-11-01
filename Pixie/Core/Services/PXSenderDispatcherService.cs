using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Pixie.Core.Services
{
    public class PXSenderDispatcherService
    {
        public class SenderNotFoundException : Exception {
            public SenderNotFoundException(int senderId) : base($"Sender with id \"${senderId}\" not found") { }
        }

        public class DefaultSenderNotFoundException : Exception {
            public DefaultSenderNotFoundException() : base("Default sender not found: probably no server is running") { }
        }

        public const int DEFAULT_SERVER_SENDER_ID = 1;
        public const int DEFAULT_AGENT_SENDER_ID = 2;

        private IDictionary<int, IPXMessageSenderService> senders = new ConcurrentDictionary<int, IPXMessageSenderService>();

        internal PXSenderDispatcherService() { }

        internal void Register(IPXMessageSenderService sender) {
            senders[sender.SenderId] = sender;
        }

        internal void Unregister(IPXMessageSenderService sender) {
            senders.Remove(sender.SenderId);
        }

        public IPXMessageSenderService GetSender(int senderId) {
            try {
                return senders[senderId];
            } catch (KeyNotFoundException) {
                throw new SenderNotFoundException(senderId);
            }
        }

        public IPXMessageSenderService GetDefaultSender() {
            try {
                return GetSender(DEFAULT_SERVER_SENDER_ID);
            } catch (SenderNotFoundException) {
                throw new DefaultSenderNotFoundException();
            }
        }

        public IPXMessageSenderService GetDefaultAgentSender() {
            return GetSender(DEFAULT_AGENT_SENDER_ID);
        }
    }
}
