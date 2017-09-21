using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiscordBot.Classes
{
    class Urban
    {
        public List<string> Tags { get; set; }
        public string ResultType { get; set; }
        public List<List> List { get; set; }
        public List<string> Sounds { get; set; }
    }

    class List
    {
        public string Definition { get; set; }
        public string Permalink { get; set; }
        [JsonProperty(propertyName: "thumbs_up")]
        public int ThumbsUp { get; set; }
        public string Author { get; set; }
        public string Word { get; set; }
        public int DefId { get; set; }
        public string CurrentVote { get; set; }
        public string Example { get; set; }
        [JsonProperty(propertyName: "thumbs_down")]
        public int ThumbsDown { get; set; }
    }
}
