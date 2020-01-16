using DryIoc;
using Moq;
using Pixie.Core.ServiceProviders;
using Pixie.Core.Services;
using Pixie.Core.Services.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace PixieTests.Common
{
    internal class EnvironmentDefaultsServiceProvider : IPXServiceProvider
    {
        public const string HOST = "localhost";
        public const int PORT = 7777;

        public void OnBoot(IContainer container) {
            container.RegisterDelegate(delegate(IResolverContext context) {
                var environmentMock = new Mock<IPXEnvironmentService>();

                environmentMock.Setup(e => e.GetString(It.Is<string>(s => s == PXEnvironmentService.ENV_PARAM_HOST), It.IsAny<Func<string>>())).Returns(HOST);
                environmentMock.Setup(e => e.GetInt(It.Is<string>(s => s == PXEnvironmentService.ENV_PARAM_PORT), It.IsAny<Func<int>>())).Returns(PORT);

                return environmentMock.Object;
            }, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
        }

        public void OnPostBoot(IContainer container) {
        }
    }
}
