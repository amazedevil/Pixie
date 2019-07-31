using DryIoc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Cli {
    [Serializable]
    public abstract class PXCliCommand {
        private StringBuilder outputBuilder = new StringBuilder();

        public virtual void Execute(IResolverContext resolver) {}

        protected void Println(string s) {
            outputBuilder.AppendLine(s);
        }

        internal string FlushOutput() {
            try {
                return outputBuilder.ToString();
            } finally {
                outputBuilder.Clear();
            }
        }
    }
}
