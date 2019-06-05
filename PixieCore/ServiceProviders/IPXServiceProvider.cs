using DryIoc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.ServiceProviders {
    public interface IPXServiceProvider {

        void OnBoot(IContainer container);

        void OnPostBoot(IContainer container);

    }
}
