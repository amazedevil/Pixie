using DryIoc;
using Moq;
using NUnit.Framework;
using Pixie.Core;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services;
using PixieTests.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PixieTests
{
    public class ServiceProvidersTests {
        private class TestServer : PXServer {
            private IPXServiceProvider[] providers;

            public TestServer(IPXServiceProvider[] providers) : base() {
                this.providers = providers;
            }

            protected override IPXServiceProvider[] GetServiceProviders() {
                return providers;
            }
        }

        [Test]
        public void ServiceProviderMethodsCallTest() {
            var serviceProviderMockFirst = new Mock<IPXServiceProvider>(MockBehavior.Strict);
            var serviceProviderMockSecond = new Mock<IPXServiceProvider>(MockBehavior.Strict);

            var sequence = new MockSequence();

            serviceProviderMockFirst.InSequence(sequence).Setup(sp => sp.OnBoot(It.IsAny<IContainer>()));
            serviceProviderMockSecond.InSequence(sequence).Setup(sp => sp.OnBoot(It.IsAny<IContainer>()));

            serviceProviderMockFirst.InSequence(sequence).Setup(sp => sp.OnPostBoot(It.IsAny<IContainer>()));
            serviceProviderMockSecond.InSequence(sequence).Setup(sp => sp.OnPostBoot(It.IsAny<IContainer>()));

            TestServer server = new TestServer(
                new IPXServiceProvider[] {
                    new EnvironmentDefaultsServiceProvider(),
                    serviceProviderMockFirst.Object,
                    serviceProviderMockSecond.Object
                }
            );

            server.StartAsync();

            serviceProviderMockFirst.Verify(sp => sp.OnBoot(It.IsAny<IContainer>()), Times.Once);
            serviceProviderMockFirst.Verify(sp => sp.OnPostBoot(It.IsAny<IContainer>()), Times.Once);

            serviceProviderMockSecond.Verify(sp => sp.OnBoot(It.IsAny<IContainer>()), Times.Once);
            serviceProviderMockSecond.Verify(sp => sp.OnPostBoot(It.IsAny<IContainer>()), Times.Once);
        }
    }
}
