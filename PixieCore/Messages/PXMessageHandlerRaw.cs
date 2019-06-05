using DryIoc;
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

        public PXMessageHandlerRaw SetupContainer(IContainer container) {
            this.container = container;
            return this;
        }

        public virtual void Handle() {}
    }
}
