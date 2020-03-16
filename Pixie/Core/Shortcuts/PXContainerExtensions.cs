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

    public static IPXClientService Client(this IResolver resolver) {
        return resolver.Resolve<IPXClientService>();
    }

    public static PXSchedulerService Scheduler(this IResolver resolver) {
        return resolver.Resolve<PXSchedulerService>();
    }

    public static IPXMessageSenderService Sender(this IResolver resolver) {
        return resolver.SenderDispatcher().GetDefaultSender();
    }

    public static IPXMessageSenderService AgentSender(this IResolver resolver) {
        return resolver.SenderDispatcher().GetDefaultAgentSender();
    }

    public static PXSenderDispatcherService SenderDispatcher(this IResolver resolver) {
        return resolver.Resolve<PXSenderDispatcherService>();
    }

    public static PXEndpointService Endpoints(this IResolver resolver) {
        return resolver.Resolve<PXEndpointService>();
    }

    public static PXAgentService Agents(this IResolver resolver) {
        return resolver.Resolve<PXAgentService>();
    }

    public static PXHandlerService Handlers(this IResolver resolver) {
        return resolver.Resolve<PXHandlerService>();
    }

    //internal

    internal static PXErrorHandlingService Errors(this IResolver resolver) {
        return resolver.Resolve<PXErrorHandlingService>();
    }
}