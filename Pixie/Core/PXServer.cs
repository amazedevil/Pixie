using DryIoc;
using Pixie.Core.Cli;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services.Internal;
using Pixie.Core.Services;
using System;
using System.Threading.Tasks;

namespace Pixie.Core
{
    public class PXServer
    {
        private Container container;
        private Task endpointsProcessingTask = null;

        protected virtual IPXServiceProvider[] GetServiceProviders() {
            return new IPXServiceProvider[] { };
        }

        public PXServer() {
            this.container = CreateContainer();
        }

        public void StartAsync() {
            Start();
        }

        public void StartSync() {
            Start();
            WaitForEndpointsToStop();
        }

        public void Start() {
            try {
                foreach (var module in GetServiceProviders()) {
                    module.OnRegister(this.container);
                }

                CloseRegistrations();

                foreach (var module in GetServiceProviders()) {
                    module.OnInitialize(this.container);
                }

                StartScheduler();

                StartEndpoints();
            } catch (Exception ex) {
                this.container.Errors().Handle(ex, PXErrorHandlingService.Scope.PixieServer);
            }
        }

        public void WaitForEndpointsToStop() {
            this.endpointsProcessingTask?.Wait();
        }

        private void CloseRegistrations() {
            if (this.container.Handlers() is PXMessageHandlerService messageHandlerService) {
                messageHandlerService.CloseRegistration();
            }

            if (this.container.Endpoints() is PXEndpointService endpointService) {
                endpointService.CloseRegistration();
            }
        }

        private Container CreateContainer() {
            var container = new Container();

            container.Use(this);

            container.RegisterDelegate(r => new PXSchedulerService(r.Resolve<IContainer>()), Reuse.Singleton);
            container.RegisterDelegate(r => new PXEndpointService(r.Resolve<IContainer>()), Reuse.Singleton);
            container.RegisterDelegate(r => new PXSenderDispatcherService(), Reuse.Singleton);
            container.Register<PXMiddlewareService>(Reuse.Singleton);
            container.Register<IPXStreamWrapperService, PXStreamWrapperService>(Reuse.Singleton);
            container.Register<PXErrorHandlingService>();
            container.RegisterDelegate<IPXEnvironmentService>(r => new PXEnvironmentService());
            container.RegisterDelegate(r => new PXLoggerService(r.Resolve<IContainer>()));
            container.RegisterDelegate<IPXMessageHandlerService>(_ => new PXMessageHandlerService(), Reuse.Singleton);

            return container;
        }

        private void StartScheduler() {
            this.container.Logger().Info("Starting scheduler");

            container.Resolve<PXSchedulerService>().Launch();
        }

        private async void StartEndpoints() {
            this.container.Endpoints().BuildEndpoints();

            this.endpointsProcessingTask = this.container.Endpoints().StartEndpoints();
            await this.endpointsProcessingTask;
            this.endpointsProcessingTask = null;
        }
    }
}
