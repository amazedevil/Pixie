using Pixie.Core.Services;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Pixie.Core.Cli
{
    public class PXCliClient
    {
        private PXEnvironmentService env = new PXEnvironmentService();
        private BinaryFormatter formatter = new BinaryFormatter();

        protected virtual void OnOutput(string s) {
            Console.Write(s);
        }

        public async Task Send(PXCliCommand command) {
            using (var pipe = new NamedPipeClientStream(env.GetString(PXCliConsts.PX_ENV_CLI_PIPE_NAME_PARAMETER, PXCliConsts.PX_ENV_CLI_PIPE_NAME_DEFAULT))) {
                await pipe.ConnectAsync();

                formatter.Serialize(pipe, command);

                string output = (string)formatter.Deserialize(pipe);

                if (output.Length > 0) {
                    OnOutput(output);
                }
            }
        }
    }
}
