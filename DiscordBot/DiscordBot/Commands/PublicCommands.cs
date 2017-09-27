using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DiscordBot.Enums;
using System.Net;
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
using System.IO;

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
        private string[] weatherDirections = new string[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW", "N" };

        [Command("uptime")]
        [Description("Gives the time how long has the bot been running for")]
        public async Task Uptime(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            TimeSpan uptime = TimeSpan.FromMilliseconds(DateTime.Now.Subtract(startTime).TotalMilliseconds);
            string humanizedUptime = uptime.Humanize(7, minUnit: TimeUnit.Second, maxUnit: TimeUnit.Year).Humanize(LetterCasing.Title);

            await ctx.RespondAsync(humanizedUptime);
        }

        [Command("ping")]
        [Description("Example ping command")]
        [Aliases("pong")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            DiscordEmoji emoji = DiscordEmoji.FromName(ctx.Client, ":ping_pong:");

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

            string url = "";

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

                string playTime = selectedMain[0].InnerHtml.Trim();
                string winrate = selectedMain[2].InnerHtml.Trim();
                string kda = selectedMain[3].InnerHtml.Trim();
                string elo = selectedCasual[0].InnerHtml.Trim();
                string winrateRanked = "";
                string eloRanked = "";
                string rank = "";

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
                StringBuilder topFiveHeroes = new StringBuilder();

                foreach (var hero in topHeroes)
                {
                    string heroName = hero.QuerySelector("span.name").TextContent.Trim();
                    string heroPosition = hero.QuerySelector("div.god").ChildNodes[4].TextContent.Trim();
                    var otherHeroInfo = hero.QuerySelectorAll("td.text-center div");
                    string heroKda = otherHeroInfo[0].TextContent.Trim();
                    string heroKdaTotal = otherHeroInfo[1].TextContent.Trim();
                    string heroWinrate = otherHeroInfo[2].TextContent.Trim();
                    string heroHours = otherHeroInfo[3].TextContent.Trim();
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

            using (WebClient client = new WebClient())
            {
                string value = client.DownloadString($"http://api.urbandictionary.com/v0/define?term={ term }");
                Urban json = JsonConvert.DeserializeObject<Urban>(value);

                List randomDescription = json.List.ElementAt(rnd.Next(json.List.Count));

                DiscordEmbed embed = new DiscordEmbedBuilder().WithColor(DiscordColor.Lilac)
                    .WithDescription($"{ randomDescription.Word } by { randomDescription.Author }")
                    .WithTimestamp(DateTime.Now)
                    .AddField("Definition", randomDescription.Definition.Truncate(400, "[...]", Truncator.FixedNumberOfCharacters))
                    .AddField("Example", randomDescription.Example.Truncate(400, "[...]", Truncator.FixedNumberOfCharacters))
                    .AddField($"More examples for this word: { randomDescription.Permalink }", $":thumbsup: { randomDescription.ThumbsUp } | :thumbsdown: { randomDescription.ThumbsDown }")
                    .WithAuthor(ctx.Member.Username, ctx.Member.AvatarUrl, ctx.Member.AvatarUrl)
                    .WithFooter($"Requested by { ctx.Member.Username }#{ ctx.Member.Discriminator }", ctx.Member.AvatarUrl);

                await ctx.RespondAsync("", embed: embed);
            }
        }

        [Command("ready")]
        [Description("READY")]
        [Aliases("eyesight", "sight", "shalin")]
        public async Task Ready(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            string url = "https://paladins.gamepedia.com/media/paladins.gamepedia.com/5/59/Sha_Lin_SP_Ability2_2.ogg";

            await ctx.RespondWithFileAsync("../../Files/Pictures/READY.jpg", url);
        }

        [Command("challenge")]
        [Description("CHALLENGE")]
        [Aliases("turtle", "makoa")]
        public async Task Challenge(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            string url = "https://paladins.gamepedia.com/media/paladins.gamepedia.com/2/22/Makoa_Plushy_TR_TakingDamage_2.ogg";

            DiscordMessage message = await ctx.RespondWithFileAsync("../../Files/Pictures/CHALLENGE.jpg", url);

            DiscordEmoji emojiMuscle = DiscordEmoji.FromName(ctx.Client, ":muscle:");
            DiscordEmoji emojiTurtle = ctx.Message.Content.StartsWith("!makoa") ? 
                DiscordEmoji.FromName(ctx.Client, ":makoa:") :
                DiscordEmoji.FromName(ctx.Client, ":turtle:");
            DiscordEmoji emojiAnchor = DiscordEmoji.FromName(ctx.Client, ":anchor:");

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
            var titles = selectedTitles.Select(t => t.TextContent).ToList();
            var scores = selectedScores.Select(s => s.TextContent).ToList();

            var randomNumbers = RandomDistinctNumbers(0, 50, 5);

            StringBuilder sb = new StringBuilder();
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

            term = term.Contains(" ") ? term.Replace(" ", "+") : term;
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
            HttpClient client = new HttpClient();
            var mainWeatherIconStream = await client.GetStreamAsync($"https:{ selectedMainWeatherIcon.GetAttribute("src") }");
            MagickImage mainWeatherIcon = new MagickImage(mainWeatherIconStream);
            List<Weather> weatherList = new List<Weather>();
            int counter = 0;

            for (int i = 1; i < selectedWeek.Length; i++)
            {
                Weather w = new Weather();
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
            searchListResponse.Items.Select(i => i.Id.Kind == "youtube#video").ToList();
            var searchedVideo = searchListResponse.Items.First();
            string videoTitle = searchedVideo.Snippet.Title;
            string videoPublishDate = searchedVideo.Snippet.PublishedAt.Value.ToOrdinalWords();
            string videoDescription = searchedVideo.Snippet.Description.Truncate(500, "[...]", Truncator.FixedNumberOfCharacters);
            string videoUrl = $"https://www.youtube.com/watch?v={ searchedVideo.Id.VideoId }";

            await ctx.RespondAsync($" ** { videoTitle } **\n `{ videoPublishDate } - { videoDescription }`\n { videoUrl }");
        }

        [Command("members")]
        [Description("Shows a list of all members currently in the server, their name, nickname and date joined")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task Members(CommandContext ctx)
        {
            StringBuilder sb = new StringBuilder();
            var allMembers = await ctx.Guild.GetAllMembersAsync();
            allMembers.OrderBy(m => m.Username)
                .ToList()
                .ForEach(m => AppendMemberToString(m, sb));

            HttpClient client = new HttpClient();
            var content = new StringContent(sb.ToString());
            var response = await client.PostAsync("https://hastebin.com/documents", content);
            var jsonToDeserialize = await response.Content.ReadAsStringAsync();
            JToken json = JToken.Parse(jsonToDeserialize);
            string key = json.SelectToken("key").ToString();

            await ctx.Member.SendMessageAsync($"https://hastebin.com/{ key }");
        }

        [Command("roll")]
        [Description("Simulates a dice roll for you.")]
        [Aliases("r", "dice")]
        public async Task Roll(CommandContext ctx, [Description("Format should be: [rolls]d[sides]")] string dice = "1d6")
        {
            await ctx.TriggerTypingAsync();

            if(!dice.Contains('d'))
            {
                await ctx.RespondAsync("Wrong format. Correct format for roll should be `[rolls]d[sides]`");
                return;
            }

            int rolls = int.Parse(dice.Split('d').ElementAt(0));
            int sides = int.Parse(dice.Split('d').ElementAt(1));

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
                int roll = rnd.Next(sides) + 1;

                await ctx.RespondAsync($"You rolled: { roll }");
            } 
            else
            {
                int[] totalRolls = Enumerable.Range(0, rolls)
                    .Select(n => n = (rnd.Next(sides) + 1)).ToArray();
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

            string rating = ctx.Channel.IsNSFW ? "rating:explicit" : "rating:safe";

            if (tags.Equals(""))
            {
                await ctx.RespondAsync("Please put a valid tag to search");
                return;
            }

            using (WebClient client = new WebClient())
            {
                string jsonString = client.DownloadString($"https://danbooru.donmai.us/posts.json?random=true&limit=20&tags={ rating } { tags }");
                JArray json = JArray.Parse(jsonString);

                if (!json.Any())
                {
                    await ctx.RespondAsync("No results, try another tag or try again");
                    return;
                }

                string pictureId = json.FirstOrDefault().SelectToken("id").ToString();

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

            PUBGStatsClient client = new PUBGStatsClient(cfg.ApiKeyPUBG);
            var stats = await client.GetPlayerStatsAsync(name);
            var foundStats = stats.Stats.Find(s => s.Mode == pickedMode && s.Region == pickedRegion && s.Season == Seasons.EASeason4).Stats;

            var avatarUrl = stats.Avatar;
            avatarUrl = $"{ avatarUrl.Substring(0, avatarUrl.LastIndexOf('.')) }_medium.jpg";
            var playerName = stats.PlayerName;
            var kda = foundStats.Find(s => s.Stat == Stats.KDR);
            var wins = foundStats.Find(s => s.Stat == Stats.Wins);
            var winPercentage = foundStats.Find(s => s.Stat == Stats.WinPercentage);
            var roundsPlayed = foundStats.Find(s => s.Stat == Stats.RoundsPlayed);
            var top10Rate = foundStats.Find(s => s.Stat == Stats.Top10Rate);
            var top10Ratio = foundStats.Find(s => s.Stat == Stats.WinTop10Ratio);
            var rating = foundStats.Find(s => s.Stat == Stats.Rating);

            List<StatModel> list = new List<StatModel>()
            {
                kda, wins, winPercentage, roundsPlayed, top10Rate, top10Ratio, rating
            };

            HttpClient httpclient = new HttpClient();
            var avatarIcon = await httpclient.GetStreamAsync(avatarUrl);
            MagickImage avatar = new MagickImage(avatarIcon);
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
                    MagickColor color = GetColorBasedOnPercentile(item.Percentile);
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

            HttpClient httpclient = new HttpClient();
            var imageStream = await httpclient.GetStreamAsync(url);
            MagickImage img = new MagickImage(imageStream);
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
            var interactivity = ctx.Client.GetInteractivityModule();
            term = term.Replace(' ', '+');

            using (WebClient client = new WebClient())
            {
                string value = client.DownloadString($"https://www.googleapis.com/customsearch/v1?key={ cfg.ApiKeyGoogleSearch }&cx={ cfg.CxGoogleSearch }&q={ term }&num=10&searchType=image");
                JToken token = JToken.Parse(value);
                var items = token.SelectToken("items");
                int position = 0;
                bool param = true;

                DiscordEmbed embed = GetPictureEmbed(items, position);

                var msg = await ctx.RespondAsync("", embed: embed);

                var arrowForward = DiscordEmoji.FromName(ctx.Client, ":arrow_forward:");
                var arrowBackward = DiscordEmoji.FromName(ctx.Client, ":arrow_backward:");
                var pauseButton = DiscordEmoji.FromName(ctx.Client, ":pause_button:");
                var stopButton = DiscordEmoji.FromName(ctx.Client, ":stop_button:");

                await msg.CreateReactionAsync(arrowBackward);
                await msg.CreateReactionAsync(arrowForward);
                await msg.CreateReactionAsync(pauseButton);
                await msg.CreateReactionAsync(stopButton);

                while (param)
                {
                    var one = await interactivity.WaitForReactionAsync(s => s.Name == arrowForward.Name
                        || s.Name == arrowBackward.Name
                        || s.Name == pauseButton.Name
                        || s.Name == stopButton.Name, ctx.User, TimeSpan.FromSeconds(10));

                    if (one != null)
                    {
                        switch (one.Emoji.Name)
                        {
                            // :arrow_backward: emoji
                            case "◀":
                                if (position == 0)
                                    position = 9;
                                else
                                    position--;
                                embed = GetPictureEmbed(items, position);
                                await msg.DeleteAllReactionsAsync();
                                msg = await msg.ModifyAsync("", embed: embed);
                                await msg.CreateReactionAsync(arrowBackward);
                                await msg.CreateReactionAsync(arrowForward);
                                await msg.CreateReactionAsync(pauseButton);
                                await msg.CreateReactionAsync(stopButton);
                                break;
                            // :arrow_forward: emoji
                            case "▶":
                                if (position == 9)
                                    position = 0;
                                else
                                    position++;
                                embed = GetPictureEmbed(items, position);
                                await msg.DeleteAllReactionsAsync();
                                msg = await msg.ModifyAsync("", embed: embed);
                                await msg.CreateReactionAsync(arrowBackward);
                                await msg.CreateReactionAsync(arrowForward);
                                await msg.CreateReactionAsync(pauseButton);
                                await msg.CreateReactionAsync(stopButton);
                                break;
                            // :pause_button: emoji
                            case "⏸":
                                await msg.DeleteAllReactionsAsync();
                                param = false;
                                break;
                            // :stop_buttom: emoji
                            case "⏹":
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

        [Command("mute")]
        [Description("Mutes an user")]
        [RequireOwner]
        public async Task Mute(CommandContext ctx, [Description("The user you want to mute, mentioned")] DiscordMember user, [Description("Duration of mute, in minutes")] double duration = 5)
        {
            if (user.Id == 258902871720984577)
            {
                await ctx.RespondAsync("You cant do that!");
                return;
            }
            
            var mutedRole = ctx.Guild.Roles.Where(r => r.Id.Equals(352133974837035018));
            var userRoles = new List<DiscordRole>(user.Roles);
            var timeInMiliseconds = (int) TimeSpan.FromMinutes(duration).TotalMilliseconds;

            if (user.Roles.Contains(mutedRole.First()))
            {
                await ctx.RespondAsync("User is already muted!");
                return;
            }

            await user.ReplaceRolesAsync(mutedRole);

            await ctx.RespondAsync($"Muting { user.Mention } for { duration.Minutes().Humanize() }");

            Timer t = new Timer(async e => await user.ReplaceRolesAsync(userRoles), null, timeInMiliseconds, Timeout.Infinite);
        }

        [Command("setmute")]
        [RequireOwner]
        [Hidden]
        public async Task SetMute(CommandContext ctx)
        {
            var guildRoles = ctx.Guild.Roles;
            var bot = await ctx.Guild.GetMemberAsync(258902871720984577);
            var mutedRole = guildRoles.Where(r => r.Id.Equals(352133974837035018)).FirstOrDefault();
            var guildChannels = ctx.Guild.Channels.Where(c => c.Type == ChannelType.Text).ToList();

            foreach (var channel in guildChannels)
            {
                if (channel.PermissionOverwrites.Where(r => r.Deny.ToString().Contains("ManageRoles")).Count() == 0)
                    await channel.AddOverwriteAsync(mutedRole, Permissions.None, Permissions.SendMessages);
            }

            await ctx.RespondAsync("All done!");
        }

        [Command("pin")]
        [Description("Pins a message by its ID")]
        [RequirePermissions(Permissions.ManageChannels)]
        public async Task Pin(CommandContext ctx, [Description("ID of the message you want to pin")] ulong id)
        {
            DiscordMessage msg = await ctx.Channel.GetMessageAsync(id);

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
        [Hidden]
        [RequireOwner]
        public async Task Test(CommandContext ctx)
        {
            
        }

        [Command("game")]
        [RequireOwner]
        [Hidden]
        [Aliases("playing", "status")]
        public async Task Game(CommandContext ctx, [RemainingText] string game)
        {
            Game g = new Game(game) { StreamType = GameStreamType.NoStream };
            await ctx.Client.UpdateStatusAsync(g);
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
            WebClient client = new WebClient();
            MemoryStream stream = new MemoryStream(client.DownloadData(avatarLink));

            await ctx.Client.EditCurrentUserAsync(null, stream);
        }

        private void AppendMemberToString(DiscordMember m, StringBuilder sb)
        {
            sb.Append($"Name: { m.Username }#{ m.Discriminator }, Nickname: { m.Nickname ?? "NO NICKNAME" }, Date joined: { m.JoinedAt.Date.Humanize() } ({ m.JoinedAt.Date.ToString("dd/MM/yyyy") })\n");
        }

        private string GetWindDirectionFromDegrees(double deg)
        {
            int index = (int) Math.Round(deg / 11.25 / 2);
            return weatherDirections[index];
        }

        private bool IsActionOnCoolDown()
        {
            double secondsPassed = DateTime.Now.Subtract(cooldown).Seconds;

            if (secondsPassed < 10)
            {
                cooldownTimerLeft = (10 - secondsPassed).Seconds().Humanize(minUnit: TimeUnit.Second);
                return true;
            }

            cooldown = DateTime.Now;

            return false;
        }

        private List<int> RandomDistinctNumbers(int minNumber, int maxNumber, int numberOfNumbers)
        {
            List<int> numbers = new List<int>();
            int number = 0;

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
                .WithTitle($"Picture { position + 1 }/10")
                .WithFooter("Image search result")
                .WithImageUrl(url);
        }
    }
}