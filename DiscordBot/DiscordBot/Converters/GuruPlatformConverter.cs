using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DiscordBot.Enums;

namespace DiscordBot.Converters
{
    class GuruPlatformConverter : IArgumentConverter<GuruPlatform>
    {
        public bool TryConvert(string value, CommandContext ctx, out GuruPlatform result)
        {
            switch (value)
            {
                case "pc":
                    result = GuruPlatform.PC;
                    return true;
                case "ps":
                    result = GuruPlatform.PS;
                    return true;
                case "xb":
                    result = GuruPlatform.XB;
                    return true;
                default:
                    result = GuruPlatform.PC;
                    return false;
            }
        }
    }
}
