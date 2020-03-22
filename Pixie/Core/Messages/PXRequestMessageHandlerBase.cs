using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Messages
{
    public class PXRequestMessageHandlerBase<T, R> : PXMessageHandlerRaw 
        where T : struct 
        where R : struct
    {
        public new static Type DataType { get { return typeof(T); } }

        public virtual R Handle(T data) { return default; }

        public override void Handle() {
            if (data != null) {
                result = Handle((T)data);
            }
        }
    }
}
