using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pixie.Core.Services
{
    public class PXEnvironmentService
    {
        public const string ENV_PARAM_CLI_PIPE_NAME = "PX_ENV_CLI_PIPE_NAME";

        private JObject items;

        public PXEnvironmentService() {
            string envFilePath = AppDomain.CurrentDomain.BaseDirectory + ".env";

            if (File.Exists(envFilePath)) {
                items = JObject.Parse(File.ReadAllText(envFilePath));
            } else {
                items = new JObject();
            }
        }

        public string GetString(string key, string defaultValue = null) {
            if (!items.ContainsKey(key)) {
                return defaultValue;
            }

            return (string)items[key];
        }

        public int GetInt(string key, int defaultValue = 0) {
            if (!items.ContainsKey(key)) {
                return defaultValue;
            }

            return (int)items[key];
        }
    }
}
