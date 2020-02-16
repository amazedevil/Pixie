using DryIoc;
using Moq;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services;
using Pixie.Core.Services.Internal;
using Pixie.Toolbox.StreamWrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace PixieTests.Common
{
    internal class EnvironmentDefaultsServiceProvider : IPXServiceProvider
    {
        private string host;
        private int port;

        internal EnvironmentDefaultsServiceProvider(string host = "localhost", int port = 7777) {
            this.host = host;
            this.port = port;
        }

        public void OnRegister(IContainer container) {
            container.RegisterDelegate(delegate(IResolverContext context) {
                var environmentMock = new Mock<IPXEnvironmentService>();

                environmentMock.Setup(e => e.GetString(It.Is<string>(s => s == PXEnvironmentService.ENV_PARAM_HOST), It.IsAny<Func<string>>())).Returns(this.host);
                environmentMock.Setup(e => e.GetInt(It.Is<string>(s => s == PXEnvironmentService.ENV_PARAM_PORT), It.IsAny<Func<int>>())).Returns(this.port);

                HandleMock(environmentMock);

                return environmentMock.Object;
            }, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
        }

        public void OnInitialize(IContainer container) {
        }

        public virtual void HandleMock(Mock<IPXEnvironmentService> mock) {
        }
    }
}
