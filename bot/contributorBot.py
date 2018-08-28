from github import Github
from datetime import datetime, timedelta
import os
import discord
import asyncio

DEBUG_ONLY = True
INACTIVE_THRESHOLD = 15 # Defines how many days one has to be inactive to be counted inactive.

GITHUB_TOKEN = ""
DISCORD_TOKEN = ""

with open('githubToken.txt', 'r') as content:
    GITHUB_TOKEN = content.read()

with open('discordToken.txt', 'r') as content:
    DISCORD_TOKEN = content.read()

OWNER = "LeagueSandbox"

ContributorActivity = {}
ContributorDiscords = {}
GithubClient = Github(GITHUB_TOKEN)

def InitOrRefreshActivity():
    print("Fetching all org repos... ", end='')
    repos = GithubClient.get_organization(OWNER).get_repos('public')
    print("done.")

    for repo in repos:
        allCommits = repo.get_commits()
        message = "Iterating through {}'s commits... ".format(repo.name)

        i = 0
        for commitTldr in allCommits:
            i += 1
            print("\r{}{} ".format(message, i), end='')

            # If our author is already marked as active
            if commitTldr.author != None and commitTldr.author.login in ContributorActivity.keys() and ContributorActivity[commitTldr.author.login]:
                continue

            # If commit is a merge commit
            if len(commitTldr.parents) > 1:
                continue

            # If our author is already marked as inactive and commit is old
            date = commitTldr.commit.author.date
            isOldCommit = datetime.now() - date > timedelta(days=INACTIVE_THRESHOLD)

            if commitTldr.author != None and commitTldr.author.login in ContributorActivity.keys() and isOldCommit:
                continue

            # A second request is required to get files, as getting all commits doesn't send the change list
            commit = repo.get_commit(commitTldr.sha)

            # If the commit only edits *.md files
            files = commit.files

            if files != None and all(file.filename.endswith(".md") for file in files):
                continue

            # If total lines edited is less than 10
            totalChanges = 0
            for file in files:
                totalChanges += file.changes

            if totalChanges < 10:
                continue

            if commit.author != None:
                ContributorActivity[commit.author.login] = not isOldCommit

        print("done.")

    print("All done.")
    ReadContributorFile()

def ReadContributorFile():
    keys = ContributorActivity.keys()
    if os.path.exists("contributors.txt"):
        lines = []
        with open("contributors.txt", 'r') as f:
            lines = f.readlines()
        lines = [x.strip() for x in lines]
        lines = list(filter(None, lines))
        for line in lines:
            split = line.split()
            if split[0] in keys:
                ContributorDiscords[split[0]] = split[1]

def FlushContributorFile():
    lines = []
    for key, value in ContributorDiscords.items():
        lines.append("{} {}".format(key, value))

    with open("contributors.txt", 'w+') as file:
        for line in lines:
            file.write("{}\n".format(line))

DISCORD_SERVER_ID = "166860156506865665"
DISCORD_ACTIVE_ROLE_ID = "167012523814551552"
DISCORD_INACTIVE_ROLE_ID = "259634105019269130"

Client = discord.Client()

@Client.event
async def on_message(message):
    if message.content.lower().startswith('!printActivity'):
        tmp = ""
        if not DEBUG_ONLY: tmp = await client.send_message(message.channel, "Refreshing contributor activity, this *might* take a while...")
        InitOrRefreshActivity()
        reply = ""
        for key, value in ContributorActivity.items():
            reply += "{} is {}active.\n".format(key, '' if value else 'in')

        if not DEBUG_ONLY: await client.edit_message(tmp, reply)
    elif message.content.lower().startswith('!forceRefresh'):
        await UpdateRoles()
        if not DEBUG_ONLY: await client.add_reaction(message, "✅")

async def Update():
    InitOrRefreshActivity()
    for key, value in ContributorActivity.items():
        log("{} is {}active.\n".format(key, '' if value else 'in'))

    guild = Client.get_server(DISCORD_SERVER_ID)

    if guild == None or guild.unavailable:
        log("It seems like either we are not in the discord server or server is down.\n")
        return

    inactiveRole = None
    activeRole = None
    for role in guild.roles:
        if role.id == DISCORD_INACTIVE_ROLE_ID:
            inactiveRole = role
        elif role.id == DISCORD_ACTIVE_ROLE_ID:
            activeRole = role

    for user in guild.members:
        userId = user.id
        # print("{} = {}".format(user.name, ["{}-{}".format(x.name, x.id) for x in user.roles]))
        if userId in ContributorDiscords.values():
            githubName = ""
            for key, value in ContributorDiscords.items():
                if value == userId:
                    githubName = key
                    break
            
            isActive = ContributorActivity[githubName]

            if not inactiveRole in user.roles:
                log("A contributor ({}) is not marked as inactive contributor, adding role... ".format(user.name))
                if not DEBUG_ONLY: await Client.add_roles(user, inactiveRole)
                print("done.")

            if isActive and not activeRole in user.roles:
                log("An active contributor ({}) doesn't have active role, adding role... ".format(user.name))
                if not DEBUG_ONLY: await Client.add_roles(user, activeRole)
                print("done.")
            elif not isActive and activeRole in user.roles:
                log("An inactive contributor ({}) is marked as active, removing role... ".format(user.name))
                if not DEBUG_ONLY: await Client.remove_roles(user, activeRole)
                print("done.")
        else:
            if activeRole in user.roles:
                log("A non-contributor ({}) is marked as active contributor, removing role... ".format(user.name))
                if not DEBUG_ONLY: await Client.remove_roles(user, activeRole)
                print("done.")

            if inactiveRole in user.roles:
                log("A non-contributor ({}) is marked as inactive contributor, removing role... ".format(user.name))
                if not DEBUG_ONLY: await Client.remove_roles(user, activeRole)
                print("done.")

async def background_task():
    print("Welcome!")
    await Client.wait_until_ready()

    while True:
        ReadContributorFile()
        await Update()
        await asyncio.sleep(900) # 15 minutes

def log(text, fileOnly = False):
    text = "[{}] {}".format(datetime.now(), text)
    with open("logs.txt", 'a+') as log_file:
        log_file.write(text if text.endswith('\n') else '{}\n'.format(text))

    if not fileOnly:
        print(text, end='')


Client.loop.create_task(background_task())
Client.run(DISCORD_TOKEN)
