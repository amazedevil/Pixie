using DryIoc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Middlewares {
    public interface IPXMiddleware {
        void Handle(IResolverContext context, Action<IResolverContext> next);
    }
}
