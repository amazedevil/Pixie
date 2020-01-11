using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pixie.Core.Services.Internal
{
    internal class PXEnvironmentService : IPXEnvironmentService
    {
        public const string ENV_PARAM_CLI_PIPE_NAME = "PX_ENV_CLI_PIPE_NAME";
        public const string ENV_PARAM_LOG_LEVEL = "PX_ENV_LOG_LEVEL";

        private JObject items;

        internal PXEnvironmentService() {
            string envFilePath = AppDomain.CurrentDomain.BaseDirectory + ".env";

            if (File.Exists(envFilePath)) {
                items = JObject.Parse(File.ReadAllText(envFilePath));
            } else {
                items = new JObject();
            }
        }

        public string GetString(string key, string defaultValue) {
            if (!items.ContainsKey(key)) {
                return defaultValue;
            }

            return (string)items[key];
        }

        public int GetInt(string key, int defaultValue) {
            if (!items.ContainsKey(key)) {
                return defaultValue;
            }

            return (int)items[key];
        }
    }
}
