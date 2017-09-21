using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Classes
{
    class Weather
    {
        public string Day { get; set; }
        public string TempHigh { get; set; }
        public string TempLow { get; set; }
        public MagickImage Icon { get; set; }
    }
}
