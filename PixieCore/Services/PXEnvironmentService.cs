using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pixie.Core.Services {
    public class PXEnvironmentService {
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

        public int GetInt(string key) {
            return (int)items[key];
        }
    }
}
