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

namespace PixieCoreTests
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
            var serviceProviderMock = new Mock<IPXServiceProvider>(MockBehavior.Strict);

            var sequence = new MockSequence();

            serviceProviderMock.InSequence(sequence).Setup(sp => sp.OnBoot(It.IsAny<IContainer>()));
            serviceProviderMock.InSequence(sequence).Setup(sp => sp.OnPostBoot(It.IsAny<IContainer>()));

            TestServer server = new TestServer(
                new TestInitialOptions(), 
                new IPXServiceProvider[] { serviceProviderMock.Object }
            );

            server.Start();

            Thread.Sleep(1000);

            serviceProviderMock.Verify(sp => sp.OnBoot(It.IsAny<IContainer>()), Times.Once);
            serviceProviderMock.Verify(sp => sp.OnPostBoot(It.IsAny<IContainer>()), Times.Once);
        }
    }
}
