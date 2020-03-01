using DryIoc;
using Pixie.Core.Services;
using System;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace Pixie.Core.Cli
{
    internal class PXCliServer : PXEndpointService.IEndpoint
    {
        private Task task = null;
        private CancellationTokenSource cancellationTokenSource = null;

        private IContainer container;
        private string pipeName;

        private BinaryFormatter formatter = new BinaryFormatter();

        internal PXCliServer(string pipeName, IContainer container) {
            this.pipeName = pipeName;
            this.container = container;
        }

        public Task Start() {
            async Task Run() {
                this.task = RunServer();
                await this.task;
                this.task = null;
            }

            return Run();
        }

        public void Stop() {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = null;
            task?.Wait();
        }

        private void ExecuteInCliScope(Action<IResolverContext> action) {
            using (var jobContext = this.container.OpenScope()) {
                action(jobContext);
            }
        }

        private async Task RunServer() {
            this.container.Logger().Info("Starting CLI server");

            this.cancellationTokenSource = new CancellationTokenSource();

            while (true) {
                try {
                    using (var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous)) {
                        await pipe.WaitForConnectionAsync(this.cancellationTokenSource.Token);

                        PXCliCommand command = (PXCliCommand)formatter.Deserialize(pipe);
                        
                        ExecuteInCliScope(delegate (IResolverContext context) {
                            this.container.Handlers().HandleCliCommand(command, context);
                        });

                        formatter.Serialize(pipe, command.FlushOutput());
                    }
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception e) {
                    this.container.Errors().Handle(e, Services.PXErrorHandlingService.Scope.CliCommand);
                }
            }
        }
    }
}
