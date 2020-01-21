using Pixie.Core.Exceptions;
using Pixie.Core.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Pixie.Core.Services.Internal
{
    internal class PXMessageHandlerService : IPXMessageHandlerService
    {
        private IDictionary<Type, Func<PXMessageHandlerRaw>> messageHandlerProviders = new Dictionary<Type, Func<PXMessageHandlerRaw>>();

        private volatile bool registrationsAllowed = true;

        public void RegisterHandlerType<T>() {
            RegisterHandlerType(typeof(T));
        }

        public void RegisterProvider<T>(Func<PXMessageHandlerBase<T>> provider) where T : struct {
            RegisterProvider(typeof(T), provider);
        }

        public void RegisterHandlerType(Type messageHandlerType) {
            RegisterProvider(messageHandlerType.GetProperty(
                PXMessageInfo.MESSAGE_CLASS_FIELD_DATA_TYPE,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
            ).GetValue(null) as Type, delegate { return Activator.CreateInstance(messageHandlerType) as PXMessageHandlerRaw; });
        }

        public void RegisterProvider(Type messageType, Func<PXMessageHandlerRaw> provider) {
            if (!registrationsAllowed) {
                throw new PXRegistrationsAfterBoot();
            }

            messageHandlerProviders[messageType] = provider;
        }

        public PXMessageHandlerRaw Instantiate(Type messageType) {
            return messageHandlerProviders[messageType]();
        }

        public PXMessageHandlerBase<T> Instantiate<T>() where T : struct {
            return Instantiate(typeof(T)) as PXMessageHandlerBase<T>;
        }

        public IEnumerable<Type> GetMessageTypes() {
            return messageHandlerProviders.Keys;
        }

        internal void CloseRegistration() {
            registrationsAllowed = false;
        }
    }
}
