using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Common.Concurrent
{
    internal class PXSerialActionQueue
    {
        private AsyncProducerConsumerQueue<Action> actions = new AsyncProducerConsumerQueue<Action>();

        public PXSerialActionQueue() {
            RunProcessing();
        }

        ~PXSerialActionQueue() {
            actions.CompleteAdding();
        }

        public async void RunProcessing() {
            try {
                while (true) {
                    (await actions.DequeueAsync()).Invoke();
                }
            } catch (InvalidOperationException) {
                //means nothing to dequeue
            }
        }

        public void Enqueue(Action action) {
            actions.Enqueue(action);
        }
    }
}
