using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Lavalink4NET;
using Lavalink4NET.DSharpPlus;
using Lavalink4NET.Player;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace All_I_Want_For_Christmas_Bot
{
    class Program
    {
        static Config config;
        static CommandsNextExtension commands;
        static LavalinkNode audioService;

        public static DiscordChannel currentChannel;
        public static DiscordClient discord;
        public static QueuedLavalinkPlayer player;

        public static Timer shotgunResetTimer;
        public static Timer delayTimer;

        public static Stopwatch shotgunStopWatch;

        public static bool dead = false;
        static void Main(string[] args)
        {
            try
            {
                MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static async Task MainAsync(string[] args)
        {
            Console.Title = "Mariah Carey";
            string json = File.ReadAllText("config.json");
            dynamic deserializedJson = JsonConvert.DeserializeObject(json);
            config = new Config();

            foreach (var item in deserializedJson)
            {
                config = new Config(item.commandPrefix.ToString(), item.token.ToString(), item.GuildID.ToString());
            }

            DiscordConfiguration discordConfiguration = new DiscordConfiguration
            {
                Token = config.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            };

            discord = new DiscordClient(discordConfiguration);

            Process LavaLinkProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    FileName = "java",
                    Arguments = $@"-jar LavaLink.jar"
                }
            };

            LavaLinkProcess.Start();

            commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableDms = true,
                IgnoreExtraArguments = true,
                UseDefaultCommandHandler = false
            });

            commands.RegisterCommands<BotCommands>();

            audioService = new LavalinkNode(new LavalinkNodeOptions
            {
                RestUri = "http://localhost:6420/",
                WebSocketUri = "ws://localhost:6420/",
                Password = "crabrave",
                DisconnectOnStop = false
            }, new DiscordClientWrapper(discord));

            discord.VoiceStateUpdated += async e =>
            {
                try
                {
                    await discord.UpdateStatusAsync(new DiscordActivity("Send \"shotgun\" in chat to kill me.", ActivityType.Playing));
                }
                catch
                {

                }

                try
                {
                    if (e.User.IsCurrent || dead)
                    {
                        return;
                    }

                    if (currentChannel == null)
                    {
                        DiscordChannel[] channels = e.Client.Guilds[config.GuildID].Channels.Values.ToArray();

                        List<DiscordChannel> voiceChannels = new List<DiscordChannel>();

                        for (int i = 0; i < channels.Length; i++)
                        {
                            if (channels[i].Type == ChannelType.Voice)
                            {
                                voiceChannels.Add(channels[i]);
                            }
                        }

                        foreach (var channel in voiceChannels)
                        {
                            if (channel.Users.Count() > 0 && channel != currentChannel)
                            {
                                currentChannel = channel;

                                player = audioService.GetPlayer<QueuedLavalinkPlayer>(config.GuildID)
                                    ?? await audioService.JoinAsync<QueuedLavalinkPlayer>(config.GuildID, channel.Id);

                                var ran = new Random();

                                var temp = await audioService.GetTracksAsync(getPlaylist(), Lavalink4NET.Rest.SearchMode.YouTube);
                                LavalinkTrack[] Tracklist = temp.ToArray();

                                Tracklist = Tracklist.OrderBy(w => ran.Next()).ToArray();

                                for (int i = 0; i < Tracklist.Length; i++)
                                {
                                    await player.PlayAsync(Tracklist[i]);
                                }

                                return;
                            }
                        }
                    }
                    else if (!(currentChannel.Users.Count() > 1))
                    {
                        DiscordChannel[] channels = e.Client.Guilds[config.GuildID].Channels.Values.ToArray();

                        List<DiscordChannel> voiceChannels = new List<DiscordChannel>();

                        for (int i = 0; i < channels.Length; i++)
                        {
                            if (channels[i].Type == ChannelType.Voice)
                            {
                                voiceChannels.Add(channels[i]);
                            }
                        }

                        foreach (var channel in voiceChannels)
                        {
                            if (channel.Users.Count() > 0 && channel != currentChannel)
                            {
                                currentChannel = channel;

                                await player.ConnectAsync(channel.Id);

                                return;
                            }
                        }

                        currentChannel = null;

                        await player.DisconnectAsync();
                        player.Dispose();
                    }
                    
                }
                catch (Exception ligma)
                {
                    Console.WriteLine(ligma);
                }
                
            };

            discord.MessageCreated += async e =>
            {
                await HandleCommandAsync(e);
            };

            discord.ClientErrored += async e =>
            {
                Console.WriteLine("Connection Error! attempting reconnection.");
                await discord.ConnectAsync();
            };

            discord.Resumed += async e =>
            {
                Console.WriteLine("Reconnection Successful!");
            };

            discord.Ready += async e =>
            {
                await audioService.InitializeAsync();

                await discord.UpdateStatusAsync(new DiscordActivity("Send \"shotgun\" in chat to kill me.", ActivityType.Playing));

                Console.WriteLine("Finished Startup!");
            };

            discord.GuildDownloadCompleted += async e =>
            {
                await PreyOnPeople(e);
            };

            audioService.TrackEnd += AudioService_TrackEnd;

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }


        public static async Task killbot()
        {
            currentChannel = null;
            dead = true;
            await player.DisconnectAsync();
            player.Dispose();

            Random ran = new Random();
            long time = ran.Next(1800000, 7200000);

            delayTimer = new Timer(time);
            delayTimer.AutoReset = false;
            delayTimer.Elapsed += ILiveAgain;
            delayTimer.Start();
        }

        public static async Task StartGunTimer()
        {
            shotgunResetTimer = new Timer(900000);
            shotgunResetTimer.AutoReset = false;
            shotgunResetTimer.Elapsed += ShotgunReset;
            shotgunResetTimer.Start();

            shotgunStopWatch = new Stopwatch();
            shotgunStopWatch.Start();
        }


        // We need a custom Command Handler so we can ensure that only people with a specific Role can modify the playlist.
        internal static async Task HandleCommandAsync(MessageCreateEventArgs e)
        {
            if (e.Author.IsCurrent)
            {
                return;
            }

            if (e.Message.Content.StartsWith(config.Prefix))
            {
                string message = e.Message.Content.Remove(0, config.Prefix.Length);
                var cmd = commands.FindCommand(message, out var args);
                var ctx = commands.CreateContext(e.Message, config.Prefix, cmd, args);
                await Task.Run(async () => await commands.ExecuteCommandAsync(ctx));
            }
            


        }

        internal static async Task AudioService_TrackEnd(object sender, Lavalink4NET.Events.TrackEndEventArgs eventArgs)
        {
            if (player.Queue.IsEmpty)
            {
                var ran = new Random();

                var temp = await audioService.GetTracksAsync(getPlaylist(), Lavalink4NET.Rest.SearchMode.YouTube);
                LavalinkTrack[] Tracklist = temp.ToArray();

                Tracklist = Tracklist.OrderBy(w => ran.Next()).ToArray();

                for (int i = 0; i < Tracklist.Length; i++)
                {
                    await player.PlayAsync(Tracklist[i]);
                }
            }
        }

        internal static void ILiveAgain(object sender, ElapsedEventArgs e)
        {
            Task.FromResult(ILiveAgainAsync());
        }

        internal static async Task ILiveAgainAsync()
        {
            dead = false;

            BotCommands.step = 0;
            shotgunResetTimer.Stop();
            shotgunStopWatch.Stop();

            DiscordChannel[] channels = discord.Guilds[config.GuildID].Channels.Values.ToArray();

            List<DiscordChannel> voiceChannels = new List<DiscordChannel>();

            for (int i = 0; i < channels.Length; i++)
            {
                if (channels[i].Type == ChannelType.Voice)
                {
                    voiceChannels.Add(channels[i]);
                }
            }

            foreach (var channel in voiceChannels)
            {
                if (channel.Users.Count() > 0)
                {
                    if (channel.Users.Count() == 1)
                    {
                        if (!channel.Users.ToArray()[0].IsCurrent)
                        {
                            currentChannel = channel;

                            player = audioService.GetPlayer<QueuedLavalinkPlayer>(config.GuildID)
                                ?? await audioService.JoinAsync<QueuedLavalinkPlayer>(config.GuildID, channel.Id);

                            var ran = new Random();

                            var temp = await audioService.GetTracksAsync(getPlaylist(), Lavalink4NET.Rest.SearchMode.YouTube);
                            LavalinkTrack[] Tracklist = temp.ToArray();

                            Tracklist = Tracklist.OrderBy(w => ran.Next()).ToArray();

                            for (int i = 0; i < Tracklist.Length; i++)
                            {
                                await player.PlayAsync(Tracklist[i]);
                            }

                            return;
                        }
                    }
                    else
                    {
                        currentChannel = channel;

                        player = audioService.GetPlayer<QueuedLavalinkPlayer>(config.GuildID)
                            ?? await audioService.JoinAsync<QueuedLavalinkPlayer>(config.GuildID, channel.Id);

                        var ran = new Random();

                        var temp = await audioService.GetTracksAsync(getPlaylist(), Lavalink4NET.Rest.SearchMode.YouTube);
                        LavalinkTrack[] Tracklist = temp.ToArray();

                        Tracklist = Tracklist.OrderBy(w => ran.Next()).ToArray();

                        for (int i = 0; i < Tracklist.Length; i++)
                        {
                            await player.PlayAsync(Tracklist[i]);
                        }

                        return;
                    }
                }
            }
        }

        internal static void ShotgunReset(object sender, ElapsedEventArgs e)
        {
            Task.FromResult(ShotgunResetAsync());
        }

        internal static async Task ShotgunResetAsync()
        {
            BotCommands.step = 0;
            shotgunResetTimer.Stop();
            shotgunStopWatch.Stop();
        }

        public static async Task PreyOnPeople(GuildDownloadCompletedEventArgs e)
        {
            try
            {
                DiscordChannel[] channels = e.Client.Guilds[config.GuildID].Channels.Values.ToArray();

                List<DiscordChannel> voiceChannels = new List<DiscordChannel>();

                for (int i = 0; i < channels.Length; i++)
                {
                    if (channels[i].Type == ChannelType.Voice)
                    {
                        voiceChannels.Add(channels[i]);
                    }
                }

                foreach (var channel in voiceChannels)
                {
                    if (channel.Users.Count() > 0)
                    {
                        if (channel.Users.Count() == 1)
                        {
                            if (!channel.Users.ToArray()[0].IsCurrent)
                            {
                                currentChannel = channel;

                                player = audioService.GetPlayer<QueuedLavalinkPlayer>(config.GuildID)
                                    ?? await audioService.JoinAsync<QueuedLavalinkPlayer>(config.GuildID, channel.Id);

                                var ran = new Random();

                                var temp = await audioService.GetTracksAsync(getPlaylist(), Lavalink4NET.Rest.SearchMode.YouTube);
                                LavalinkTrack[] Tracklist = temp.ToArray();

                                Tracklist = Tracklist.OrderBy(w => ran.Next()).ToArray();

                                for (int i = 0; i < Tracklist.Length; i++)
                                {
                                    await player.PlayAsync(Tracklist[i]);
                                }

                                return;
                            }
                        }
                        else
                        {
                            currentChannel = channel;

                            player = audioService.GetPlayer<QueuedLavalinkPlayer>(config.GuildID)
                                ?? await audioService.JoinAsync<QueuedLavalinkPlayer>(config.GuildID, channel.Id);

                            var ran = new Random();

                            var temp = await audioService.GetTracksAsync(getPlaylist(), Lavalink4NET.Rest.SearchMode.YouTube);
                            LavalinkTrack[] Tracklist = temp.ToArray();

                            Tracklist = Tracklist.OrderBy(w => ran.Next()).ToArray();

                            for (int i = 0; i < Tracklist.Length; i++)
                            {
                                await player.PlayAsync(Tracklist[i]);
                            }

                            return;
                        }
                    }
                }
            }
            catch (Exception ligma)
            {
                Console.WriteLine(ligma);
            }

        }

        internal static string getPlaylist()
        {
            string playlist = File.ReadAllText($@"Cache/Playlist.txt");
            return playlist;
        }
    }
}
