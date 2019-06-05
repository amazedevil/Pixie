using DryIoc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Tasks {
    public class PXTaskBase {
        protected IContainer container;

        public PXTaskBase SetupContainer(IContainer container) {
            this.container = container;
            return this;
        }

        public virtual void Execute() { }
    }
}
