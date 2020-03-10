using DryIoc;
using Pixie.Core.Exceptions;
using Pixie.Core.Tasks;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Specialized;

namespace Pixie.Core.Services
{
    public class PXSchedulerService
    {
        internal const string SCHEDULER_SERVICE = "scheduler_service";

        private IContainer container;
        private IScheduler scheduler;

        public PXSchedulerService(IContainer container) : base() {
            this.container = container;
            this.scheduler = GetScheduler();

            this.scheduler.Context.Add(SCHEDULER_SERVICE, this);
        }

        internal void Launch() {
            this.scheduler.Start();
        }

        internal void ExecuteInJobScope(Action<IResolverContext> action) {
            using (var jobContext = this.container.OpenScope()) {
                action(jobContext);
            }
        }

        private IScheduler GetScheduler() {
            NameValueCollection props = new NameValueCollection {
                //generated name prevents issues when multiple Pixie instances running, e.g. in tests
                { "quartz.scheduler.instanceName", "PixieScheduler-" + Guid.NewGuid().ToString() }, 
                { "quartz.threadPool.threadCount", "10" },
                { "quartz.jobStore.misfireThreshold", "60000" }
            };

            StdSchedulerFactory factory = new StdSchedulerFactory(props);

            return factory.GetScheduler().GetAwaiter().GetResult();
        }

        public void Schedule(IJobDetail job, TimeSpan delay) {
            ScheduleInternal(
                job,
                TriggerBuilder.Create()
                    .StartAt(DateTime.Now + delay)
                    .Build()
            );
        }

        public void ScheduleCrontab(IJobDetail job, string cronString) {
            ScheduleInternal(
                job,
                TriggerBuilder.Create()
                    .StartNow()
                    .WithCronSchedule(cronString)
                    .Build()
            );
        }

        public bool DeleteJob(JobKey key) {
            return scheduler.DeleteJob(key).GetAwaiter().GetResult();
        }

        private void ScheduleInternal(IJobDetail job, ITrigger trigger) {
            if (!job.JobType.IsInstanceOfType(typeof(PXJobBase))) {
                throw new PXJobBaseTypeInvalid(job.JobType);
            }

            scheduler.ScheduleJob(job, trigger);
        }
    }
}
