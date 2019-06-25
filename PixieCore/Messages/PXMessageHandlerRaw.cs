using DryIoc;
using Pixie.Core.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Messages {
    public class PXMessageHandlerRaw {
        public static string MessageName { get { throw new NotSupportedException(); } }
        public static Type DataType { get { return null; } }

        protected object data;
        protected IContainer container;

        public virtual void SetupData(object data) {
            this.data = data;
        }

        public void Handle(IContainer container) {
            this.container = container;

            container.Logger().Info(string.Format("Handing message with " + this.GetType().ToString()));

            Handle();
        }

        public virtual void Handle() {}
    }
}
