﻿using DryIoc;
using Pixie.Core.Services.Internal;
using System;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace Pixie.Core.Cli
{
    internal class PXCliServer
    {
        private Task task;
        private CancellationTokenSource cancellationTokenSource;

        private Container container;

        private string pipeName = PXCliConsts.PX_ENV_CLI_PIPE_NAME_DEFAULT;
        private BinaryFormatter formatter = new BinaryFormatter();

        internal PXCliServer(Container container) {
            this.container = container;
            pipeName = container.Env().GetString(PXEnvironmentService.ENV_PARAM_CLI_PIPE_NAME, pipeName);

            cancellationTokenSource = new CancellationTokenSource();
            task = RunServer(cancellationTokenSource.Token);
        }

        internal void Stop() {
            cancellationTokenSource.Cancel();
            task.Wait();
        }

        private void ExecuteInCliScope(Action<IResolverContext> action) {
            using (var jobContext = this.container.OpenScope()) {
                action(jobContext);
            }
        }

        private async Task RunServer(CancellationToken token) {
            while (true) {
                try {
                    using (var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous)) {
                        await pipe.WaitForConnectionAsync(token);

                        PXCliCommand command = (PXCliCommand)formatter.Deserialize(pipe);

                        string output = "";

                        ExecuteInCliScope(delegate (IResolverContext context) {
                            this.container.Middlewares().HandleOverMiddlewares(delegate (IResolverContext ctx) {
                                command.Execute(ctx);
                                output = command.FlushOutput();
                            }, context, Services.PXMiddlewareService.Scope.Cli);
                        });

                        formatter.Serialize(pipe, output);
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
