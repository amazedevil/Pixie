using DryIoc;
using Pixie.Core.Messages;
using Pixie.Core.Middlewares;
using Pixie.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pixie.Core.Services {
    public class PXMiddlewareService {
        public enum Type {
            Universal,
            Scheduled,
            Message
        }

        private List<IPXMiddleware> schedulerMiddlewares = new List<IPXMiddleware>();
        private List<IPXMiddleware> messageMiddlewares = new List<IPXMiddleware>();

        public void AddMiddleware(IPXMiddleware middleware, Type type) {
            switch (type) {
                case Type.Message:
                    messageMiddlewares.Add(middleware);
                    break;
                case Type.Scheduled:
                    schedulerMiddlewares.Add(middleware);
                    break;
                case Type.Universal:
                    schedulerMiddlewares.Add(middleware);
                    messageMiddlewares.Add(middleware);
                    break;
            }
        }

        public void RemoveMiddleware(IPXMiddleware middleware) {
            messageMiddlewares.Remove(middleware);
            schedulerMiddlewares.Remove(middleware);
        }

        internal void HandleOverMiddlewares(Action<IContainer> handler, IContainer container, PXMiddlewareService.Type type) {
            ApplyMiddlewares(
                this.GetMiddlewares(type),
                delegate (IContainer ctr) { handler(ctr); },
                container
            );
        }

        internal void ApplyMiddlewares(IEnumerable<IPXMiddleware> middlewares, Action<IContainer> endOfChain, IContainer container) {
            Action<IContainer> action = delegate (IContainer ctr) {
                endOfChain(ctr);
            };

            foreach (IPXMiddleware middleware in middlewares.Reverse()) {
                var previousAction = action;

                action = delegate (IContainer ctr) {
                    middleware.Handle(ctr, previousAction);
                };
            }

            action(container);
        }

        internal IEnumerable<IPXMiddleware> GetMiddlewares(Type type) {
            switch (type) {
                case Type.Scheduled:
                    return schedulerMiddlewares;
                case Type.Message:
                    return messageMiddlewares;
            }

            throw new NotSupportedException(); //TODO: throw something better
        }
    }
}
