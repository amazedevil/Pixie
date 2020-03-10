using DryIoc;
using Pixie.Core.Cli;
using Pixie.Core.Exceptions;
using Pixie.Core.Messages;
using Pixie.Core.Middlewares;
using Pixie.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Pixie.Core.Services
{
    public class PXHandlerMappingService
    {
        public enum SpecificMessageHandlerType
        {
            ClientDisconnect
        }

        public class MappingItem {
            internal MappingItem() {
                this.Middlewares = new List<IPXMiddleware>();
            }

            internal List<IPXMiddleware> Middlewares { get; private set; }

            public MappingItem Middleware(IEnumerable<IPXMiddleware> middlewares) {
                this.Middlewares.AddRange(middlewares);
                return this;
            }

            public MappingItem Middleware(IPXMiddleware middleware) {
                this.Middlewares.Add(middleware);
                return this;
            }
        }

        public class Group : MappingItem {
            internal List<MappingItem> Items { get; private set; }

            public Group(IEnumerable<MappingItem> items = null) : base() {
                this.Items = items?.ToList() ?? new List<MappingItem>();
            }

            public Group MessageHandler<T>() {
                Items.Add(MessageHandlerItem.CreateWithMessageHandlerType<T>());
                return this;
            }

            public Group MessageHandlerProvider<T>(Func<PXMessageHandlerBase<T>> provider) where T : struct {
                Items.Add(MessageHandlerItem.CreateWithProvider(provider));
                return this;
            }

            public Group ClientDisconnectMessageHandler<T>() {
                Items.Add(SpecialMessageHandlerItem.ClientDisconnectMessageHandler<T>());
                return this;
            }

            public Group ClientDisconnectMessageHandlerProvider(Func<PXMessageHandlerBase<PXMessageVoid>> provider) {
                Items.Add(SpecialMessageHandlerItem.ClientDisconnectMessageHandlerProvider(provider));
                return this;
            }

            public Group CliCommand<T>() {
                Items.Add(new CliCommand(typeof(T)));
                return this;
            }

            public Group CliCommandFallback() {
                Items.Add(PXHandlerMappingService.CliCommand.Fallback());
                return this;
            }

            public Group Job<T>() {
                Items.Add(new Job(typeof(T)));
                return this;
            }

            public Group FallbackJob() {
                Items.Add(PXHandlerMappingService.Job.Fallback());
                return this;
            }

            public Group NestedGroup(Action<Group> handler) {
                var g = new Group();
                handler(g);
                Items.Add(g);
                return this;
            }
        }

        public class MessageHandlerItem : MappingItem
        {
            internal Type MessageType { get; private set; }
            internal Func<PXMessageHandlerRaw> Provider { get; private set; }

            public MessageHandlerItem(Type messageType, Func<PXMessageHandlerRaw> provider) : base() {
                this.MessageType = messageType;
                this.Provider = provider;
            }

            public static MessageHandlerItem CreateWithMessageHandlerType<T>() {
                return CreateWithMessageHandlerType(typeof(T));
            }

            public static MessageHandlerItem CreateWithProvider<T>(Func<PXMessageHandlerBase<T>> provider) where T : struct {
                return new MessageHandlerItem(typeof(T), provider);
            }

            public static MessageHandlerItem CreateWithMessageHandlerType(Type messageHandlerType) {
                //TODO: make type validation, to throw specific exception if it doesn't have required fields
                return new MessageHandlerItem(messageHandlerType.GetProperty(
                    PXMessageInfo.MESSAGE_CLASS_FIELD_DATA_TYPE,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
                ).GetValue(null) as Type, delegate { return Activator.CreateInstance(messageHandlerType) as PXMessageHandlerRaw; });
            }
        }

        public class SpecialMessageHandlerItem : MappingItem
        {
            internal SpecificMessageHandlerType MessageType { get; private set; }
            internal Func<PXMessageHandlerBase<PXMessageVoid>> Provider { get; private set; }

            public SpecialMessageHandlerItem(SpecificMessageHandlerType messageType, Func<PXMessageHandlerBase<PXMessageVoid>> provider) : base() {
                this.MessageType = messageType;
                this.Provider = provider;
            }

            public static SpecialMessageHandlerItem ClientDisconnectMessageHandlerProvider(Func<PXMessageHandlerBase<PXMessageVoid>> provider) {
                return new SpecialMessageHandlerItem(SpecificMessageHandlerType.ClientDisconnect, provider);
            }

            public static SpecialMessageHandlerItem ClientDisconnectMessageHandler<T>() {
                return ClientDisconnectMessageHandlerProvider(
                    delegate { return Activator.CreateInstance(typeof(T)) as PXMessageHandlerBase<PXMessageVoid>; }
                );
            }
        }

        public class CliCommand : MappingItem
        {
            internal Type Type { get; private set; }

            public CliCommand(Type type) {
                this.Type = type;
            }

            public static CliCommand Fallback() {
                return new CliCommand(typeof(PXCliCommand));
            }
        }

        public class Job : MappingItem {
            internal Type Type { get; private set; }

            public Job(Type type) {
                this.Type = type;
            }

            public static Job Fallback() {
                return new Job(typeof(PXJobBase));
            }
        }

        private struct MessageHandlerWrapper
        {
            public Func<PXMessageHandlerRaw> provider;
            public List<IPXMiddleware> middlewares;
        }

        private struct SpecificMessageHandlerWrapper
        {
            public Func<PXMessageHandlerBase<PXMessageVoid>> provider;
            public List<IPXMiddleware> middlewares;
        }

        private struct CommonHandlerWrapper
        {
            public List<IPXMiddleware> middlewares;
        }

        private IDictionary<Type, List<MessageHandlerWrapper>> messages = new Dictionary<Type, List<MessageHandlerWrapper>>();
        private IDictionary<SpecificMessageHandlerType, List<SpecificMessageHandlerWrapper>> specificMessages 
            = new Dictionary<SpecificMessageHandlerType, List<SpecificMessageHandlerWrapper>> ();
        private IDictionary<Type, CommonHandlerWrapper> cliCommands = new Dictionary<Type, CommonHandlerWrapper>();
        private IDictionary<Type, CommonHandlerWrapper> jobs = new Dictionary<Type, CommonHandlerWrapper>();

        private bool isRegistrationClosed = false;
        
        public void Register(MappingItem item) {
            if (isRegistrationClosed) {
                throw new PXRegistrationOutOfTime();
            }

            void Handle(MappingItem mi, List<IPXMiddleware> tail) {
                var middlewares = new List<IPXMiddleware>(tail);
                middlewares.AddRange(mi.Middlewares);

                switch (mi) {
                    case MessageHandlerItem mh:
                        if (!messages.ContainsKey(mh.MessageType)) {
                            messages[mh.MessageType] = new List<MessageHandlerWrapper>();
                        }

                        messages[mh.MessageType].Add(new MessageHandlerWrapper() {
                            provider = mh.Provider,
                            middlewares = middlewares
                        });
                        break;
                    case SpecialMessageHandlerItem smh:
                        if (!specificMessages.ContainsKey(smh.MessageType)) {
                            specificMessages[smh.MessageType] = new List<SpecificMessageHandlerWrapper>();
                        }

                        specificMessages[smh.MessageType].Add(new SpecificMessageHandlerWrapper() {
                            provider = smh.Provider,
                            middlewares = middlewares
                        });
                        break;
                    case CliCommand cli:
                        cliCommands[cli.Type] = new CommonHandlerWrapper() {
                            middlewares = middlewares
                        };
                        break;
                    case Job j:
                        jobs[j.Type] = new CommonHandlerWrapper() {
                            middlewares = middlewares
                        };
                        break;
                    case Group g:
                        foreach (var item in g.Items) {
                            Handle(item, middlewares);
                        }
                        break;
                }
            }

            Handle(item, new List<IPXMiddleware>());
        }

        internal IEnumerable<Type> GetHandlableMessageTypes() {
            return messages.Keys;
        }

        internal void HandleMessage(object message, Action<Action<IResolverContext>> contextProvider) {
            foreach (var data in messages[message.GetType()]) {
                contextProvider(delegate (IResolverContext context) {
                    ApplyMiddlewares(data.middlewares, delegate (IResolverContext processedContext) {
                        data.provider().SetupData(message).Handle(processedContext);
                    }, context);
                });
            }
        }

        internal void HandleSpecialMessage(SpecificMessageHandlerType type, Action<Action<IResolverContext>> contextProvider) {
            if (specificMessages.ContainsKey(type)) {
                foreach (var data in specificMessages[type]) {
                    contextProvider(delegate (IResolverContext context) {
                        ApplyMiddlewares(data.middlewares, delegate (IResolverContext processedContext) {
                            data.provider().Handle(processedContext);
                        }, context);
                    });
                }
            }
        }

        internal void HandleJob(PXJobBase job, IResolverContext context) {
            var type = job.GetType();

            if (!jobs.ContainsKey(type)) {
                type = typeof(PXJobBase);
            }

            if (jobs.ContainsKey(type)) {
                ApplyMiddlewares(jobs[type].middlewares, delegate(IResolverContext processedContext) {
                    job.Execute(processedContext);
                }, context);
            } else {
                job.Execute(context);
            }
        }

        internal void HandleCliCommand(PXCliCommand command, IResolverContext context) {
            var type = command.GetType();

            if (!cliCommands.ContainsKey(type)) {
                type = typeof(PXCliCommand);
            }

            if (cliCommands.ContainsKey(type)) {
                ApplyMiddlewares(jobs[type].middlewares, delegate (IResolverContext processedContext) {
                    command.Execute(processedContext);
                }, context);
            } else {
                command.Execute(context);
            }
        }

        private void ApplyMiddlewares(IEnumerable<IPXMiddleware> middlewares, Action<IResolverContext> endOfChain, IResolverContext context) {
            Action<IResolverContext> action = delegate (IResolverContext ctx) {
                endOfChain(ctx);
            };

            foreach (IPXMiddleware middleware in middlewares.Reverse()) {
                var previousAction = action;

                action = delegate (IResolverContext ctx) {
                    middleware.Handle(ctx, previousAction);
                };
            }

            action(context);
        }

        internal void CloseRegistration() {
            this.isRegistrationClosed = true;
        }

    }
}
