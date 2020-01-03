using DryIoc;
using Pixie.Core.DependencyInjection;
using Pixie.Core.Middlewares;
using Pixie.Core.Services;
using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Pixie.Core.Tasks
{
    public class PXJobBase : IJob
    {
        protected IResolverContext context;

        protected JobDataMap Data { get; private set; }

        public virtual void Execute() { }

        public Task Execute(IJobExecutionContext context) {
            var scheduleService = (context.Scheduler.Context.Get(PXSchedulerService.SCHEDULER_SERVICE) as PXSchedulerService);

            scheduleService.ExecuteInJobScope(delegate (IResolverContext jobContext) {
                try {
                    jobContext.Logger().Info("Executing Job " + this.GetType().ToString());

                    jobContext.Middlewares().HandleOverMiddlewares(
                        delegate (IResolverContext ctx) {
                            this.context = ctx;
                            this.Data = context.MergedJobDataMap;
                            Execute();
                        },
                        jobContext,
                        PXMiddlewareService.Scope.Scheduled
                    );
                } catch (Exception e) {
                    jobContext.Logger().Exception(e);
                }
            });


            return Task.CompletedTask;
        }
    }
}
