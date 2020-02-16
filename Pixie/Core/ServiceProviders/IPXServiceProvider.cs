using DryIoc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.ServiceProviders
{
    public interface IPXServiceProvider
    {
        void OnRegister(IContainer container);
        void OnInitialize(IContainer container);
    }
}
