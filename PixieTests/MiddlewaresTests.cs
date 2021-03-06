﻿using DryIoc;
using NUnit.Framework;
using Pixie.Core.Messages;
using Pixie.Core.Middlewares;
using Pixie.Core.Services;
using PixieTests.Common;
using System;
using System.Collections.Generic;

namespace PixieTests
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

        class MessageHandler : PXMessageHandlerBase<TestMessages.TestMessageType1>
        {
        }

        private class MiddlewareTestServer : ServerTester.TestServer
        {
            public List<int> result = new List<int>();

            internal MiddlewareTestServer(string address, int port) : base(address, port) { }

            public override void OnRegister(IContainer container) {
                container.Use(result);

                container.Handlers().Register(
                    PXHandlerService.MessageHandlerItem.CreateWithMessageHandlerType<MessageHandler>()
                        .Middleware(new Middleware() { number = 3 })
                        .Middleware(new Middleware() { number = 2 })
                        .Middleware(new Middleware() { number = 1 })
                );

                //moved registration to bottom,
                //so signal message will be received
                //after middlewares firing
                base.OnRegister(container);
            }
        }

        [Test]
        public void MiddlewaresExecutionOrderTest() {
            var server = new MiddlewareTestServer("0.0.0.0", PortProvider.ProviderPort());

            ServerTester.PlayCommonServerTest(server);

            Assert.AreEqual(new int[] { 3, 2, 1 }, server.result.ToArray());
        }
    }
}
