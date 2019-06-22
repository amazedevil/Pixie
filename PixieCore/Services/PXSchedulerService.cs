using DryIoc;
using Pixie.Core.Middlewares;
using Pixie.Core.Tasks;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pixie.Core.Services {
    public class PXSchedulerService {

        internal const string SCHEDULER_SERVICE = "scheduler_service";

        private IContainer container;
        private IScheduler scheduler;

        internal PXSchedulerService(IContainer container) : base() {
            this.container = container;
            this.scheduler = CreateScheduler();

            this.scheduler.Context.Add(SCHEDULER_SERVICE, this);
        }

        internal void Launch() {
            this.scheduler.Start();
        }

        internal IContainer CreateCommandContainer() {
            return this.container.CreateFacade();
        }

        private IScheduler CreateScheduler() {
            NameValueCollection props = new NameValueCollection {
            };

            StdSchedulerFactory factory = new StdSchedulerFactory(props);

            return factory.GetScheduler().GetAwaiter().GetResult();
        }

        public void Schedule(IJobDetail job, TimeSpan delay) {
            scheduler.ScheduleJob(
                job,
                TriggerBuilder.Create()
                    .StartAt(DateTime.Now + delay)
                    .Build()
            );
        }

        public void ScheduleCrontab(IJobDetail job, string cronString) {
            scheduler.ScheduleJob(
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
    }
}
