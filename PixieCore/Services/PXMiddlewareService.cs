using DryIoc;
using Pixie.Core.Messages;
using Pixie.Core.Middlewares;
using Pixie.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pixie.Core.Services
{
    public class PXMiddlewareService
    {

        [Flags]
        public enum Scope
        {
            Message = 1,
            Scheduled = 2,
            Cli = 4,
            Universal = Message | Scheduled | Cli
        }

        private struct MiddlewareWrapper
        {
            public IPXMiddleware middleware;
            public Scope scope;
        }

        private List<MiddlewareWrapper> middlewares = new List<MiddlewareWrapper>();

        public void AddMiddleware(IPXMiddleware middleware, Scope scope) {
            middlewares.Add(new MiddlewareWrapper() {
                middleware = middleware,
                scope = scope
            });
        }

        public void RemoveMiddleware(IPXMiddleware middleware) {
            middlewares.RemoveAll(m => Object.ReferenceEquals(m, middleware));
        }

        internal void HandleOverMiddlewares(Action<IResolverContext> handler, IResolverContext context, Scope scope) {
            ApplyMiddlewares(
                this.GetMiddlewares(scope),
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

        internal IEnumerable<IPXMiddleware> GetMiddlewares(Scope scope) {
            return this.middlewares.Where(m => m.scope.HasFlag(scope)).Select(m => m.middleware);
        }
    }
}
