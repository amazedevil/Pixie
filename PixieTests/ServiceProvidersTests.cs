using DryIoc;
using Moq;
using NUnit.Framework;
using Pixie.Core;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PixieTests
{
    public class ServiceProvidersTests {
        private class TestServer : PXServer {
            private IPXServiceProvider[] providers;

            public TestServer(IPXInitialOptionsService options, IPXServiceProvider[] providers) : base(options) {
                this.providers = providers;
            }

            protected override IPXServiceProvider[] GetServiceProviders() {
                return providers;
            }
        }

        private class TestInitialOptions : IPXInitialOptionsService {
            public int Port => 7777;

            public bool Debug => false;

            public string Host => "localhost";
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
                new TestInitialOptions(), 
                new IPXServiceProvider[] { 
                    serviceProviderMockFirst.Object,
                    serviceProviderMockSecond.Object
                }
            );

            server.Start();

            Thread.Sleep(1000);

            serviceProviderMockFirst.Verify(sp => sp.OnBoot(It.IsAny<IContainer>()), Times.Once);
            serviceProviderMockFirst.Verify(sp => sp.OnPostBoot(It.IsAny<IContainer>()), Times.Once);

            serviceProviderMockSecond.Verify(sp => sp.OnBoot(It.IsAny<IContainer>()), Times.Once);
            serviceProviderMockSecond.Verify(sp => sp.OnPostBoot(It.IsAny<IContainer>()), Times.Once);
        }
    }
}
