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
            Message,
            Cli
        }

        private List<IPXMiddleware> schedulerMiddlewares = new List<IPXMiddleware>();
        private List<IPXMiddleware> messageMiddlewares = new List<IPXMiddleware>();
        private List<IPXMiddleware> cliCommandMiddlewares = new List<IPXMiddleware>();

        public void AddMiddleware(IPXMiddleware middleware, Type type) {
            switch (type) {
                case Type.Message:
                    messageMiddlewares.Add(middleware);
                    break;
                case Type.Scheduled:
                    schedulerMiddlewares.Add(middleware);
                    break;
                case Type.Cli:
                    cliCommandMiddlewares.Add(middleware);
                    break;
                case Type.Universal:
                    schedulerMiddlewares.Add(middleware);
                    messageMiddlewares.Add(middleware);
                    cliCommandMiddlewares.Add(middleware);
                    break;
            }
        }

        public void RemoveMiddleware(IPXMiddleware middleware) {
            messageMiddlewares.Remove(middleware);
            schedulerMiddlewares.Remove(middleware);
            cliCommandMiddlewares.Remove(middleware);
        }

        internal void HandleOverMiddlewares(Action<IResolverContext> handler, IResolverContext context, PXMiddlewareService.Type type) {
            ApplyMiddlewares(
                this.GetMiddlewares(type),
                handler,
                context
            );
        }

        internal void ApplyMiddlewares(IEnumerable<IPXMiddleware> middlewares, Action<IResolverContext> endOfChain, IResolverContext context) {
            Action<IResolverContext> action = delegate (IResolverContext ctx) {
                endOfChain(ctx);
            };

            foreach (IPXMiddleware middleware in middlewares.Reverse()) {
                var previousAction = action;

                action = delegate (IResolverContext ctx) {
                    middleware.Handle(ctx, previousAction);
                };
            }

            action(context);
        }

        internal IEnumerable<IPXMiddleware> GetMiddlewares(Type type) {
            switch (type) {
                case Type.Scheduled:
                    return schedulerMiddlewares;
                case Type.Message:
                    return messageMiddlewares;
                case Type.Cli:
                    return cliCommandMiddlewares;
            }

            throw new NotSupportedException(); //TODO: throw something better
        }
    }
}
