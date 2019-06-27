using DryIoc;
using Pixie.Core.DependencyInjection;
using Pixie.Core.Middlewares;
using Pixie.Core.Services;
using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Pixie.Core.Tasks {
    public class PXJobBase : IJob {
        protected IContainer container;

        protected JobDataMap Data { get; private set; }

        public virtual void Execute() { }

        public Task Execute(IJobExecutionContext context) {
            var originalContainer = (context.Scheduler.Context.Get(PXSchedulerService.SCHEDULER_SERVICE) as PXSchedulerService).CreateJobContainer();

            try {
                originalContainer.Logger().Info("Executing Job " + this.GetType().ToString());

                originalContainer.Middlewares().HandleOverMiddlewares(
                    delegate (IContainer ctr) {
                        this.container = ctr;
                        this.Data = context.MergedJobDataMap;
                        Execute();
                    },
                    originalContainer,
                    PXMiddlewareService.Type.Scheduled
                );
            } catch (Exception e) {
                originalContainer.Logger().Exception(e);
            }

            return Task.CompletedTask;
        }
    }
}
