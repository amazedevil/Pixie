using DryIoc;
using Moq;
using NUnit.Framework;
using Pixie.Core;
using Pixie.Core.Messages;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services;
using Pixie.Core.Services.Internal;
using Pixie.Core.StreamWrappers;
using Pixie.Toolbox.StreamWrappers;
using PixieCoreTests.Client;
using PixieTests.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace PixieTests
{
    class ServerTests
    {
        //Simple server test

        private class SimpleTestServer : ServerTester.TestServer {
            internal SimpleTestServer(string host, int port) : base(host, port) {}
        }

        [Test]
        public void ClientToServerMessagePassingTest() {
            ServerTester.PlayCommonServerTest(new SimpleTestServer("localhost", PortProvider.ProviderPort()));
        }

        //SSL test

        private class EnvPlusSsl : EnvironmentDefaultsServiceProvider
        {
            internal EnvPlusSsl(string host, int port) : base(host, port) { }

            public override void HandleMock(Mock<IPXEnvironmentService> mock) {
                base.HandleMock(mock);

                mock.Setup(e => e.GetString(It.Is<string>(s => s == PXSSLStreamWrapper.ENV_PARAM_CERTIFICATE_PATH), It.IsAny<Func<string>>())).Returns("Resources/certificate.p12");
            }
        }

        private class SslTestServer : ServerTester.TestServer
        {
            internal SslTestServer(string host, int port) : base(host, port) { }

            protected override IPXStreamWrapper[] GetStreamWrappers(IResolverContext context) {
                return new IPXStreamWrapper[] {
                    new PXSSLStreamWrapper(context)
                };
            }

            protected override EnvironmentDefaultsServiceProvider CreateEnvServiceProvider() {
                return new EnvPlusSsl(this.Host, this.Port);
            }
        }

        [Test]
        public void SslServerMessagePassingTest() {
            ServerTester.PlayCommonServerTest(new SslTestServer("localhost", PortProvider.ProviderPort()), delegate(TestClient.Builder builder) {
                builder.SslEnabled(true);
            });
        }
    }
}
