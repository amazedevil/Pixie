using Pixie.Core.Services.Internal;
using System;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace Pixie.Core.Cli
{
    public class PXCliClient
    {
        private BinaryFormatter formatter = new BinaryFormatter();
        private string pipeName;

        public PXCliClient(string pipeName = PXCliConsts.PX_CLI_PIPE_NAME_DEFAULT) {
            this.pipeName = pipeName;
        }

        protected virtual void OnOutput(string s) {
            Console.Write(s);
        }

        public async Task Send(PXCliCommand command) {
            using (var pipe = new NamedPipeClientStream(this.pipeName)) {
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
