using DryIoc;
using NUnit.Framework;
using Pixie.Core;
using Pixie.Core.ServiceProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace PixieTests
{
    class ServicesTests {
        private class TestServiceProvider : IPXServiceProvider
        {
            public void OnRegister(IContainer container) {
            }

            public void OnInitialize(IContainer container) {
                Assert.IsNotNull(container.Logger());
                Assert.IsNotNull(container.Middlewares());
                Assert.IsNotNull(container.Env());
                Assert.IsNotNull(container.Client());
                Assert.IsNotNull(container.Scheduler());
                Assert.IsNotNull(container.Sender());
                Assert.IsNotNull(container.Handlers());
                Assert.IsNotNull(container.Errors());
            }
        }

        private class TestServer : PXServer
        {
            protected override IPXServiceProvider[] GetServiceProviders() {
                return new IPXServiceProvider[] {
                    new TestServiceProvider(),
                };
            }
        }

        [Test]
        public void CommonServicesAvailabilityTest() {
            (new TestServer()).StartAsync();
        }
    }
}
