﻿using Pixie.Core.Services.Internal;
using System;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
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
            using (var pipe = new NamedPipeClientStream(env.GetString(PXEnvironmentService.ENV_PARAM_CLI_PIPE_NAME, PXCliConsts.PX_ENV_CLI_PIPE_NAME_DEFAULT))) {
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