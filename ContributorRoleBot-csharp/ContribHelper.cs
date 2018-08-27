using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Octokit;

namespace ContributorRoleBot
{
    public static class ContribHelper
    {
        private const string OWNER = "LeagueSandbox";

        public static Dictionary<string, bool> ContributorActivity = new Dictionary<string, bool>();
        public static Dictionary<string, ulong> ContributorDiscords = new Dictionary<string, ulong>();
        public static GitHubClient GitHubClient = new GitHubClient(new ProductHeaderValue("LSContribRoleBot-TEST"));

        private static DateTimeOffset _lastRefresh;

        static ContribHelper()
        {
            GitHubClient.Credentials = new Credentials(Auth.GITHUB_TOKEN);
        }

        public static void InitOrRefresh(bool force = false)
        {
            if (DateTimeOffset.Now - _lastRefresh < TimeSpan.FromHours(1) && !force)
            {
                return;
            }

            _lastRefresh = DateTimeOffset.Now;

            Console.Write("Getting all org repos... ");
            var repos = GitHubClient.Repository.GetAllForOrg("LeagueSandbox").Result;
            Console.WriteLine("done.");
            foreach (var repo in repos)
            {
                var allCommits = GitHubClient.Repository.Commit.GetAll(OWNER, repo.Name).Result;
                var message = $"Iterating through {repo.Name}'s {allCommits.Count} commits... ";
                for (var i = 0; i < allCommits.Count; i++) //foreach (var commitTldr in allCommits)
                {
                    var commitTldr = allCommits[i];
                    Console.Write("\r" + message + (i + 1) + " ");

                    // If our author is already marked as active
                    if (commitTldr.Author != null && ContributorActivity.ContainsKey(commitTldr.Author.Login) &&
                        ContributorActivity[commitTldr.Author.Login])
                    {
                        continue;
                    }

                    // If commit is a merge commit
                    if (commitTldr.Parents.Count > 1)
                    {
                        continue;
                    }

                    // If our author is already marked as inactive and commit is +1 month old
                    var date = commitTldr.Commit.Author.Date;
                    var isOldCommit = DateTimeOffset.Now - date > TimeSpan.FromDays(30);

                    if (commitTldr.Author != null &&
                        ContributorActivity.ContainsKey(commitTldr.Author.Login) &&
                        isOldCommit)
                    {
                        continue;
                    }

                    // A second request is required to get files, as getting all commits doesn't send the change list
                    var commit = GitHubClient.Repository.Commit.Get(OWNER, repo.Name, allCommits[i].Sha).Result;

                    // If commit only edits README.md
                    var files = commit.Files;
                    if (files != null && files.Count == 1 && files[0].Filename.Equals("README.md"))
                    {
                        continue;
                    }

                    if (commit.Author != null)
                    {
                        ContributorActivity[commit.Author.Login] = !isOldCommit;
                    }
                }

                Console.WriteLine("done.");
            }

            Console.WriteLine("All done.");

            ReadContributorFile();
        }

        public static void ReadContributorFile()
        {
            ContributorDiscords = ContributorActivity.ToDictionary(x => x.Key, y => 0ul);
            if (File.Exists("contributors.txt"))
            {
                foreach (var line in File.ReadAllLines("contributors.txt"))
                {
                    var split = line.Split(' ');
                    if (ContributorDiscords.ContainsKey(split[0]))
                    {
                        ContributorDiscords[split[0]] = ulong.Parse(split[1]);
                    }
                }
            }
        }

        public static void FlushContributorFile()
        {
            var lines = new List<string>();

            foreach (var entry in ContributorDiscords)
            {
                lines.Add($"{entry.Key} {entry.Value}");
            }

            File.WriteAllLines("contributors.txt", lines);
        }
    }
}
