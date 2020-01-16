using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pixie.Core.Services.Internal
{
    internal class PXEnvironmentService : IPXEnvironmentService
    {
        public const string ENV_PARAM_CLI_PIPE_NAME = "PX_CLI_PIPE_NAME";
        public const string ENV_PARAM_LOG_LEVEL = "PX_LOG_LEVEL";
        public const string ENV_PARAM_HOST = "PX_HOST";
        public const string ENV_PARAM_PORT = "PX_PORT";

        private JObject items;

        internal PXEnvironmentService() {
            string envFilePath = AppDomain.CurrentDomain.BaseDirectory + ".env";

            if (File.Exists(envFilePath)) {
                items = JObject.Parse(File.ReadAllText(envFilePath));
            } else {
                items = new JObject();
            }
        }

        public string GetString(string key, Func<string> defaultValueProvider) {
            if (!items.ContainsKey(key)) {
                return defaultValueProvider();
            }

            return (string)items[key];
        }

        public int GetInt(string key, Func<int> defaultValueProvider) {
            if (!items.ContainsKey(key)) {
                return defaultValueProvider();
            }

            return (int)items[key];
        }
    }
}
