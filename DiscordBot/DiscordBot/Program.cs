using System;
using System.Collections.Generic;
using System.Linq;
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
            var program = new Program();
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
            client.UseInteractivity(new InteractivityConfiguration());

            // Required if running on w7
            client.SetWebSocketClient<WebSocket4NetClient>();

            await client.ConnectAsync();

            // Makes bot be connected all the time until shut down manually
            await Task.Delay(-1);
        }

        private async Task Client_GuildMemberUpdated(GuildMemberUpdateEventArgs e)
        {
            var bot = await e.Guild.GetMemberAsync(258902871720984577);
            var channel = GetChannelToPostNotificationIn(e, bot);

            if (e.NicknameAfter == null && e.NicknameBefore == null || e.NicknameBefore == e.NicknameAfter)
            {
                if (e.RolesAfter.Count > e.RolesBefore.Count)
                {
                    foreach (var role in e.RolesAfter)
                    {
                        if (!e.RolesBefore.Contains(role))
                        {
                            var embed = new DiscordEmbedBuilder().WithColor(DiscordColor.Green)
                                .WithAuthor(e.Member.Username, e.Member.AvatarUrl, e.Member.AvatarUrl)
                                .AddField("Role Assigned", $"`{ role.Name }`, Role Id: { role.Id }")
                                .AddField("Role Permissions", role.Permissions.ToPermissionString());
                            await channel.SendMessageAsync(embed: embed);
                        }
                    }
                }
                else
                {
                    foreach (var role in e.RolesBefore)
                    {
                        if (!e.RolesAfter.Contains(role))
                        {
                            var embed = new DiscordEmbedBuilder().WithColor(DiscordColor.Red)
                                .WithAuthor(e.Member.Username, e.Member.AvatarUrl, e.Member.AvatarUrl)
                                .AddField("Role Removed", $"`{ role.Name }`, Role Id: { role.Id }")
                                .AddField("Role Permissions", role.Permissions.ToPermissionString());
                            await channel.SendMessageAsync(embed: embed);
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

            var bot = await e.Guild.GetMemberAsync(258902871720984577);
            var channel = GetChannelToPostNotificationIn(e, bot);

            var embed = new DiscordEmbedBuilder().WithColor(DiscordColor.Green)
                .WithAuthor(e.Member.Username, e.Member.AvatarUrl, e.Member.AvatarUrl)
                .AddField("New Member Joined", $"Welcome to our new member { e.Member.Mention }")
                .AddField("Account Creation Date", $"{ e.Member.CreationTimestamp.ToString("dd/MM/yyyy HH:mm:ss") } ({ e.Member.CreationTimestamp.Humanize() })");

            await channel.SendMessageAsync(embed: embed);

            if (e.Guild.Id == 227847810152792064)
            {
                _ = Task.Run(async () => await AssignRoleToAMember(e, channel));
            }
        }

        private async Task AssignRoleToAMember(GuildMemberAddEventArgs e, DiscordChannel channel)
        {
            var interactivity = client.GetInteractivityModule();
            var msg = await channel.SendMessageAsync("Please take your time and select the region you usually play in.");

            var emojiUS = DiscordEmoji.FromName(client, ":flag_us:");
            var emojiEU = DiscordEmoji.FromName(client, ":flag_eu:");
            var emojiSEA = DiscordEmoji.FromName(client, ":ocean:");
            var emojiBR = DiscordEmoji.FromName(client, ":flag_br:");
            var emojiRU = DiscordEmoji.FromName(client, ":flag_ru:");
            var emojiAU = DiscordEmoji.FromName(client, ":flag_au:");
            var emojiList = new List<DiscordEmoji>() { emojiUS, emojiEU, emojiSEA, emojiBR, emojiRU, emojiAU };

            foreach (var emoji in emojiList)
            {
                await msg.CreateReactionAsync(emoji);
                await Task.Delay(250);
            }

            var reaction = await interactivity.WaitForReactionAsync(xe => emojiList.Contains(xe), e.Member, TimeSpan.FromSeconds(60));

            if (reaction != null)
            {
                switch (reaction.Emoji.GetDiscordName())
                {
                    //  :flag_us: emoji
                    case ":flag_us:":
                        await AssignRoleToAMemberAsync(e, "NA");
                        break;
                    //  :flag_eu: emoji
                    case ":flag_eu:":
                        await AssignRoleToAMemberAsync(e, "EU");
                        break;
                    //  :ocean: emoji, representing SEA region for now..
                    case ":ocean:":
                        await AssignRoleToAMemberAsync(e, "SEA");
                        break;
                    //  :flag_br: emoji
                    case ":flag_br:":
                        await AssignRoleToAMemberAsync(e, "BR");
                        break;
                    //  :flag_ru: emoji
                    case ":flag_ru:":
                        await AssignRoleToAMemberAsync(e, "RU-S-A");
                        break;
                    //  :flag_au: emoji
                    case ":flag_au:":
                        await AssignRoleToAMemberAsync(e, "AUS");
                        break;
                    default:
                        await channel.SendMessageAsync("It broke..");
                        break;
                }

                await channel.SendMessageAsync("All done! If you dont mind playing in multiple regions, message the server admin or one of the mods to assign you the role needed.");
            }
            else
            {
                await channel.SendMessageAsync("Very well then, if you wish to have a role assigned later on, message the server admin or one of the mods.");
            }
        }

        private Task Client_Ready(ReadyEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", "Client is ready to process events.", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", $"Guild available: { e.Guild.Name }", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Client_ClientErrored(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "Bot", $"Exception occured: { e.Exception.GetType() }: { e.Exception.Message } ({ e.Exception.InnerException }", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", $"{ e.Context.User.Username } successfully executed '{ e.Command.QualifiedName }'", DateTime.Now);

            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "Bot", $"{ e.Context.User.Username } tried executing '{ e.Command?.QualifiedName ?? "<unknown command>" }' but it errored: { e.Exception.GetType() }: { e.Exception.Message ?? "<no message>" }", DateTime.Now);

            if (e.Exception is ChecksFailedException ex)
            {
                if (ex.FailedChecks.FirstOrDefault(xe => xe is DSharpPlus.CommandsNext.Attributes.CooldownAttribute) != null)
                {
                    await e.Context.RespondAsync("Command on cooldown, please wait a little bit before calling it again");
                    return;
                }

                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                var embed = new DiscordEmbedBuilder().WithTitle("Access denied")
                    .WithDescription($"{ emoji } You do not have the permissions required to execute this command.")
                    .WithColor(DiscordColor.Red);

                await e.Context.RespondAsync("", embed: embed);
            }
            else if (!(e.Exception is CommandNotFoundException))
            {
                var embed = new DiscordEmbedBuilder()
                    .AddField("An exception occured when executing a command", e.Exception.GetType().ToString())
                    .AddField("Error message", e.Exception.Message)
                    .WithColor(DiscordColor.Red)
                    .WithTimestamp(DateTime.Now)
                    .WithFooter($"{ client.CurrentUser.Username }#{ client.CurrentUser.Discriminator }", client.CurrentUser.AvatarUrl);

                await e.Context.Channel.SendMessageAsync(embed: embed);
            }
            else
            {
                await e.Context.Channel.SendMessageAsync("Command doesnt exist");
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
                    e.Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", $"{ e.Author.Username } successfully executed 'same'", DateTime.Now);
                    await e.Channel.SendMessageAsync("same");
                }

                else if (e.Message.Content.ToLower().StartsWith("tru") || e.Message.Content.ToLower().StartsWith("true"))
                {
                    e.Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", $"{ e.Author.Username } successfully executed 'true'", DateTime.Now);
                    await e.Channel.SendMessageAsync("tru");
                }

                else if (e.Message.Content.ToLower().StartsWith("https://boards.4chan.org/"))
                {
                    var urlArray = e.Message.Content.ToLower().TakeWhile(xc => !xc.Equals(' ')).ToArray();
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

                    var embed = new DiscordEmbedBuilder().WithColor(DiscordColor.Lilac)
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
                    e.Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", $"{ e.Author.Username } successfully executed 'halp'", DateTime.Now);
                    await e.Channel.SendFileAsync("../../Files/Pictures/halp.jpg");
                }

                else if (e.Message.Content.ToLower().Contains("*rubs hands*") || e.Message.Content.ToLower().Contains("good goy"))
                {
                    e.Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", $"{ e.Author.Username } successfully executed 'goy'", DateTime.Now);
                    await e.Channel.SendFileAsync("../../Files/Pictures/good goy.jpg");
                }
            }
        }

        private async Task AssignRoleToAMemberAsync(GuildMemberAddEventArgs e, string roleName)
        {
            var role = e.Guild.Roles.Where(xr => xr.Name == roleName).FirstOrDefault();
            var member = e.Member;
            await member.GrantRoleAsync(role);
        }

        private DiscordChannel GetChannelToPostNotificationIn(GuildMemberUpdateEventArgs e, DiscordMember bot)
        {
            var channel = e.Guild.Channels.FirstOrDefault(xc => xc.Name.ToLower().Contains("bot") || xc.Name.ToLower().Contains("test"));

            if (channel == null)
            {
                channel = e.Guild.Channels.FirstOrDefault(xc => xc.PermissionsFor(bot).ToPermissionString().Contains("Send message") == true);
            }

            return channel;
        }

        private DiscordChannel GetChannelToPostNotificationIn(GuildMemberAddEventArgs e, DiscordMember bot)
        {
            var channel = e.Guild.Channels.FirstOrDefault(xc => xc.Name.ToLower().Contains("bot") || xc.Name.ToLower().Contains("test"));

            if (channel == null)
            {
                channel = e.Guild.Channels.FirstOrDefault(xc => xc.PermissionsFor(bot).ToPermissionString().Contains("Send message") == true);
            }

            return channel;
        }
    }
}
