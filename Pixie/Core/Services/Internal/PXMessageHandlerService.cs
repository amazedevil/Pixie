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
        private enum SpecificHandler
        {
            ClientDisconnect
        }

        private IDictionary<Type, Func<PXMessageHandlerRaw>> messageHandlerProviders = new Dictionary<Type, Func<PXMessageHandlerRaw>>();
        private IDictionary<SpecificHandler, Func<PXMessageHandlerRaw>> specificMessageHandlerProviders = new Dictionary<SpecificHandler, Func<PXMessageHandlerRaw>>();

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

        public void RegisterProviderForClientDisconnect(Func<PXMessageHandlerBase<PXMessageVoid>> provider) {
            RegisterSpecificMessageHandler(SpecificHandler.ClientDisconnect, provider);
        }

        public void RegisterProviderForClientDisconnect(Type messageHandlerType) {
            RegisterProviderForClientDisconnect(delegate { return Activator.CreateInstance(messageHandlerType) as PXMessageHandlerBase<PXMessageVoid>; });
        }

        public void RegisterProvider(Type messageType, Func<PXMessageHandlerRaw> provider) {
            if (!registrationsAllowed) {
                throw new PXRegistrationOutOfTime();
            }

            messageHandlerProviders[messageType] = provider;
        }

        public PXMessageHandlerRaw Instantiate(Type messageType) {
            return messageHandlerProviders[messageType]();
        }

        public PXMessageHandlerBase<T> Instantiate<T>() where T : struct {
            return Instantiate(typeof(T)) as PXMessageHandlerBase<T>;
        }

        public PXMessageHandlerBase<PXMessageVoid> InstantiateForClientDisconnect() {
            return InstantiateSpecificMessageHandler(SpecificHandler.ClientDisconnect) as PXMessageHandlerBase<PXMessageVoid>;
        }

        public IEnumerable<Type> GetMessageTypes() {
            return messageHandlerProviders.Keys;
        }

        private void RegisterSpecificMessageHandler(SpecificHandler handler, Func<PXMessageHandlerRaw> provider) {
            if (provider != null) {
                specificMessageHandlerProviders[handler] = provider;
            } else {
                specificMessageHandlerProviders.Remove(handler);
            }
        }

        private PXMessageHandlerRaw InstantiateSpecificMessageHandler(SpecificHandler handler) {
            if (!specificMessageHandlerProviders.ContainsKey(handler)) {
                return null;
            }

            return specificMessageHandlerProviders[handler]();
        }

        internal void CloseRegistration() {
            registrationsAllowed = false;
        }
    }
}
