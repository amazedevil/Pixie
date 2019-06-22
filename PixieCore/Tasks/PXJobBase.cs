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
            this.container.Middlewares().HandleOverMiddlewares(
                delegate(IContainer ctr) {
                    this.container = ctr;
                    this.Data = context.MergedJobDataMap;
                    Execute();
                },
                (context.Scheduler.Context.Get(PXSchedulerService.SCHEDULER_SERVICE) as PXSchedulerService).CreateCommandContainer(),
                PXMiddlewareService.Type.Scheduled
            );

            return Task.CompletedTask;
        }
    }
}
