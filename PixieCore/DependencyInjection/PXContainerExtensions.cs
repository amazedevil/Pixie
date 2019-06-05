using DryIoc;
using Pixie.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.DependencyInjection {
    public static class PXContainerExtensions {
        public static IPXLoggerService Logger(this IContainer container) {
            return container.Resolve<IPXLoggerService>();
        }

        public static IPXInitialOptionsService InitialOptions(this IContainer container) {
            return container.Resolve<IPXInitialOptionsService>();
        }

        public static PXMiddlewareService Middlewares(this IContainer container) {
            return container.Resolve<PXMiddlewareService>();
        }

        public static PXEnvironmentService Env(this IContainer container) {
            return container.Resolve<PXEnvironmentService>();
        }

        public static IPXClientService Client(this IContainer container) {
            return container.Resolve<IPXClientService>();
        }

        public static PXSchedulerService SchedulerService(this IContainer container) {
            return container.Resolve<PXSchedulerService>();
        }
    }
}
