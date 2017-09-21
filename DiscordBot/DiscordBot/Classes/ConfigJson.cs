using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Classes
{
    class ConfigJson
    {
        [JsonProperty("TokenDiscord")]
        public string TokenDiscord { get; private set; }
        [JsonProperty("ApiKeyYoutube")]
        public string ApiKeyYoutube { get; private set; }
        [JsonProperty("ApplicationNameYoutube")]
        public string ApplicationNameYoutube { get; private set; }
        [JsonProperty("ApiKeyPUBG")]
        public string ApiKeyPUBG { get; private set; }

        public static ConfigJson GetConfigJson()
        {
            var json = "";
            using (var fs = File.OpenRead("../../config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = sr.ReadToEnd();

            return JsonConvert.DeserializeObject<ConfigJson>(json);
        }
    }
}
