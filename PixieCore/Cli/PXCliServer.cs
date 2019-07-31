using DryIoc;
using Pixie.Core.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Pixie.Core.Cli {
    internal class PXCliServer {

        private bool isClosed = false;
        private Container container;

        private string pipeName = PXCliConsts.PX_ENV_CLI_PIPE_NAME_DEFAULT;
        private UnicodeEncoding streamEncoding = new UnicodeEncoding();
        private Type[] commandTypes;
        private BinaryFormatter formatter = new BinaryFormatter();

        internal PXCliServer(Type[] commandTypes, Container container) {
            this.container = container;
            this.commandTypes = commandTypes;
            pipeName = container.Env().GetString(PXCliConsts.PX_ENV_CLI_PIPE_NAME_PARAMETER, pipeName);

            RunServer();
        }

        private void ExecuteInCliScope(Action<IResolverContext> action) {
            using (var jobContext = this.container.OpenScope()) {
                action(jobContext);
            }
        }

        private async void RunServer() {
            while (!isClosed) {
                using (var pipe = new NamedPipeServerStream(pipeName)) {
                    await pipe.WaitForConnectionAsync();

                    PXCliCommand command = (PXCliCommand)formatter.Deserialize(pipe);

                    string output = "";

                    ExecuteInCliScope(delegate (IResolverContext context) {
                        this.container.Middlewares().HandleOverMiddlewares(delegate (IResolverContext ctx) {
                            command.Execute(ctx);
                            output = command.FlushOutput();
                        }, context, Services.PXMiddlewareService.Type.Cli);
                    });

                    formatter.Serialize(pipe, output);
                }
            }
        }
    }
}
