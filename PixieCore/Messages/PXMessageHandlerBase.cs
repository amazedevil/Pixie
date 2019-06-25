using System;
using System.Collections.Generic;
using System.Text;

namespace Pixie.Core.Messages {
    public class PXMessageHandlerBase<T> : PXMessageHandlerRaw where T : struct {
        public new static string MessageName
        {
            get {
                return typeof(T).GetField(PXMessageInfo.MESSAGE_CLASS_FIELD_NAME).GetValue(null) as string;
            }
        }

        public new static Type DataType { get { return typeof(T); } }

        public virtual void Handle(T data) {}

        public override void Handle() {
            if (data != null) {
                Handle((T)data);
            }
        }
    }
}
