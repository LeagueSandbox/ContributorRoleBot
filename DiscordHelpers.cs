using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace ContributorRoleBot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("contributoractivity")]
        [Summary("Prints if contributors are active or not.")]
        [Alias("printactivity", "activity", "contribactivity")]
        public async Task PrintActivityAsync()
        {
            await ReplyAsync("Refreshing contributor activity, this *might* take up to a minute... ");
            ContribHelper.InitOrRefresh();
            var reply = "";
            foreach (var keyValuePair in ContribHelper.ContributorActivity)
            {
                reply += $"{keyValuePair.Key} is {(keyValuePair.Value ? "" : "in")}active.\n";
            }

            await ReplyAsync(reply);
        }
    }

    public static class DiscordHelpers
    {
        public const ulong SERVER_ID = 166860156506865665;
        public const ulong ACTIVE_CONTRIB_ROLE_ID = 167012523814551552;
        public const ulong INACTIVE_CONTRIB_ROLE_ID = 259634105019269130;

        public static DiscordSocketClient Client;
        public static CommandService Commands;
        public static IServiceProvider Services;

        static DiscordHelpers()
        {
            Client = new DiscordSocketClient();
            Commands = new CommandService();
            Services = new ServiceCollection().AddSingleton(Client).AddSingleton(Commands).BuildServiceProvider();

            InstallCommandsAsync().GetAwaiter().GetResult();

            Client.Log += Log;
        }

        public static async Task InstallCommandsAsync()
        {
            Client.MessageReceived += HandleCommandAsync;

            await Commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private static async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null)
            {
                return;
            }

            int argPos = 0;

            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(Client.CurrentUser, ref argPos)))
            {
                return;
            }

            var context = new SocketCommandContext(Client, message);
            var result = await Commands.ExecuteAsync(context, argPos, Services);

            if (!result.IsSuccess)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        public static async Task Login()
        {
            if (Client.ConnectionState == ConnectionState.Connecting ||
                Client.ConnectionState == ConnectionState.Connected)
            {
                await Task.Delay(1000);
                return;
            }

            await Client.LoginAsync(TokenType.Bot, Auth.DISCORD_TOKEN);
            await Client.StartAsync();
        }

        public static async Task Logout()
        {
            if (Client.ConnectionState == ConnectionState.Disconnecting ||
                Client.ConnectionState == ConnectionState.Disconnected)
            {
                await Task.Delay(1000);
                return;
            }

            await Client.LogoutAsync();
            await Client.StopAsync();
        }

        private static Task Log(LogMessage arg)
        {
            Console.WriteLine(arg.ToString());
            return Task.CompletedTask;
        }
    }
}
