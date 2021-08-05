using System;
using System.Threading;
using System.Threading.Tasks;

using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.SlashCommands;
using DisCatSharp.SlashCommands.EventArgs;

using Microsoft.Extensions.Logging;

namespace DisCatSharp.Examples.Basics.Main
{
    /// <summary>
    /// The bot.
    /// </summary>
    internal class Bot : IDisposable
    {

#if DEBUG 
        public static string prefix = "r!";
#else 
        public static string prefix = "remz!";
#endif
        public static ulong devguild = 870421632336945203;

        public static CancellationTokenSource ShutdownRequest;
        public static DiscordClient Client;
        public static SlashCommandsExtension Slash;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
        private InteractivityExtension INext;
        private CommandsNextExtension CNext;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bot"/> class.
        /// </summary>
        /// <param name="Token">The token.</param>
        public Bot(string Token)
        {
            ShutdownRequest = new CancellationTokenSource();

            LogLevel logLevel;
#if DEBUG
            logLevel = LogLevel.Debug;
#else 
            logLevel = LogLevel.Error;
#endif
            var cfg = new DiscordConfiguration
            {
                Token = Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = logLevel,
                Intents = DiscordIntents.AllUnprivileged,
                MessageCacheSize = 2048
            };

            Client = new DiscordClient(cfg);

            Client.UseSlashCommands();

            CNext = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { prefix },
                CaseSensitive = true,
                EnableMentionPrefix = true,
                IgnoreExtraArguments = true,
                DefaultHelpChecks = null,
                EnableDefaultHelp = true,
                EnableDms = true
            });

            Slash = Client.GetSlashCommands();

            INext = Client.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.WrapAround,
                PaginationDeletion = PaginationDeletion.DeleteMessage,
                PollBehaviour = PollBehaviour.DeleteEmojis,
                ButtonBehavior = ButtonPaginationBehavior.Disable
            });
            RegisterEventListener(Client, Slash, CNext);
            RegisterCommands(CNext, Slash);
        }

        /// <summary>
        /// Disposes the Bot.
        /// </summary>
        public void Dispose()
        {
            Client.Dispose();
            INext = null;
            CNext = null;
            Client = null;
            Slash = null;
            Environment.Exit(0);
        }

        /// <summary>
        /// Starts the Bot.
        /// </summary>
        public async Task RunAsync() {
            await Client.ConnectAsync();
            while (!ShutdownRequest.IsCancellationRequested)
            {
                await Task.Delay(2000);
            }
            await Client.UpdateStatusAsync(activity: null, userStatus: UserStatus.Offline, idleSince: null);
            await Client.DisconnectAsync();
            await Task.Delay(2500);
            Dispose();
        }

        /// <summary>
        /// Registers the event listener.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="cnext">The commandsnext extension.</param>
        private void RegisterEventListener(DiscordClient client, SlashCommandsExtension slash, CommandsNextExtension cnext) {

            /* Client Basic Events */
            client.SocketOpened += Client_SocketOpened;
            client.SocketClosed += Client_SocketClosed;
            client.SocketErrored += Client_SocketErrored;
            client.Heartbeated += Client_Heartbeated;
            client.Ready += Client_Ready;
            client.Resumed += Client_Resumed;

            /* Client Events */
            //client.GuildUnavailable += Client_GuildUnavailable;
            //client.GuildAvailable += Client_GuildAvailable;

            /* CommandsNext Error */
            cnext.CommandErrored += CNext_CommandErrored;

            /* Slash Infos */
            client.ApplicationCommandCreated += Discord_ApplicationCommandCreated;
            client.ApplicationCommandDeleted += Discord_ApplicationCommandDeleted;
            client.ApplicationCommandUpdated += Discord_ApplicationCommandUpdated;
            slash.SlashCommandErrored += Slash_SlashCommandErrored;
            slash.SlashCommandExecuted += Slash_SlashCommandExecuted;
        }

        /// <summary>
        /// Registers the commands.
        /// </summary>
        /// <param name="cnext">The commandsnext extension.</param>
        /// <param name="slash">The slashcommands extension.</param>
        private void RegisterCommands(CommandsNextExtension cnext, SlashCommandsExtension slash)
        {
            cnext.RegisterCommands<Commands.Main>(); // Commands.Main = Ordner.Class
            slash.RegisterCommands<SlashCommands.Main>(devguild); // guild id
            //slash.RegisterCommands<SlashCommands.Main>(); // global - SlashCommands.Main = Ordner.Class
        }

        private static Task Client_Ready(DiscordClient dcl, ReadyEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Starting with Prefix {prefix} :3");
            Console.WriteLine($"Starting {Client.CurrentUser.Username}");
            Console.WriteLine("Client ready!");
            Console.WriteLine($"Shard {dcl.ShardId}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Loading Commands...");
            Console.ForegroundColor = ConsoleColor.Magenta;
            var commandlist = dcl.GetCommandsNext().RegisteredCommands;
            foreach (var command in commandlist)
            {
                Console.WriteLine($"Command {command.Value.Name} loaded.");
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Bot ready!");
            return Task.CompletedTask;
        }

        private static Task Client_Resumed(DiscordClient dcl, ReadyEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Bot resumed!");
            return Task.CompletedTask;
        }

        private static Task Discord_ApplicationCommandUpdated(DiscordClient sender, ApplicationCommandEventArgs e)
        {
            sender.Logger.LogInformation($"Shard {sender.ShardId} sent application command updated: {e.Command.Name}: {e.Command.Id} for {e.Command.ApplicationId}");
            return Task.CompletedTask;
        }
        private static Task Discord_ApplicationCommandDeleted(DiscordClient sender, ApplicationCommandEventArgs e)
        {
            sender.Logger.LogInformation($"Shard {sender.ShardId} sent application command deleted: {e.Command.Name}: {e.Command.Id} for {e.Command.ApplicationId}");
            return Task.CompletedTask;
        }
        private static Task Discord_ApplicationCommandCreated(DiscordClient sender, ApplicationCommandEventArgs e)
        {
            sender.Logger.LogInformation($"Shard {sender.ShardId} sent application command created: {e.Command.Name}: {e.Command.Id} for {e.Command.ApplicationId}");
            return Task.CompletedTask;
        }
        private static Task Slash_SlashCommandExecuted(SlashCommandsExtension sender, SlashCommandExecutedEventArgs e)
        {
            Console.WriteLine($"Slash/Info: {e.Context.CommandName}");
            return Task.CompletedTask;
        }

        private static Task Slash_SlashCommandErrored(SlashCommandsExtension sender, SlashCommandErrorEventArgs e)
        {
            Console.WriteLine($"Slash/Error: {e.Exception.Message} | CN: {e.Context.CommandName} | IID: {e.Context.InteractionId}");
            return Task.CompletedTask;
        }

        private static Task CNext_CommandErrored(CommandsNextExtension ex, CommandErrorEventArgs e)
        {
            if (e.Command == null)
            {
                Console.WriteLine($"{e.Exception.Message}");
            }
            else
            {
                Console.WriteLine($"{e.Command.Name}: {e.Exception.Message}");
            }
            return Task.CompletedTask;
        }

        private static Task Client_SocketOpened(DiscordClient dcl, SocketEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Socket opened");
            return Task.CompletedTask;
        }

        private static Task Client_SocketErrored(DiscordClient dcl, SocketErrorEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Socket has an error! " + e.Exception.Message.ToString());
            return Task.CompletedTask;
        }

        private static Task Client_SocketClosed(DiscordClient dcl, SocketCloseEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Socket closed: " + e.CloseMessage);
            return Task.CompletedTask;
        }

        private static Task Client_Heartbeated(DiscordClient dcl, HeartbeatEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Received Heartbeat:" + e.Ping);
            Console.ForegroundColor = ConsoleColor.Gray;
            return Task.CompletedTask;
        }
    }
}