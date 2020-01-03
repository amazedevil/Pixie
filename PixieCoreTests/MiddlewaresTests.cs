using DryIoc;
using NUnit.Framework;
using Pixie.Core.Middlewares;
using Pixie.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace PixieCoreTests
{
    class MiddlewaresTests
    {
        class Middleware : IPXMiddleware
        {
            public int number;

            public void Handle(IResolverContext context, Action<IResolverContext> next) {
                context.Resolve<List<int>>().Add(number);

                next(context);
            }
        }

        [Test]
        public void MiddlewaresExecutionOrderTest() {
            var service = new PXMiddlewareService();

            List<int> result = new List<int>();

            Container container = new Container();

            container.Use(result);

            foreach (var middleware in new IPXMiddleware[] {
                new Middleware() { number = 3 },
                new Middleware() { number = 2 },
                new Middleware() { number = 1 },
            }) {
                service.AddMiddleware(middleware, PXMiddlewareService.Scope.Universal);
            }

            service.HandleOverMiddlewares(delegate (IResolverContext context) {
                Assert.AreEqual(
                    new int[] { 3, 2, 1 },
                    context.Resolve<List<int>>()
                );
            }, container, PXMiddlewareService.Scope.Universal);
        }
    }
}
