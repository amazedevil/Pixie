using DryIoc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Messages
{
    public class PXMessageHandlerRaw
    {
        public static Type DataType { get { return null; } }

        protected object data;
        protected object result;
        protected IResolverContext context;

        internal object Result { get => result; }

        internal PXMessageHandlerRaw SetupData(object data) {
            this.data = data;
            return this;
        }

        public void Handle(IResolverContext context) {
            this.context = context;

            context.Logger().Info(string.Format("Handing message with " + this.GetType().ToString()));

            Handle();
        }

        public virtual void Handle() { }
    }
}
