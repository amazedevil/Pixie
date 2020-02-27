using DryIoc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Services
{
    public class PXErrorHandlingService
    {
        public enum Scope
        {
            None,
            SocketClientMessage,
            SocketClient,
            Job,
            CliCommand,
            CliServer,
            SocketServer,
            PixieServer
        }

        private PXLoggerService logger;

        public PXErrorHandlingService(IContainer container) {
            this.logger = container.Resolve<PXLoggerService>();
        }

        public virtual void Handle(Exception e, Scope scope) {
            LogException(e);
        }

        private void LogException(Exception e) {
            this.logger.Exception(e);
        }
    }
}
