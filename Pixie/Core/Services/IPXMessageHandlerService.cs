﻿using Pixie.Core.Messages;
using System;
using System.Collections.Generic;

namespace Pixie.Core.Services
{
    public interface IPXMessageHandlerService
    {
        void RegisterProvider<T>(Func<PXMessageHandlerBase<T>> provider) where T : struct;
        void RegisterProvider(Type messageHandlerType, Func<PXMessageHandlerRaw> provider);
        void RegisterHandlerType<T>();
        void RegisterHandlerType(Type messageHandlerType);
        PXMessageHandlerRaw Instantiate(Type messageType);
        PXMessageHandlerBase<T> Instantiate<T>() where T : struct;
        IEnumerable<Type> GetMessageTypes();
    }
}