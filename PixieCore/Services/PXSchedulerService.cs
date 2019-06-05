using DryIoc;
using NCrontab;
using Pixie.Core.Middlewares;
using Pixie.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pixie.Core.Services {
    public class PXSchedulerService {
        private class RegularTaskWrapper {
            public PXTaskBase task;
            public DateTime lastRun;
            public CrontabSchedule schedule;

            public void RunIfScheduled(DateTime previousTime, DateTime currentTime) {
                if (schedule.GetNextOccurrence(previousTime, currentTime.AddSeconds(1f)) < currentTime) {
                    task.Execute();
                }
            }
        }

        private class ScheduledTaskWrapper {
            public PXTaskBase task;
            public DateTime time;
        }

        private const int DELTA_TIME = 1000;

        private IContainer container;
        private bool isRunning = true;
        private DateTime lastTickTime = DateTime.Now;

        private LinkedList<ScheduledTaskWrapper> scheduledQueue = new LinkedList<ScheduledTaskWrapper>();
        private List<RegularTaskWrapper> regularTasks = new List<RegularTaskWrapper>();

        internal PXSchedulerService(IContainer container) : base() {
            this.container = container;
        }

        public void Schedule(PXTaskBase task, float delay) {
            var time = DateTime.Now + TimeSpan.FromSeconds(delay);

            var wrapper = new ScheduledTaskWrapper() { task = task, time = time };

            var node = FindNode(x => x.time > time);

            if (node == null) {
                scheduledQueue.AddLast(wrapper);
            } else {
                scheduledQueue.AddAfter(node, wrapper);
            }
        }

        public void ScheduleRegular(PXTaskBase task, string cronString) {
            regularTasks.Add(new RegularTaskWrapper() {
                lastRun = DateTime.Now,
                schedule = CrontabSchedule.Parse(cronString),
                task = task
            });
        }

        public void Remove(Func<PXTaskBase, bool> predicate) {
            scheduledQueue.Remove(FindNode(w => predicate(w.task)));
        }

        private LinkedListNode<ScheduledTaskWrapper> FindNode(Func<ScheduledTaskWrapper, bool> predicate) {
            LinkedListNode<ScheduledTaskWrapper> node = scheduledQueue.First;
            while (node != null) {
                if (predicate(node.Value)) {
                    return node;
                }
                node = node.Next;
            }

            return null;
        }

        internal async void Launch() {
            DateTime currentTime = DateTime.Now;

            void ProcessScheduledTasks() {
                while ((scheduledQueue.First?.Value.time ?? DateTime.MaxValue) < currentTime) {
                    StartTask(scheduledQueue.First.Value.task);
                    scheduledQueue.RemoveFirst();
                }
            }

            void ProcessRegularTasks() {
                foreach (var task in regularTasks) {
                    task.RunIfScheduled(lastTickTime, currentTime);
                }
            }

            while (isRunning) {
                ProcessScheduledTasks();
                ProcessRegularTasks();

                lastTickTime = currentTime;

                await Task.Delay(DELTA_TIME);
            }
        }

        private void StartTask(PXTaskBase task) {
            Action<IContainer> action = delegate(IContainer container) {
                task.SetupContainer(container).Execute();
            };

            foreach (IPXMiddleware middleware in this.container
                .Resolve<PXMiddlewareService>()
                .GetMiddlewares(PXMiddlewareService.Type.Scheduled)
                .Reverse()
            ) {
                var previousAction = action;

                action = delegate (IContainer container) {
                    middleware.Handle(container, previousAction);
                };
            }

            action(CreateCommandContainer());
        }

        private IContainer CreateCommandContainer() {
            return this.container.CreateFacade();
        }
    }
}
