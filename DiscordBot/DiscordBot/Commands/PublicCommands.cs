using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DiscordBot.Enums;
using Newtonsoft.Json;
using DiscordBot.Classes;
using Humanizer;
using Humanizer.Localisation;
using Newtonsoft.Json.Linq;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using System.Threading;
using AngleSharp;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using ImageMagick;
using PUBGSharp;
using PUBGSharp.Helpers;
using PUBGSharp.Net.Model;
using System.Net.Http;
using PUBGSharp.Data;

namespace DiscordBot.Commands
{
    class PublicCommands
    {
        private static Random rnd = new Random();
        private DateTime startTime = DateTime.Now;
        private DateTime cooldown = DateTime.Now;
        private string cooldownTimerLeft;
        private IConfiguration angleSharpConfiguration = AngleSharpConfigurationWithUserAgent();
        private ConfigJson cfg = ConfigJson.GetConfigJson();
        private Permissions mutedRolePermissions = Permissions.AccessChannels | Permissions.ChangeNickname | Permissions.CreateInstantInvite | Permissions.ReadMessageHistory;
        private Permissions assignRolePermissions = Permissions.Administrator | Permissions.BanMembers | Permissions.DeafenMembers | Permissions.KickMembers | Permissions.ManageChannels | Permissions.ManageEmojis
            | Permissions.ManageGuild | Permissions.ManageMessages | Permissions.ManageNicknames | Permissions.ManageRoles | Permissions.ManageWebhooks | Permissions.MoveMembers | Permissions.MuteMembers | Permissions.ViewAuditLog;

        [Command("uptime")]
        [Description("Gives the time how long has the bot been running for")]
        public async Task Uptime(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var uptime = TimeSpan.FromMilliseconds(DateTime.Now.Subtract(startTime).TotalMilliseconds);
            var humanizedUptime = uptime.Humanize(7, minUnit: TimeUnit.Second, maxUnit: TimeUnit.Year).Humanize(LetterCasing.Title);

            await ctx.RespondAsync(humanizedUptime);
        }

        [Command("ping")]
        [Description("Example ping command")]
        [Aliases("pong")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var emoji = DiscordEmoji.FromName(ctx.Client, ":ping_pong:");

            await ctx.RespondAsync($"{ emoji } Pong! Ping: { ctx.Client.Ping }ms");
        }

        [Command("guru")]
        [Description("Gets the paladins.guru profile of selected user name")]
        [Aliases("paladinsguru")]
        public async Task Guru(CommandContext ctx, [Description("User name which you wanna check")] string userName, [Description("Platform on which you wish to search the name on, PS for ps4 and XB for xbox")] GuruPlatform platform = GuruPlatform.PC)
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            if (userName.Equals(""))
            {
                await ctx.RespondAsync("Please put a valid username to look up");
                return;
            }

            var url = "";

            switch (platform)
            {
                case GuruPlatform.PC:
                    url = $"http://paladins.guru/profile/pc/{ userName }";
                    break;
                case GuruPlatform.PS:
                    url = $"http://paladins.guru/profile/ps/{ userName }";
                    break;
                case GuruPlatform.XB:
                    url = $"http://paladins.guru/profile/xb/{ userName }";
                    break;
            }

            try
            {
                var docMain = await BrowsingContext.New(angleSharpConfiguration).OpenAsync(url);
                var docRank = await BrowsingContext.New(angleSharpConfiguration).OpenAsync($"{ url }/ranked");
                var docCasual = await BrowsingContext.New(angleSharpConfiguration).OpenAsync($"{ url }/casual");

                var selectorMain = "div.widget-content.stat div";
                var selectorRanked = "div.col-xs-7 div span";
                var selectorCasual = "span.lg-text";
                var selectorHeroes = "table.champion-side-table tbody tr";
                var selectorRank = "div.col-xs-5 div";

                var selectedMain = docMain.QuerySelectorAll(selectorMain);
                var selectedRanked = docRank.QuerySelectorAll(selectorRanked);
                var selectedCasual = docCasual.QuerySelectorAll(selectorCasual);

                var playTime = selectedMain[0].InnerHtml.Trim();
                var winrate = selectedMain[2].InnerHtml.Trim();
                var kda = selectedMain[3].InnerHtml.Trim();
                var elo = selectedCasual[0].InnerHtml.Trim();
                var winrateRanked = "";
                var eloRanked = "";
                var rank = "";

                try
                {
                    rank = docRank.QuerySelector(selectorRank).TextContent.Trim();
                    eloRanked = selectedRanked[0].InnerHtml.Trim();
                    winrateRanked = selectedRanked[4].InnerHtml.Trim();
                }
                catch (Exception)
                {

                    rank = "None";
                    eloRanked = "None";
                    winrateRanked = "None";
                }

                var topHeroes = docMain.QuerySelectorAll(selectorHeroes);
                var topFiveHeroes = new StringBuilder();

                foreach (var hero in topHeroes)
                {
                    var heroName = hero.QuerySelector("span.name").TextContent.Trim();
                    var heroPosition = hero.QuerySelector("div.god").ChildNodes[4].TextContent.Trim();
                    var otherHeroInfo = hero.QuerySelectorAll("td.text-center div");
                    var heroKda = otherHeroInfo[0].TextContent.Trim();
                    var heroKdaTotal = otherHeroInfo[1].TextContent.Trim();
                    var heroWinrate = otherHeroInfo[2].TextContent.Trim();
                    var heroHours = otherHeroInfo[3].TextContent.Trim();
                    topFiveHeroes.Append($"{ heroName } ({ heroPosition })  { heroKda } ({ heroKdaTotal })  { heroWinrate }  { heroHours }\n");
                }

                using (MagickImage i = new MagickImage("../../Files/Pictures/paladins-original.png"))
                {
                    new Drawables().FontPointSize(15)
                        .Font("Verdana")
                        .FillColor(MagickColors.White)
                        .Text(5, 18, $"Play time: { playTime }")
                        .Text(5, 38, $"Win rate: { winrate }")
                        .Text(5, 58, $"Elo: { elo }")
                        .Text(5, 78, "Top 5 champions: ")
                        .Text(5, 98, $"{ topFiveHeroes.ToString() }")
                        .Text(315, 18, $"Rank: { rank }")
                        .Text(315, 38, $"Win rate ranked: { winrateRanked }")
                        .Text(315, 58, $"Elo ranked: { eloRanked }")
                        .Text(615, 18, "Quick info")
                        .Draw(i);

                    i.Write("../../Files/Pictures/paladins.png");
                }

                await ctx.RespondWithFileAsync("../../Files/Pictures/paladins.png", url);
            }
            catch (Exception)
            {
                await ctx.RespondAsync($"{ url }\nNo quick info because either profile doesnt exist or something is missing..");
            }
        }

        [Command("answer")]
        [Description("Answers a question for you with a simple yes or no")]
        [Aliases("question")]
        public async Task Answer(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            if (rnd.NextDouble() < 0.5)
                await ctx.RespondAsync("I would say yes..");
            else
                await ctx.RespondAsync("Maybe not..");
        }

        [Command("cointoss")]
        [Description("Flips a coin")]
        [Aliases("coinflip", "coin", "flip")]
        public async Task Flip(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            string coinSide = rnd.NextDouble() < 0.5 ? "`Heads`" : "`Tails`";

            await ctx.RespondAsync($"Coin lands on { coinSide }");
        }

        [Command("urban")]
        [Description("Gets a random definition and an example for a term")]
        public async Task Urban(CommandContext ctx, [RemainingText][Description("Term you wanna look up")] string term)
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            if (term.Equals(""))
            {
                await ctx.RespondAsync("Please put a valid term to look up");
                return;
            }

            using (HttpClient client = new HttpClient())
            {
                var value = await client.GetStringAsync($"http://api.urbandictionary.com/v0/define?term={ term }");
                var json = JsonConvert.DeserializeObject<Urban>(value);

                var randomDescription = json.List.ElementAt(rnd.Next(json.List.Count));

                var embed = new DiscordEmbedBuilder().WithColor(DiscordColor.Lilac)
                    .WithAuthor(ctx.Member.Username, ctx.Member.AvatarUrl, ctx.Member.AvatarUrl)
                    .WithDescription($"{ randomDescription.Word } by { randomDescription.Author }")
                    .AddField("Definition", randomDescription.Definition.Truncate(400, "[...]", Truncator.FixedNumberOfCharacters))
                    .AddField("Example", randomDescription.Example.Truncate(400, "[...]", Truncator.FixedNumberOfCharacters))
                    .AddField($"More examples for this word: { randomDescription.Permalink }", $":thumbsup: { randomDescription.ThumbsUp } | :thumbsdown: { randomDescription.ThumbsDown }")
                    .WithFooter($"Requested by { ctx.Member.Username }#{ ctx.Member.Discriminator }", ctx.Member.AvatarUrl)
                    .WithTimestamp(DateTime.Now);

                await ctx.RespondAsync("", embed: embed);
            }
        }

        [Command("ready")]
        [Description("READY")]
        [Aliases("eyesight", "sight", "shalin")]
        public async Task Ready(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var url = "https://paladins.gamepedia.com/media/paladins.gamepedia.com/5/59/Sha_Lin_SP_Ability2_2.ogg";

            await ctx.RespondWithFileAsync("../../Files/Pictures/READY.jpg", url);
        }

        [Command("challenge")]
        [Description("CHALLENGE")]
        [Aliases("turtle", "makoa")]
        public async Task Challenge(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var url = "https://paladins.gamepedia.com/media/paladins.gamepedia.com/2/22/Makoa_Plushy_TR_TakingDamage_2.ogg";

            var message = await ctx.RespondWithFileAsync("../../Files/Pictures/CHALLENGE.jpg", url);

            var emojiMuscle = DiscordEmoji.FromName(ctx.Client, ":muscle:");
            var emojiTurtle = ctx.Message.Content.StartsWith("!makoa") ?
                DiscordEmoji.FromName(ctx.Client, ":makoa:") :
                DiscordEmoji.FromName(ctx.Client, ":turtle:");
            var emojiAnchor = DiscordEmoji.FromName(ctx.Client, ":anchor:");

            await message.CreateReactionAsync(emojiMuscle);
            await message.CreateReactionAsync(emojiTurtle);
            await message.CreateReactionAsync(emojiAnchor);
        }

        [Command("mal")]
        [Description("Selects a random of 5 animes out of top 50 from MyAnimeList")]
        public async Task Mal(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            var url = "https://myanimelist.net/topanime.php";
            var doc = await BrowsingContext.New(angleSharpConfiguration).OpenAsync(url);
            var selectorTitles = "div.di-ib.clearfix a:nth-child(1)";
            var selectorScores = "div.js-top-ranking-score-col.di-ib.al span";
            var selectedTitles = doc.QuerySelectorAll(selectorTitles);
            var selectedScores = doc.QuerySelectorAll(selectorScores);
            var titles = selectedTitles.Select(xt => xt.TextContent).ToList();
            var scores = selectedScores.Select(xs => xs.TextContent).ToList();

            var randomNumbers = GetRandomDistinctNumbers(0, 50, 5);

            var sb = new StringBuilder();
            sb.Append("```");

            foreach (var number in randomNumbers)
            {
                sb.Append($"#{ number + 1 }: { titles[number] }, Score: { scores[number] }\n");
            }

            sb.Append("```");

            await ctx.RespondAsync($"Random top 5 animes are (which you should probably watch):\n{ sb.ToString() }");
        }

        [Command("google")]
        [Description("Makes a google search for you")]
        [Aliases("lmgtfy")]
        public async Task Google(CommandContext ctx, [RemainingText][Description("Term you want to check up")] string term)
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            if (term.Equals(""))
            {
                await ctx.RespondAsync("Please put a valid term to look up");
                return;
            }

            term = term.Replace(" ", "+");
            await ctx.RespondAsync($"https://lmgtfy.com/?q={ term }");
        }

        [Command("weather")]
        [Description("Gives the current weather statistics for location provided")]
        public async Task Weather(CommandContext ctx, [RemainingText][Description("Location you want to get the statistics for")] string location)
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            if (location.Equals(""))
            {
                await ctx.RespondAsync("Please put a valid location");
                return;
            }

            var doc = await BrowsingContext.New(angleSharpConfiguration).OpenAsync($"https://www.google.com/search?q=weather+{ location }");

            var selectorCurrentTemp = "#wob_tm";
            var selectorLocation = "#wob_loc";
            var selectorMainWeatherIcon = "#wob_tci";
            var selectorDateTime = "#wob_dts";
            var selectorPrecipitation = "#wob_pp";
            var selectorHumidity = "#wob_hm";
            var selectorWind = "#wob_ws";
            var selectorWeek = "div.wob_df";
            var selectorTempToday = "span.wob_t";

            var selectedCurrentTemp = doc.QuerySelector(selectorCurrentTemp).TextContent;
            var selectedLocation = doc.QuerySelector(selectorLocation).TextContent;
            var selectedMainWeatherIcon = doc.QuerySelector(selectorMainWeatherIcon);
            var selectedDateTime = doc.QuerySelector(selectorDateTime).TextContent;
            var selectedPrecipitation = doc.QuerySelector(selectorPrecipitation).TextContent;
            var selectedHumidity = doc.QuerySelector(selectorHumidity).TextContent;
            var selectedWind = doc.QuerySelector(selectorWind).TextContent;
            var selectedWeek = doc.QuerySelectorAll(selectorWeek);
            var todayTempHigh = selectedWeek.FirstOrDefault().QuerySelectorAll(selectorTempToday)[0].TextContent;
            var todayTempLow = selectedWeek.FirstOrDefault().QuerySelectorAll(selectorTempToday)[2].TextContent;

            //.GetStreamAsync()
            var client = new HttpClient();
            var mainWeatherIconStream = await client.GetStreamAsync($"https:{ selectedMainWeatherIcon.GetAttribute("src") }");
            var mainWeatherIcon = new MagickImage(mainWeatherIconStream);
            var weatherList = new List<Weather>();
            var counter = 0;

            for (int i = 1; i < selectedWeek.Length; i++)
            {
                var w = new Weather();
                w.Day = selectedWeek[i].QuerySelector("div.vk_lgy").GetAttribute("aria-label");

                var temperatures = selectedWeek[i].QuerySelectorAll("span.wob_t");
                w.TempHigh = temperatures[0].TextContent;
                w.TempLow = temperatures[2].TextContent;

                var weatherIconUrl = $"https:{ selectedWeek[i].QuerySelector("div.wob_df div img").GetAttribute("src") }";
                var weatherIconStream = await client.GetStreamAsync(weatherIconUrl);
                w.Icon = new MagickImage(weatherIconStream);
                w.Icon.Write($"../../Files/Pictures/WeatherIconsFilename/{ weatherIconUrl.Substring(weatherIconUrl.LastIndexOf('/')) }");
                w.Icon.Write($"../../Files/Pictures/WeatherIcons/{ selectedWeek[i].QuerySelector("div.wob_df div img").GetAttribute("alt") }.png");
                weatherList.Add(w);
            }

            using (MagickImage i = new MagickImage("../../Files/Pictures/weather-original.png"))
            {
                var image = new Drawables().FontPointSize(12)
                    .Font("Verdana")
                    .FillColor(MagickColors.White)
                    .Text(5, 18, selectedLocation)
                    .Text(5, 33, $"High of { todayTempHigh }°C, low of { todayTempLow }°C")
                    .Text(5, 48, "Precipitation")
                    .Text(5, 63, "Humidity")
                    .Text(5, 78, "Wind")
                    .Text(5, 93, "Next 7 days")
                    .Text(85, 48, selectedPrecipitation)
                    .Text(85, 63, selectedHumidity)
                    .Text(85, 78, selectedWind)
                    .Text(240, 78, selectedDateTime)
                    .Composite(260, 10, CompositeOperator.Atop, new MagickImage(mainWeatherIcon))
                    .FontPointSize(16)
                    .Text(305, 20, $"{ selectedCurrentTemp }°C");

                foreach (var w in weatherList)
                {
                    image.FontPointSize(8)
                        .Font("Verdana")
                        .FillColor(MagickColors.White)
                        .Text((counter * 50) + 10, 103, w.Day)
                        .Text((counter * 50) + 5, 148, $"{ w.TempHigh }°C /{ w.TempLow }°C")
                        .Composite((counter * 50) + 5, 100, CompositeOperator.Atop, w.Icon);

                    counter++;
                }

                image.Draw(i);
                i.Write("../../Files/Pictures/weather.png");
            }

            await ctx.RespondWithFileAsync("../../Files/Pictures/weather.png");
        }

        [Command("youtube")]
        [Description("Links a top video for the term you search for")]
        [Aliases("yt")]
        public async Task Youtube(CommandContext ctx, [RemainingText][Description("The term you want to look up")] string term)
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            if (term.Equals(""))
            {
                await ctx.RespondAsync("Please put a valid term to look up");
                return;
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = cfg.ApiKeyYoutube,
                ApplicationName = cfg.ApplicationNameYoutube,
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = term;
            searchListRequest.MaxResults = 5;

            var searchListResponse = await searchListRequest.ExecuteAsync();
            searchListResponse.Items.Select(xi => xi.Id.Kind == "youtube#video").ToList();
            var searchedVideo = searchListResponse.Items.First();
            var videoTitle = searchedVideo.Snippet.Title;
            var videoPublishDate = searchedVideo.Snippet.PublishedAt.Value.ToOrdinalWords();
            var videoDescription = searchedVideo.Snippet.Description.Truncate(500, "[...]", Truncator.FixedNumberOfCharacters);
            var videoUrl = $"https://www.youtube.com/watch?v={ searchedVideo.Id.VideoId }";

            await ctx.RespondAsync($" ** { videoTitle } **\n `{ videoPublishDate } - { videoDescription }`\n { videoUrl }");
        }

        [Command("members")]
        [Description("Shows a list of all members currently in the server, their name, nickname and date joined")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task Members(CommandContext ctx)
        {
            var sb = new StringBuilder();
            var allMembers = await ctx.Guild.GetAllMembersAsync();
            allMembers.OrderBy(xm => xm.Username)
                .ToList()
                .ForEach(xm => AppendMemberToString(xm, sb));

            var client = new HttpClient();
            var content = new StringContent(sb.ToString());
            var response = await client.PostAsync("https://hastebin.com/documents", content);
            var jsonToDeserialize = await response.Content.ReadAsStringAsync();
            var json = JToken.Parse(jsonToDeserialize);
            var key = json.SelectToken("key").ToString();

            await ctx.Member.SendMessageAsync($"https://hastebin.com/{ key }");
        }

        [Command("roll")]
        [Description("Simulates a dice roll for you.")]
        [Aliases("r", "dice")]
        public async Task Roll(CommandContext ctx, [Description("Format should be: [rolls]d[sides]")] string dice = "1d6")
        {
            await ctx.TriggerTypingAsync();

            if (!dice.Contains('d'))
            {
                await ctx.RespondAsync("Wrong format. Correct format for roll should be `[rolls]d[sides]`");
                return;
            }

            var rolls = int.Parse(dice.Split('d').ElementAt(0));
            var sides = int.Parse(dice.Split('d').ElementAt(1));

            if (rolls < 1 || sides < 1)
            {
                await ctx.RespondAsync("Number of rolls and sides has to be atleast 1.");
                return;
            }

            if (rolls > 20 || sides > 20)
            {
                await ctx.RespondAsync("Dont you think thats bit too excessive?");
                return;
            }

            if (rolls == 1)
            {
                var roll = rnd.Next(sides) + 1;

                await ctx.RespondAsync($"You rolled: { roll }");
            }
            else
            {
                var totalRolls = Enumerable.Range(0, rolls)
                    .Select(xn => xn = (rnd.Next(sides) + 1)).ToArray();
                await ctx.RespondAsync($"You rolled: { totalRolls.Humanize() }");
            }
        }

        [Command("booru")]
        [Description("Takes a random picture off of danbooru")]
        public async Task Booru(CommandContext ctx, [RemainingText][Description("Tags you want to search, please stick to the booru convention of tag searching")] string tags)
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            var rating = ctx.Channel.IsNSFW ? "rating:explicit" : "rating:safe";

            if (tags.Equals(""))
            {
                await ctx.RespondAsync("Please put a valid tag to search");
                return;
            }

            using (HttpClient client = new HttpClient())
            {
                var jsonString = await client.GetStringAsync($"https://danbooru.donmai.us/posts.json?random=true&limit=20&tags={ rating } { tags }");
                var json = JArray.Parse(jsonString);

                if (!json.Any())
                {
                    await ctx.RespondAsync("No results, try another tag or try again");
                    return;
                }

                var pictureId = json.FirstOrDefault().SelectToken("id").ToString();

                await ctx.RespondAsync($"http://danbooru.donmai.us/posts/{ pictureId }");
            }
        }

        [Command("pubg")]
        [Description("Gives the stats for a specific player")]
        public async Task Pubg(CommandContext ctx, [Description("Name of the player")] string name, [Description("Mode you want to look the stats up for")] string mode = "SoloFpp", [Description("Region you want to look the stats up for")] string region = "AGG")
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            if (!Enum.TryParse(mode, true, out Mode pickedMode))
            {
                await ctx.RespondAsync("Wrong mode.. Modes you can use are ```Duo, DuoFpp, Solo, SoloFpp, Squad, SquadFpp```");
                return;
            }

            if (!Enum.TryParse(region, true, out Region pickedRegion))
            {
                await ctx.RespondAsync("Wrong region.. Regions you can use are ```AGG (all regions combined), AS, EU, NA, OC, SA, SEA```");
                return;
            }

            var client = new PUBGStatsClient(cfg.ApiKeyPUBG);
            var stats = await client.GetPlayerStatsAsync(name);
            var foundStats = stats.Stats.Find(xs => xs.Mode == pickedMode && xs.Region == pickedRegion && xs.Season == Seasons.EASeason5).Stats;

            var avatarUrl = stats.Avatar;
            avatarUrl = $"{ avatarUrl.Substring(0, avatarUrl.LastIndexOf('.')) }_medium.jpg";
            var playerName = stats.PlayerName;
            var kda = foundStats.Find(xs => xs.Stat == Stats.KDR);
            var wins = foundStats.Find(xs => xs.Stat == Stats.Wins);
            var winPercentage = foundStats.Find(xs => xs.Stat == Stats.WinPercentage);
            var roundsPlayed = foundStats.Find(xs => xs.Stat == Stats.RoundsPlayed);
            var top10Rate = foundStats.Find(xs => xs.Stat == Stats.Top10Rate);
            var top10Ratio = foundStats.Find(xs => xs.Stat == Stats.WinTop10Ratio);
            var rating = foundStats.Find(xs => xs.Stat == Stats.Rating);

            var list = new List<StatModel>()
            {
                kda, wins, winPercentage, roundsPlayed, top10Rate, top10Ratio, rating
            };

            var httpclient = new HttpClient();
            var avatarIcon = await httpclient.GetStreamAsync(avatarUrl);
            var avatar = new MagickImage(avatarIcon);
            using (MagickImage i = new MagickImage("../../Files/Pictures/stats-original.png"))
            {
                var image = new Drawables().FontPointSize(50)
                    .Font("Roboto")
                    .FillColor(MagickColors.White)
                    .TextAlignment(TextAlignment.Left)
                    .Composite(10, 15, CompositeOperator.Atop, avatar)
                    .Text(80, 50, playerName)
                    .FontPointSize(32)
                    .Text(81, 80, $"{ pickedMode.ToString() } - { pickedRegion.ToString() }")
                    .TextAlignment(TextAlignment.Center)
                    .Text(110, 180, "KDA")
                    .Text(110, 280, "Round No.")
                    .Text(300, 180, "Win No.")
                    .Text(300, 280, "Win %")
                    .Text(490, 180, "Top 10")
                    .Text(490, 280, "Top 10 Ratio")
                    .Text(490, 80, "Rating");

                foreach (var item in list)
                {
                    var color = GetColorBasedOnPercentile(item.Percentile);
                    switch (item.Stat)
                    {
                        case "K/D Ratio":
                            image.FillColor(color)
                                .Text(110, 220, item.Value);
                            break;
                        case "Wins":
                            image.FillColor(color)
                                .Text(300, 220, item.Value);
                            break;
                        case "Win %":
                            image.FillColor(color)
                                .Text(300, 320, $"{ item.Value }%");
                            break;
                        case "Rounds Played":
                            image.FillColor(color)
                                .Text(110, 320, item.Value);
                            break;
                        case "Top 10 Rate":
                            image.FillColor(color)
                                .Text(490, 220, $"{ item.Value }%");
                            break;
                        case "Win Top 10 Ratio":
                            image.FillColor(color)
                                .Text(490, 320, $"{ item.Value }%");
                            break;
                        case "Rating":
                            image.FillColor(color)
                                .Text(490, 120, item.Value);
                            break;
                        default:
                            break;
                    }
                }

                image.Draw(i);
                i.Write("../../Files/Pictures/stats.png");
            }

            await ctx.RespondWithFileAsync("../../Files/Pictures/stats.png", $"https://pubgtracker.com/profile/pc/{ name }/");
        }

        [Command("respect")]
        [Description("Pay respect to someone")]
        [Aliases("f", "payrespect")]
        public async Task Respect(CommandContext ctx, [Description("Url to the image")] string url = "")
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            if (ctx.Message.Attachments.Count > 0)
            {
                url = ctx.Message.Attachments.FirstOrDefault().Url;
            }
            else if (url.Contains('@'))
            {
                var userId = new string(url.Where(char.IsDigit).ToArray());
                var userIdULong = ulong.Parse(userId);
                var user = await ctx.Guild.GetMemberAsync(userIdULong);
                url = user.AvatarUrl;
            }
            else if (url.Equals(""))
            {
                await ctx.RespondAsync("Please attach an image or post a link to the image.");
                return;
            }

            var httpclient = new HttpClient();
            var imageStream = await httpclient.GetStreamAsync(url);
            var img = new MagickImage(imageStream);
            using (MagickImage i = new MagickImage("../../Files/Pictures/f-original.jpg"))
            {
                img.Resize(new MagickGeometry("134x197!"));
                img.VirtualPixelMethod = VirtualPixelMethod.Transparent;
                img.Distort(DistortMethod.Perspective, new double[] { 0, 0, 0, 0, 0, 197, 19, 197, 134, 0, 118, 0, 134, 197, 132, 186 });

                new Drawables().Composite(977, 353, CompositeOperator.Atop, img)
                    .Draw(i);

                i.Write("../../Files/Pictures/f.jpg");
                img.Dispose();
            }

            await ctx.RespondWithFileAsync("../../Files/Pictures/f.jpg", "F");
        }

        [Command("image")]
        [Description("Searches google for images")]
        public async Task Image(CommandContext ctx, [RemainingText][Description("Term you want to search for")] string term)
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            var interactivity = ctx.Client.GetInteractivityModule();
            term = term.Replace(' ', '+');

            using (HttpClient client = new HttpClient())
            {
                var value = await client.GetStringAsync($"https://www.googleapis.com/customsearch/v1?key={ cfg.ApiKeyGoogleSearch }&cx={ cfg.CxGoogleSearch }&q={ term }&num=10&searchType=image");
                var token = JToken.Parse(value);
                var items = token.SelectToken("items");
                var position = 0;
                var param = true;

                var embed = GetPictureEmbed(items, position);

                var msg = await ctx.RespondAsync("", embed: embed);

                var arrowForward = DiscordEmoji.FromName(ctx.Client, ":arrow_forward:");
                var arrowBackward = DiscordEmoji.FromName(ctx.Client, ":arrow_backward:");
                var pauseButton = DiscordEmoji.FromName(ctx.Client, ":pause_button:");
                var stopButton = DiscordEmoji.FromName(ctx.Client, ":stop_button:");
                var emojiList = new List<DiscordEmoji>() { arrowBackward, arrowForward, pauseButton, stopButton };

                foreach (var emoji in emojiList)
                {
                    await msg.CreateReactionAsync(emoji);
                }

                while (param)
                {
                    var reaction = await interactivity.WaitForReactionAsync(xe => emojiList.Contains(xe), ctx.User, TimeSpan.FromSeconds(10));

                    if (reaction != null)
                    {
                        switch (reaction.Emoji.GetDiscordName())
                        {
                            // :arrow_backward: emoji
                            case ":arrow_backward:":
                                if (position == 0)
                                    position = items.Count() - 1;
                                else
                                    position--;

                                await ShowNewImage(embed, msg, emojiList, items, position);
                                break;
                            // :arrow_forward: emoji
                            case ":arrow_forward:":
                                if (position == items.Count() - 1)
                                    position = 0;
                                else
                                    position++;

                                await ShowNewImage(embed, msg, emojiList, items, position);
                                break;
                            // :pause_button: emoji
                            case ":pause_button:":
                                await msg.DeleteAllReactionsAsync();
                                param = false;
                                break;
                            // :stop_buttom: emoji
                            case ":stop_buttom:":
                                await msg.DeleteAsync();
                                param = false;
                                break;
                            default:
                                await ctx.RespondAsync("It broke");
                                break;
                        }
                    }
                    else
                    {
                        await msg.DeleteAllReactionsAsync();
                        param = false;
                    }
                }
            }
        }

        [Command("userinfo")]
        [Description("Gives info about a user in the guild")]
        [Aliases("uinfo")]
        public async Task UserInfo(CommandContext ctx, [RemainingText][Description("The user name, mentioned or just written down normally")] string name)
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            var user = await GetDiscordMemberFromStringAsync(ctx, name);

            if (user == null)
            {
                await ctx.RespondAsync("User doesnt exist");
                return;
            }

            var embed = new DiscordEmbedBuilder().WithColor(DiscordColor.SpringGreen)
                .WithAuthor(ctx.Member.DisplayName, ctx.Member.AvatarUrl, ctx.Member.AvatarUrl)
                .WithTitle("Information about the user")
                .WithThumbnailUrl(user.AvatarUrl)
                .AddField("Username", $"{ user.Username }#{ user.Discriminator }", true)
                .AddField("User Id", user.Id.ToString(), true)
                .AddField("Nickname", $"{ user.Nickname ?? "None" }", true)
                .AddField("Date Joined", $"{ user.JoinedAt.ToString("dd/MM/yyyy HH:mm:ss") }", true)
                .AddField("Role No.", $"{ user.Roles.Count() }", true)
                .AddField("Role Names", $"{ (user.Roles.Count() == 0 ? "None" : user.Roles.Select(xr => xr.Name).ToList().Humanize(xs => $"`{ xs }`")) }", false)
                .WithFooter($"{ ctx.Member.Username }#{ ctx.Member.Discriminator }", ctx.Member.AvatarUrl)
                .WithTimestamp(DateTime.Now);

            await ctx.RespondAsync(embed: embed);
        }

        [Command("serverinfo")]
        [Description("Gives info about the guild")]
        [Aliases("guildinfo", "sinfo", "ginfo")]
        public async Task ServerInfo(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            var embed = new DiscordEmbedBuilder().WithColor(DiscordColor.HotPink)
                .WithAuthor(ctx.Guild.Name, ctx.Guild.IconUrl, ctx.Guild.IconUrl)
                .WithTitle("Information about the guild")
                .WithThumbnailUrl(ctx.Guild.IconUrl)
                .AddField("Guild Name", ctx.Guild.Name, true)
                .AddField("Guild Id", ctx.Guild.Id.ToString(), true)
                .AddField("Guild Owner", $"{ ctx.Guild.Owner.Username }#{ ctx.Guild.Owner.Discriminator }", true)
                .AddField("Date Created", ctx.Guild.CreationTimestamp.ToString("dd/MM/yyyy HH:mm:ss"), true)
                .AddField("Large Guild?", ctx.Guild.IsLarge ? "Yes" : "No", true)
                .AddField("Guild Region", ctx.Guild.RegionId, true)
                .AddField("Guild Emojis No.", ctx.Guild.Emojis.Count.ToString(), true)
                .AddField("Member No.", ctx.Guild.MemberCount.ToString(), true)
                .AddField("Role No.", ctx.Guild.Roles.Count.ToString(), true)
                .AddField("Role Names", ctx.Guild.Roles.Humanize(xr => $"`{ xr.Name }`"), true)
                .WithFooter($"{ ctx.Member.Username }#{ ctx.Member.Discriminator }", ctx.Member.AvatarUrl)
                .WithTimestamp(DateTime.Now);

            await ctx.RespondAsync(embed: embed);
        }

        //[Command("reminder")]
        //[Description("Sets a reminder for you")]
        //[Aliases("remind", "remindme")]
        //public async Task Reminder(CommandContext ctx, [Description("Time, from now, at which you should get the reminder")] TimeSpan time, [RemainingText][Description("Message of the reminder")] string message = "Reminder for something important you asked to be reminded of.")
        //{
        //    var date = DateTime.Now.Add(time);
        //    var user = ctx.Member;
        //    await ctx.RespondAsync($"Reminder set for { date.Humanize(false, DateTime.Now) } with the message saying: ```{ message }```");
        //    var t = new Timer(async e => await user.SendMessageAsync(message), null, time, TimeSpan.FromMilliseconds(-1));
        //}

        [Command("color")]
        public async Task Color(CommandContext ctx, byte r, byte g, byte b)
        {
            await ctx.TriggerTypingAsync();

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            var hex = GetHexValueFromRgb(r, g, b);
            var HSL = GetHSLValueFromRgb(r, g, b);
            var CMYK = GetCMYKValueFromRgb(r, g, b);
            // More color values
            var embed = new DiscordEmbedBuilder().WithColor(new DiscordColor(r, g, b))
                .WithAuthor(ctx.Member.Username, ctx.Member.AvatarUrl, ctx.Member.AvatarUrl)
                .WithTitle("Some information about the color")
                .WithDescription("The actual color you requested is on the left of the embed")
                .AddField("Hex Value", hex, true)
                .AddField("HSL Value", HSL, true)
                .AddField("CMYK Value", CMYK, true)
                .WithFooter($"{ ctx.Member.Username }#{ ctx.Member.Discriminator }", ctx.Member.AvatarUrl)
                .WithTimestamp(DateTime.Now); ;

            await ctx.RespondAsync(embed: embed);
        }

        [Command("showroles")]
        [Description("Shows the roles that are present on the server, for easier usage of `assignrole` command")]
        [Aliases("roles")]
        public async Task ShowRoles(CommandContext ctx)
        {
            var roles = ctx.Guild.Roles;

            var embed = new DiscordEmbedBuilder().WithColor(DiscordColor.SpringGreen)
                .WithAuthor(ctx.User.Username, ctx.User.AvatarUrl, ctx.User.AvatarUrl)
                .WithTitle("Roles")
                .WithDescription("Showing all the roles on the server")
                .AddField("Roles available", roles.Humanize(xr => $"`{ xr.Name }`"))
                .WithFooter($"{ ctx.Member.Username }#{ ctx.Member.Discriminator }", ctx.Member.AvatarUrl)
                .WithTimestamp(DateTime.Now);

            await ctx.RespondAsync(embed: embed);
        }

        [Command("assignrole")]
        [Description("Grants a role to a user who calls the command, with a supplied name of the role")]
        [Aliases("giverole", "grantrole")]
        public async Task AssignRole(CommandContext ctx, [Description("The name of the role")] string name)
        {
            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            var role = ctx.Guild.Roles.FirstOrDefault(xr => xr.Name.ToLower() == name.ToLower());

            if (role == null)
            {
                await ctx.RespondAsync("Role doesnt exist, maybe try using `showroles` command first to see which roles exist on the server");
                return;
            }

            if (ctx.Member.Roles.Contains(role))
            {
                await ctx.RespondAsync("You already have that role!");
                return;
            }

            var IsAllowedToAssign = role.CheckPermission(assignRolePermissions);

            if (IsAllowedToAssign.Humanize() == "Allowed")
            {
                await ctx.RespondAsync("You cant assign that role to yourself.");
                return;
            }

            await ctx.Member.GrantRoleAsync(role);
        }

        [Command("prune")]
        [Description("Prunes your last few messages")]
        public async Task Prune(CommandContext ctx, [Description("Number of messages to prune, max amount of 50")] int numberOfMessages = 10)
        {
            await ctx.TriggerTypingAsync();

            if (numberOfMessages >= 50)
            {
                await ctx.RespondAsync("Maximum number of messages to prune is 50.");
                return;
            }

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            var messages = await ctx.Channel.GetMessagesAsync(limit: 100);
            var messagesToDelete = messages.Where(xm => xm.Author == ctx.User).ToList();

            if (messagesToDelete.Count() >= numberOfMessages)
            {
                await PruneMessagesAsync(ctx, messagesToDelete, numberOfMessages);
            }
            else
            {
                do
                {
                    messages = await ctx.Channel.GetMessagesAsync(after: messages[99].Id);
                    messagesToDelete.Concat(messages.Where(xm => xm.Author == ctx.User).ToList());
                } while (messagesToDelete.Count >= numberOfMessages);

                await PruneMessagesAsync(ctx, messagesToDelete, numberOfMessages);
            }
        }

        [Command("prunebot")]
        [Description("Prunes bot's last few messages")]
        public async Task PruneBot(CommandContext ctx, [Description("Number of messages to prune, max amount of 50")] int numberOfMessages = 10)
        {
            await ctx.TriggerTypingAsync();

            if (numberOfMessages >= 50)
            {
                await ctx.RespondAsync("Maximum number of messages to prune is 50.");
                return;
            }

            if (IsActionOnCoolDown())
            {
                await ctx.RespondAsync($"Action on cooldown, try again in { cooldownTimerLeft }");
                return;
            }

            var messages = await ctx.Channel.GetMessagesAsync(limit: 100);
            var messagesToDelete = messages.Where(xm => xm.Author.Id == 258902871720984577).ToList();

            if (messagesToDelete.Count() >= numberOfMessages)
            {
                await PruneMessagesAsync(ctx, messagesToDelete, numberOfMessages);
            }
            else
            {
                do
                {
                    messages = await ctx.Channel.GetMessagesAsync(after: messages[99].Id);
                    messagesToDelete.Concat(messages.Where(xm => xm.Author.Id == 258902871720984577).ToList());
                } while (messagesToDelete.Count >= numberOfMessages);

                await PruneMessagesAsync(ctx, messagesToDelete, numberOfMessages);
            }
        }

        [Command("mute")]
        [Description("Mutes an user")]
        [RequireOwner]
        public async Task Mute(CommandContext ctx, [Description("The user you want to mute, mentioned")] string name, [Description("Duration of mute")] TimeSpan duration)
        {
            if (ctx.Guild.Roles.FirstOrDefault(xr => xr.Name == "Muted") == null)
            {
                await ctx.RespondAsync("Please run `!setmute` command first to make this command work effectively..");
                return;
            }

            var user = await GetDiscordMemberFromStringAsync(ctx, name);

            if (user.Id == 258902871720984577)
            {
                await ctx.RespondAsync("You cant do that!");
                return;
            }
            
            var mutedRole = ctx.Guild.Roles.FirstOrDefault(xr => xr.Name == "Muted");

            if (user.Roles.Contains(mutedRole))
            {
                await ctx.RespondAsync("User is already muted!");
                return;
            }
            
            await user.GrantRoleAsync(mutedRole);
            await ctx.RespondAsync($"Muting { user.Mention } for { duration.Humanize(3, minUnit: TimeUnit.Second, maxUnit: TimeUnit.Hour) }");

            var t = new Timer(async e => await user.RevokeRoleAsync(mutedRole), null, duration, TimeSpan.FromMilliseconds(-1));
        }

        [Command("setmute")]
        [RequireOwner]
        [Hidden]
        public async Task SetMute(CommandContext ctx)
        {
            var guildRoles = ctx.Guild.Roles;
            var bot = await ctx.Guild.GetMemberAsync(258902871720984577);
            var mutedRole = guildRoles.FirstOrDefault(xr => xr.Name.Equals("Muted"));

            if (mutedRole == null)
            {
                await ctx.RespondAsync("Role doesnt exist, creating necessary role..");
                mutedRole = await ctx.Guild.CreateRoleAsync("Muted", mutedRolePermissions, DiscordColor.Red, true, true);
            }

            var guildChannels = ctx.Guild.Channels.Where(xc => xc.Type == ChannelType.Text).ToList();

            foreach (var channel in guildChannels)
            {
                if (channel.PermissionOverwrites.Where(xr => xr.Deny.ToString().Contains("ManageRoles")).Count() == 0)
                    await channel.AddOverwriteAsync(mutedRole, Permissions.None, Permissions.SendMessages);
            }

            await ctx.RespondAsync("All done! To double check, please see if permission `Muted` exists for text channels and if role `Muted` exists as well!");
        }

        [Command("pin")]
        [Description("Pins a message by its ID")]
        [RequirePermissions(Permissions.ManageChannels)]
        public async Task Pin(CommandContext ctx, [Description("ID of the message you want to pin")] ulong id)
        {
            var msg = await ctx.Channel.GetMessageAsync(id);

            await msg.PinAsync();
        }

        [Command("topic")]
        [Description("Sets the topic for the channel")]
        [RequirePermissions(Permissions.ManageChannels)]
        public async Task Topic(CommandContext ctx, [RemainingText][Description("Topic you want to be shown for the channel")] string topic)
        {
            await ctx.Channel.ModifyAsync(topic: topic ?? "");
        }

        [Command("test")]
        [RequireOwner]
        [Hidden]
        public async Task Test(CommandContext ctx)
        {

        }

        [Command("game")]
        [RequireOwner]
        [Hidden]
        [Aliases("playing", "status")]
        public async Task Game(CommandContext ctx, [RemainingText] string status)
        {
            var game = new DiscordGame(status);
            await ctx.Client.UpdateStatusAsync(game);
        }

        [Command("name")]
        [RequireOwner]
        [Hidden]
        public async Task Name(CommandContext ctx, string name)
        {
            await ctx.Client.EditCurrentUserAsync(name);
        }

        [Command("avatar")]
        [RequireOwner]
        [Hidden]
        public async Task Avatar(CommandContext ctx, string avatarLink)
        {
            var client = new HttpClient();
            var stream = await client.GetStreamAsync(avatarLink);

            await ctx.Client.EditCurrentUserAsync(null, stream);
        }

        private void AppendMemberToString(DiscordMember m, StringBuilder sb)
        {
            sb.Append($"Name: { m.Username }#{ m.Discriminator }, Nickname: { m.Nickname ?? "NO NICKNAME" }, Date joined: { m.JoinedAt.Date.Humanize() } ({ m.JoinedAt.Date.ToString("dd/MM/yyyy") })\n");
        }

        private bool IsActionOnCoolDown()
        {
            var secondsPassed = DateTime.Now.Subtract(cooldown).Seconds;

            if (secondsPassed < 10)
            {
                cooldownTimerLeft = (10 - secondsPassed).Seconds().Humanize(minUnit: TimeUnit.Second);
                return true;
            }

            cooldown = DateTime.Now;

            return false;
        }

        private List<int> GetRandomDistinctNumbers(int minNumber, int maxNumber, int numberOfNumbers)
        {
            var numbers = new List<int>();
            var number = 0;

            while (numbers.Capacity < numberOfNumbers)
            {
                number = rnd.Next(minNumber, maxNumber);
                if (!numbers.Contains(number))
                    numbers.Add(number);
            }

            numbers.Sort();

            return numbers;
        }

        private static IConfiguration AngleSharpConfigurationWithUserAgent()
        {
            var req = new AngleSharp.Network.Default.HttpRequester();
            req.Headers["Accept-Language"] = "en-gb";
            req.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:43.0) Gecko/20100101 Firefox/43.0";
            return Configuration.Default.WithDefaultLoader(requesters: new[] { req });
        }

        private MagickColor GetColorBasedOnPercentile(double? percentile)
        {
            if (percentile.HasValue)
            {
                if (percentile.Value > 0.0 && percentile.Value <= 10.0)
                {
                    return MagickColors.LightGreen;
                }
                else if (percentile.Value > 10.0 && percentile.Value <= 25.0)
                {
                    return MagickColors.GreenYellow;
                }
                else if (percentile.Value > 25.0 && percentile.Value <= 50.0)
                {
                    return MagickColors.Yellow;
                }
                else if (percentile.Value > 50.0 && percentile.Value <= 75.0)
                {
                    return MagickColors.OrangeRed;
                }
                else if (percentile.Value > 75.0 && percentile.Value <= 100.0)
                {
                    return MagickColors.Red;
                }
            }

            return MagickColors.White;
        }

        private DiscordEmbed GetPictureEmbed(JToken items, int position)
        {
            var url = items[position].SelectToken("link").ToString();
            return new DiscordEmbedBuilder().WithColor(DiscordColor.Cyan)
                .WithTitle($"Picture { position + 1 }/{ items.Count() }")
                .WithFooter("Image Search Result")
                .WithImageUrl(url);
        }

        private async Task ShowNewImage(DiscordEmbed embed, DiscordMessage msg, List<DiscordEmoji> emojiList, JToken items, int position)
        {
            embed = GetPictureEmbed(items, position);

            await msg.DeleteAllReactionsAsync();
            msg = await msg.ModifyAsync("", embed: embed);

            foreach (var emoji in emojiList)
            {
                await msg.CreateReactionAsync(emoji);
            }
        }

        private async Task<DiscordMember> GetDiscordMemberFromStringAsync(CommandContext ctx, string name)
        {
            DiscordMember user;

            if (name.Contains('@'))
            {
                var userId = new string(name.Where(char.IsDigit).ToArray());
                var userIdLong = ulong.Parse(userId);
                user = await ctx.Guild.GetMemberAsync(userIdLong);
            }
            else
            {
                var userList = await ctx.Guild.GetAllMembersAsync();
                user = userList.FirstOrDefault(xm => (xm.Username != null ? xm.Username.ToLower() == name.ToLower() : false) || (xm.Nickname != null ? xm.Nickname.ToLower() == name.ToLower() : false));
            }

            return user;
        }

        private string GetHexValueFromRgb(byte r, byte g, byte b)
        {
            var rHex = r.ToString("X2");
            var gHex = g.ToString("X2");
            var bHex = b.ToString("X2");

            return $"#{ rHex }{ gHex }{ bHex }";
        }

        private string GetHSLValueFromRgb(byte r, byte g, byte b)
        {
            var color = MagickColor.FromRgb(r, g, b);
            var HSLColor = ColorHSL.FromMagickColor(color);

            var hue = Math.Round(HSLColor.Hue * 360);
            var saturation = Math.Round(HSLColor.Saturation * 100);
            var luminace = Math.Round(HSLColor.Lightness * 100);

            return $"H: { hue }°, S: { saturation }%, L: { luminace }%";
        }

        private string GetCMYKValueFromRgb(byte r, byte g, byte b)
        {
            var color = MagickColor.FromRgb(r, g, b);
            var CMYKColor = ColorCMYK.FromMagickColor(color);

            var c = CMYKColor.C;
            var m = CMYKColor.M;
            var y = CMYKColor.Y;

            return $"C: { c }, M: { m }, Y: { y }";
        }

        private async Task PruneMessagesAsync(CommandContext ctx, List<DiscordMessage> messagesToDelete, int numberOfMessages)
        {
            for (int i = 0; i <= numberOfMessages; i++)
            {
                await ctx.Channel.DeleteMessageAsync(messagesToDelete[i]);
                await Task.Delay(250);
            }
        }
    }
}