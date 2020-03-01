using DryIoc;
using Pixie.Core.Services;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Pixie.Core.Tasks
{
    public class PXJobBase : IJob
    {
        protected IResolverContext context;

        protected JobDataMap Data { get; private set; }

        public virtual void Execute() { }

        internal void Execute(IResolverContext context) {
            this.context = context;
            Execute();
        }

        public Task Execute(IJobExecutionContext context) {
            var scheduleService = (context.Scheduler.Context.Get(PXSchedulerService.SCHEDULER_SERVICE) as PXSchedulerService);

            scheduleService.ExecuteInJobScope(delegate (IResolverContext jobContext) {
                try {
                    jobContext.Logger().Info("Executing Job " + this.GetType().ToString());

                    this.Data = context.MergedJobDataMap;

                    jobContext.Handlers().HandleJob(this, jobContext);
                } catch (Exception e) {
                    jobContext.Errors().Handle(e, PXErrorHandlingService.Scope.Job);
                }
            });

            return Task.CompletedTask;
        }
    }
}
