using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Octokit;

namespace ContributorRoleBot
{
    public static class ContribHelper
    {
        private const string OWNER = "LeagueSandbox";
        public static Dictionary<string, bool> ContributorActivity = new Dictionary<string, bool>();

        public static GitHubClient GitHubClient = new GitHubClient(new ProductHeaderValue("LSContribRoleBot-TEST"));

        static ContribHelper()
        {
            GitHubClient.Credentials = new Credentials(Auth.GITHUB_TOKEN);
        }

        public static void Init()
        {
            Console.Write("Getting all org repos... ");
            var repos = GitHubClient.Repository.GetAllForOrg("LeagueSandbox").Result;
            Console.WriteLine("done.");
            foreach (var repo in repos)
            {
                var allCommits = GitHubClient.Repository.Commit.GetAll(OWNER, repo.Name).Result;
                var message = $"Iterating through {repo.Name}'s {allCommits.Count} commits... ";
                for (var i = 0; i < allCommits.Count; i++)//foreach (var commitTldr in allCommits)
                {
                    Console.Write("\r" + message + (i + 1) + " ");

                    // If commit is a merge commit
                    if (allCommits[i].Parents.Count > 1)
                    {
                        continue;
                    }

                    var commit = GitHubClient.Repository.Commit.Get(OWNER, repo.Name, allCommits[i].Sha).Result;

                    // If commit only edits README.md
                    var files = commit.Files;
                    if (files != null && files.Count == 1 && files[0].Filename.Equals("README.md"))
                    {
                        continue;
                    }

                    // If commit was made more than a month ago
                    var date = commit.Commit.Author.Date;
                    if (DateTimeOffset.Now - date > TimeSpan.FromDays(30))
                    {
                        if (commit.Author != null && !ContributorActivity.ContainsKey(commit.Author.Login))
                        {
                            ContributorActivity[commit.Author.Login] = false;
                        }
                        continue;
                    }

                    ContributorActivity[commit.Author.Login] = true;
                }
                Console.WriteLine("done.");
            }

            Console.WriteLine("All done.");
        }
    }
}
