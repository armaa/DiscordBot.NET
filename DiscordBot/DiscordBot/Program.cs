using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DiscordBot.Enums;
using DiscordBot.Converters;
using DiscordBot.Commands;
using AngleSharp;
using Humanizer;
using DSharpPlus.Net.WebSocket;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using ImageMagick;
using DiscordBot.Classes;
using DSharpPlus.Interactivity;

namespace DiscordBot
{
    class Program
    {
        private ConfigJson cfg = ConfigJson.GetConfigJson();
        public static DiscordClient client;
        public static CommandsNextModule commands;

        static void Main(string[] args)
        {
            Program program = new Program();
            program.Bot().GetAwaiter().GetResult();
        }

        public async Task Bot()
        {
            client = new DiscordClient(new DiscordConfiguration()
            {
                AutoReconnect = true,
                LargeThreshold = 150,
                LogLevel = LogLevel.Debug,
                Token = cfg.TokenDiscord,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true
            });

            client.Ready += Client_Ready;
            client.GuildAvailable += Client_GuildAvailable;
            client.ClientErrored += Client_ClientErrored;
            client.GuildMemberAdded += Client_GuildMemberAdded;
            client.MessageCreated += Client_MessageCreated;
            client.GuildMemberUpdated += Client_GuildMemberUpdated;

            commands = client.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefix = "!",
                EnableDms = true,
                EnableMentionPrefix = true,
                CaseSensitive = true,
                SelfBot = false,
                EnableDefaultHelp = true
            });

            commands.CommandExecuted += Commands_CommandExecuted;
            commands.CommandErrored += Commands_CommandErrored;

            CommandsNextUtilities.RegisterConverter(new GuruPlatformConverter());
            CommandsNextUtilities.RegisterUserFriendlyTypeName<GuruPlatform>("platform");

            commands.RegisterCommands<PublicCommands>();
            client.UseInteractivity();

            // Required if running on w7
            client.SetWebSocketClient<WebSocket4NetClient>();

            await client.ConnectAsync();

            // Makes bot be connected all the time until shut down manually
            await Task.Delay(-1);
        }

        private async Task Client_GuildMemberUpdated(GuildMemberUpdateEventArgs e)
        {
            DiscordMember bot = await e.Guild.GetMemberAsync(258902871720984577);
            var channel = e.Guild.Channels.FirstOrDefault(c => c.PermissionsFor(bot).ToPermissionString().Contains("Send message") == true);

            if (e.NicknameAfter == null && e.NicknameBefore == null)
            {
                if (e.RolesAfter.Count > e.RolesBefore.Count)
                {
                    foreach (var role in e.RolesAfter)
                    {
                        if (!e.RolesBefore.Contains(role))
                        {
                            await channel.SendMessageAsync($"User `{ e.Member.Username }` has received an additional role `{ role.Name }`.");
                        }
                    }
                }
                else
                {
                    foreach (var role in e.RolesBefore)
                    {
                        if (!e.RolesBefore.Contains(role))
                        {
                            await channel.SendMessageAsync($"User `{ e.Member.Username }` has been removed a role `{ role.Name }`.");
                        }
                    }
                }
            }
            else
            {
                await channel.SendMessageAsync($"User `{ e.Member.Username }` changed his nickname, from `{ e.NicknameBefore ?? e.Member.Username }` to `{ e.NicknameAfter ?? e.Member.Username }`");
            }
        }

        private async Task Client_GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", $"New member joined { e.Guild.Name } guild.", DateTime.Now);

            DiscordMember bot = await e.Guild.GetMemberAsync(258902871720984577);
            var channel = e.Guild.Channels.FirstOrDefault(c => c.PermissionsFor(bot).ToPermissionString().Contains("Send message") == true);

            await channel.SendMessageAsync($"Welcome our new member { e.Member.Mention }!");
        }

        private Task Client_Ready(ReadyEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", "Client is ready to process events.", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", $"Guild available: {e.Guild.Name}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Client_ClientErrored(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "Bot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message} ({ e.Exception.InnerException }", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "Bot", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            if (e.Exception is ChecksFailedException ex)
            {
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                var embed = new DiscordEmbedBuilder().WithTitle("Access denied")
                    .WithDescription($"{emoji} You do not have the permissions required to execute this command.")
                    .WithColor(DiscordColor.Red);

                await e.Context.RespondAsync("", embed: embed);
            }
            else
            {
                await e.Context.Channel.SendMessageAsync($"Exception occured:\n```{e.Exception.GetType()}: {e.Exception.Message}```If you dont know what this means, call armaa to explain it!");
            }
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (!e.Author.IsBot)
            {
                if (e.Message.Content.StartsWith("^"))
                {
                    var messages = await e.Channel.GetMessagesAsync(2);
                    var messageId = messages.LastOrDefault().Id;
                    var message = await e.Channel.GetMessageAsync(messageId);
                    await e.Channel.SendMessageAsync($"^ { message.Author.Mention }-San! is right, you know?");
                }

                else if (e.Message.Content.ToLower().StartsWith("same") || (e.Message.Content.ToLower().Split(' ').Skip(1).FirstOrDefault()?.Equals("same") ?? false))
                {
                    await e.Channel.SendMessageAsync("same");
                }

                else if (e.Message.Content.ToLower().StartsWith("tru") || e.Message.Content.ToLower().StartsWith("true"))
                {
                    await e.Channel.SendMessageAsync("tru");
                }

                else if (e.Message.Content.ToLower().StartsWith("https://boards.4chan.org/"))
                {
                    var urlArray = e.Message.Content.ToLower().TakeWhile(m => !m.Equals('#')).ToArray();
                    var url = new string(urlArray);

                    var req = new AngleSharp.Network.Default.HttpRequester();

                    req.Headers["Accept-Language"] = "en-gb";
                    req.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:43.0) Gecko/20100101 Firefox/43.0";
                    var conf = Configuration.Default.WithDefaultLoader(requesters: new[] { req });

                    var doc = await BrowsingContext.New(conf).OpenAsync(url);

                    var selectorImage = "a.fileThumb";
                    var selectorPost = "blockquote.postMessage";

                    var selectedImage = $"https:{ doc.QuerySelector(selectorImage).GetAttribute("href") }";
                    var selectedPost = doc.QuerySelector(selectorPost);

                    DiscordEmbed embed = new DiscordEmbedBuilder().WithColor(DiscordColor.Lilac)
                        .WithDescription($"Preview for [link]({ url }) posted by `{ e.Author.Username }#{ e.Author.Discriminator }`")
                        .WithTimestamp(DateTime.Now)
                        .AddField("Op message", selectedPost.TextContent.Truncate(500, "[...]", Truncator.FixedNumberOfCharacters))
                        .WithAuthor(e.Author.Username, e.Author.AvatarUrl, e.Author.AvatarUrl)
                        .WithFooter($"Requested by { e.Author.Username }#{ e.Author.Discriminator }", e.Author.AvatarUrl)
                        .WithImageUrl(selectedImage);

                    await e.Message.DeleteAsync();
                    await e.Channel.SendMessageAsync("", embed: embed);
                }

                else if (e.Message.Content.ToLower().StartsWith("halp"))
                {
                    await e.Channel.SendFileAsync("../../Files/Pictures/halp.jpg");
                }

                else if (e.Message.Content.ToLower().Contains("*rubs hands*") || e.Message.Content.ToLower().Contains("good goy"))
                {
                    await e.Channel.SendFileAsync("../../Files/Pictures/good goy.jpg");
                }
            }
        }
    }
}
