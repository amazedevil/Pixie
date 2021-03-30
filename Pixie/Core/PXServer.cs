using DryIoc;
using Pixie.Core.Cli;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services;
using System;
using System.Threading.Tasks;

namespace Pixie.Core
{
    public class PXServer
    {
        protected Container container;
        private volatile Task endpointsProcessingTask = null;

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
            Stop();
        }

        public void Start() {
            try {
                foreach (var module in GetServiceProviders()) {
                    module.OnRegister(this.container);
                }

                CloseRegistrations();

                StartScheduler();

                StartAgents();

                StartEndpoints();

                foreach (var module in GetServiceProviders()) {
                    module.OnInitialize(this.container);
                }
            } catch (Exception ex) {
                this.container.Errors().Handle(ex, PXErrorHandlingService.Scope.PixieServer);
            }
        }

        public void Stop() {
            this.container.Agents().StopAgents();
        }

        public void WaitForEndpointsToStop() {
            this.endpointsProcessingTask?.Wait();
        }

        private void CloseRegistrations() {
            this.container.Handlers().CloseRegistration();
            this.container.Endpoints().CloseRegistration();
            this.container.Agents().CloseRegistration();
        }

        private Container CreateContainer() {
            var container = new Container();

            container.Use(this);

            container.RegisterDelegate(r => new PXSchedulerService(r.Resolve<IContainer>()), Reuse.Singleton);
            container.RegisterDelegate(r => new PXEndpointService(r.Resolve<IContainer>()), Reuse.Singleton);
            container.RegisterDelegate(r => new PXAgentService(r.Resolve<IContainer>()), Reuse.Singleton);
            container.RegisterDelegate(r => new PXSenderDispatcherService(), Reuse.Singleton);
            container.Register<PXErrorHandlingService>();
            container.RegisterDelegate(r => new PXLoggerService(r.Resolve<IContainer>()), Reuse.Singleton);
            container.RegisterDelegate(_ => new PXHandlerService(), Reuse.Singleton);

            return container;
        }

        private void StartScheduler() {
            this.container.Logger().Info("Starting scheduler");

            container.Resolve<PXSchedulerService>().Launch();
        }

        private void StartAgents() {
            this.container.Agents().BuildAndStartAgents();
        }

        private async void StartEndpoints() {
            this.container.Endpoints().BuildEndpoints();

            this.endpointsProcessingTask = this.container.Endpoints().StartEndpoints();
            await this.endpointsProcessingTask;
            this.endpointsProcessingTask = null;
        }
    }
}
