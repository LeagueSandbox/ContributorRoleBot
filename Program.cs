using System;
using System.Linq;
using System.Threading.Tasks;

namespace ContributorRoleBot
{
    class Program
    {
        static void Main()
        {
            ContribHelper.InitOrRefresh();
            DiscordHelpers.Login().GetAwaiter().GetResult();

            Update().GetAwaiter().GetResult();
        }

        static async Task Update()
        {
            while (true)
            {
                ContribHelper.InitOrRefresh();
                foreach (var keyValuePair in ContribHelper.ContributorActivity)
                {
                    Console.WriteLine($"{keyValuePair.Key} is {(keyValuePair.Value ? "" : "in")}active.");

                    var guild = DiscordHelpers.Client.GetGuild(DiscordHelpers.SERVER_ID);
                    var discordId = ContribHelper.ContributorDiscords[keyValuePair.Key];
                    var user = guild.GetUser(discordId);
                    if (user != null)
                    {
                        var inactiveRole = guild.GetRole(DiscordHelpers.INACTIVE_CONTRIB_ROLE_ID);
                        if (!user.Roles.Contains(inactiveRole))
                        {
                            await user.AddRoleAsync(inactiveRole);
                        }

                        var activeRole = guild.GetRole(DiscordHelpers.ACTIVE_CONTRIB_ROLE_ID);
                        if (keyValuePair.Value && !user.Roles.Contains(activeRole))
                        {
                            await user.AddRoleAsync(activeRole);
                        }
                        else if (!keyValuePair.Value && user.Roles.Contains(activeRole))
                        {
                            await user.RemoveRoleAsync(activeRole);
                        }

                        Console.WriteLine($"Roles have been updated for {user.Username}");
                    }
                }

                await Task.Delay(TimeSpan.FromHours(1));
            }
        }
    }
}
