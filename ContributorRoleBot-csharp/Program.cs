using System;
using System.Linq;
using System.Threading.Tasks;

namespace ContributorRoleBot
{
    class Program
    {
        static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            ContribHelper.InitOrRefresh();
            await DiscordHelpers.Login();

            await Task.Delay(30000);

            while (true)
            {
                ContribHelper.ReadContributorFile();
                await Update();
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        public static async Task Update(bool force = false)
        {
            ContribHelper.InitOrRefresh(force);
            foreach (var keyValuePair in ContribHelper.ContributorActivity)
            {
                Console.WriteLine($"{keyValuePair.Key} is {(keyValuePair.Value ? "" : "in")}active.");
            }

            var guild = DiscordHelpers.Client.GetGuild(DiscordHelpers.SERVER_ID);

            if (guild == null)
            {
                Console.WriteLine("Turns out we are not in the discord server (or not logged in?)");
                return;
            }

            var inactiveRole = guild.GetRole(DiscordHelpers.INACTIVE_CONTRIB_ROLE_ID);
            var activeRole = guild.GetRole(DiscordHelpers.ACTIVE_CONTRIB_ROLE_ID);
            await guild.DownloadUsersAsync();
            foreach (var user in guild.Users)
            {
                var userId = user.Id;
                if (ContribHelper.ContributorDiscords.ContainsValue(userId))
                {
                    var githubName = ContribHelper.ContributorDiscords.First(x => x.Value == userId).Key;
                    var isActive = ContribHelper.ContributorActivity[githubName];

                    if (!user.Roles.Contains(inactiveRole))
                    {
                        Console.Write("A contributor is not marked as inactive contributor, adding role... ");
                        await user.AddRoleAsync(inactiveRole);
                        Console.WriteLine("done.");
                    }

                    if (isActive && !user.Roles.Contains(activeRole))
                    {
                        Console.Write("An active contributor doesn't have active role, adding role... ");
                        await user.AddRoleAsync(activeRole);
                        Console.WriteLine("done.");
                    }
                    else if (!isActive && user.Roles.Contains(activeRole))
                    {
                        Console.Write("An inactive contributor is marked as active, removing role... ");
                        await user.RemoveRoleAsync(activeRole);
                        Console.WriteLine("done.");
                    }
                }
                else
                {
                    if (user.Roles.Contains(activeRole))
                    {
                        Console.Write("A non-contributor is marked as active contributor, removing role... ");
                        await user.RemoveRoleAsync(activeRole);
                        Console.WriteLine("done.");
                    }

                    if (user.Roles.Contains(inactiveRole))
                    {
                        Console.Write("A non-contributor is marked as inactive contributor, removing role... ");
                        await user.RemoveRoleAsync(inactiveRole);
                        Console.WriteLine("done.");
                    }
                }
            }
        }
    }
}
