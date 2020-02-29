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

    public static PXMiddlewareService Middlewares(this IResolver resolver) {
        return resolver.Resolve<PXMiddlewareService>();
    }

    public static IPXClientService Client(this IResolver resolver) {
        return resolver.Resolve<IPXClientService>();
    }

    public static PXSchedulerService Scheduler(this IResolver resolver) {
        return resolver.Resolve<PXSchedulerService>();
    }

    public static IPXMessageSenderService Sender(this IResolver resolver) {
        return resolver.SenderDispatcher().GetDefaultSender();
    }

    public static PXSenderDispatcherService SenderDispatcher(this IResolver resolver) {
        return resolver.Resolve<PXSenderDispatcherService>();
    }

    public static IPXMessageHandlerService Handlers(this IResolver resolver) {
        return resolver.Resolve<IPXMessageHandlerService>();
    }

    public static IPXStreamWrapperService StreamWrappers(this IResolver resolver) {
        return resolver.Resolve<IPXStreamWrapperService>();
    }

    public static PXEndpointService Endpoints(this IResolver resolver) {
        return resolver.Resolve<PXEndpointService>();
    }

    //internal

    internal static PXErrorHandlingService Errors(this IResolver resolver) {
        return resolver.Resolve<PXErrorHandlingService>();
    }
}