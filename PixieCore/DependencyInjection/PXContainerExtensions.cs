using DryIoc;
using Pixie.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

public static class PXContainerExtensions
{
    public static PXLoggerService Logger(this IResolver resolver) {
        return resolver.Resolve<PXLoggerService>();
    }

    public static IPXInitialOptionsService InitialOptions(this IResolver resolver) {
        return resolver.Resolve<IPXInitialOptionsService>();
    }

    public static PXMiddlewareService Middlewares(this IResolver resolver) {
        return resolver.Resolve<PXMiddlewareService>();
    }

    public static IPXEnvironmentService Env(this IResolver resolver) {
        return resolver.Resolve<PXEnvironmentService>();
    }

    public static IPXClientService Client(this IResolver resolver) {
        return resolver.Resolve<IPXClientService>();
    }

    public static PXSchedulerService SchedulerService(this IResolver resolver) {
        return resolver.Resolve<PXSchedulerService>();
    }

    public static IPXMessageSenderService Sender(this IResolver resolver) {
        return resolver.Resolve<IPXMessageSenderService>();
    }
}