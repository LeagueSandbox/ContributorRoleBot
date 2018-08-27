using System.IO;

namespace ContributorRoleBot
{
    public static class Auth
    {
        public static readonly string GITHUB_TOKEN = File.ReadAllText("githubToken.txt");
        public static readonly string DISCORD_TOKEN = File.ReadAllText("discordToken.txt");
    }
}
