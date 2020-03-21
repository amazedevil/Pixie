using DryIoc;
using Pixie.Core.Common.Concurrent;
using System;

namespace Pixie.Core.Services
{
    public class PXLoggerService
    {
        [Flags]
        public enum LogLevel {
            None = 0,
            Error = 1,
            Info = 2,
            Debug = 4,
            Default = Error,
            All = Error | Info | Debug,
        }

        private IPXLogWriterService writer;
        private PXSerialActionQueue processingQueue = new PXSerialActionQueue();

        public LogLevel Level { set; get; }

        internal PXLoggerService(IContainer container) {
            this.writer = container.Resolve<IPXLogWriterService>(IfUnresolved.ReturnDefault);
            this.Level = LogLevel.Default;
        }

        public void Exception(Exception e) {
            Exception(delegate { return e; });
        }

        public void Exception(Func<Exception> ep) {
            if (Level.HasFlag(LogLevel.Error)) {
                EnqueueOperation(w => w.Exception(ep()));
            }
        }

        public void Error(string s) {
            Error(delegate { return s; });
        }

        public void Error(Func<string> sp) {
            if (Level.HasFlag(LogLevel.Error)) {
                EnqueueOperation(w => w.Error(sp()));
            }
        }

        public void Info(string s) {
            Info(delegate { return s; });
        }

        public void Info(Func<string> sp) {
            if (Level.HasFlag(LogLevel.Info)) {
                EnqueueOperation(w => w.Info(sp()));
            }
        }

        public void Debug(string s) {
            Debug(delegate { return s; });
        }

        public void Debug(Func<string> sp) {
            if (Level.HasFlag(LogLevel.Debug)) {
                EnqueueOperation(w => w.Debug(sp()));
            }
        }

        internal void WaitForQueueToFinish() {

        }

        private void EnqueueOperation(Action<IPXLogWriterService> action) {
            if (this.writer == null) {
                return;
            }

            processingQueue.Enqueue(delegate {
                action(this.writer);
            });
        }
    }
}
