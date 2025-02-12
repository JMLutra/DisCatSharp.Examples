using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.EventArgs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DisCatSharp.Examples.ApplicationCommands
{
	/// <summary>
	/// The program.
	/// </summary>
	public class Program
	{
		/// <summary>
		/// Entry point.
		/// </summary>
		/// <param name="args">The args.</param>
		public static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Asynchronous method in which the bot is initialized.
		/// </summary>
		/// <param name="args">The args.</param>
		public static async Task MainAsync(string[] args)
		{
			// Logging! Let the user know that the bot started!
			Console.WriteLine("Starting bot...");

			// Create logger
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Information()
				.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();

			// CHALLENGE: Try making sure the token is provided! Hint: A Try/Catch block may be needed!
			DiscordConfiguration discordConfiguration = new()
			{
				// The token is recieved from the command line arguments (bad practice in production!)
				// Example: dotnet run <someBotTokenHere>
				// CHALLENGE: Make it read from a file, optionally from a json file using System.Text.Json
				// CHALLENGE #2: Try retriving the token from environment variables
				Token = args[0],
				LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger)
			};

			DiscordShardedClient discordShardedClient = new(discordConfiguration);

			Log.Logger.Information("Connecting to Discord...");
			await discordShardedClient.StartAsync();

			// Use the default logger provided for easy reading
			discordShardedClient.Logger.LogInformation($"Connection success! Logged in as {discordShardedClient.CurrentUser.Username}#{discordShardedClient.CurrentUser.Discriminator} ({discordShardedClient.CurrentUser.Id})");

			// Register a Random class instance now for use later over in RollRandom.cs
			ApplicationCommandsConfiguration appCommandsConfiguration = new(new ServiceCollection().AddSingleton<Random>().BuildServiceProvider());

			// Let the user know that we're registering the commands.
			discordShardedClient.Logger.LogInformation("Registering application commands...");

			// If the guild ID is not provided, then register global commands.
			ulong? guildId = null;
			if (args.Length > 1)
			{
				guildId = ulong.Parse(args[1]);
			}

			// In order not to list all the commands when adding, you can create a list of all commands with this.
			Type appCommandModule = typeof(ApplicationCommandsModule);
			var commands = Assembly.GetExecutingAssembly().GetTypes().Where(t => appCommandModule.IsAssignableFrom(t) && !t.IsNested).ToList();

			foreach (DiscordClient discordClient in discordShardedClient.ShardClients.Values)
			{
				ApplicationCommandsExtension appCommandShardExtension = discordClient.UseApplicationCommands(appCommandsConfiguration);

				// Register event handlers
				appCommandShardExtension.SlashCommandExecuted += Slash_SlashCommandExecuted;
				appCommandShardExtension.SlashCommandErrored += Slash_SlashCommandErrored;
				appCommandShardExtension.ContextMenuExecuted += Context_ContextMenuCommandExecuted;
				appCommandShardExtension.ContextMenuErrored += Context_ContextMenuCommandErrored;

				foreach (var command in commands)
				{
					if (guildId != null)
						appCommandShardExtension.RegisterGuildCommands(command, (ulong)guildId);
					else
						appCommandShardExtension.RegisterGlobalCommands(command);
				}
			}

			discordShardedClient.Logger.LogInformation("Application commands registered successfully");
			// Listen for commands by putting this method to sleep and relying off of DiscordClient's event listeners
			await Task.Delay(-1);
		}

		/// <summary>
		/// Fires when the user uses the slash command.
		/// </summary>
		/// <param name="sender">Application commands ext.</param>
		/// <param name="e">Event arguments.</param>
		private static Task Slash_SlashCommandExecuted(ApplicationCommandsExtension sender, SlashCommandExecutedEventArgs e)
		{
			Log.Logger.Information($"Slash: {e.Context.CommandName}");
			return Task.CompletedTask;
		}

		/// <summary>
		/// Fires when an exception is thrown in the slash command.
		/// </summary>
		/// <param name="sender">Application commands ext.</param>
		/// <param name="e">Event arguments.</param>
		private static Task Slash_SlashCommandErrored(ApplicationCommandsExtension sender, SlashCommandErrorEventArgs e)
		{
			Log.Logger.Error($"Slash: {e.Exception.Message} | CN: {e.Context.CommandName} | IID: {e.Context.InteractionId}");
			return Task.CompletedTask;
		}

		/// <summary>
		/// Fires when the user uses the context menu command.
		/// </summary>
		/// <param name="sender">Application commands ext.</param>
		/// <param name="e">Event arguments.</param>
		private static Task Context_ContextMenuCommandExecuted(ApplicationCommandsExtension sender, ContextMenuExecutedEventArgs e)
		{
			Log.Logger.Information($"Context: {e.Context.CommandName}");
			return Task.CompletedTask;
		}

		/// <summary>
		/// Fires when an exception is thrown in the context menu command.
		/// </summary>
		/// <param name="sender">Application commands ext.</param>
		/// <param name="e">Event arguments.</param>
		private static Task Context_ContextMenuCommandErrored(ApplicationCommandsExtension sender, ContextMenuErrorEventArgs e)
		{
			Log.Logger.Error($"Context: {e.Exception.Message} | CN: {e.Context.CommandName} | IID: {e.Context.InteractionId}");
			return Task.CompletedTask;
		}
	}
}
