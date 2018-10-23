import sys
import csv

import traceback
import discord
import asyncio

from github import Github
from datetime import datetime, timedelta

from .settings import (
    GITHUB_TOKEN, DISCORD_TOKEN, ORGANIZATION,
    DISCORD_SERVER_ID, DISCORD_ACTIVE_ROLE_ID,
    DISCORD_INACTIVE_ROLE_ID, DISCORD_NOTIFY_CHANNEL_ID,
    INACTIVE_DAYS_THRESHOLD, DISCORD_ADMIN_ID
)


class GithubContributorReader(object):

    @property
    def organization(self):
        if self._organization is None:
            self._organization = self.client.get_organization(self.organization_name)
        return self._organization

    def __init__(self, organization_name, github_token):
        self.organization_name = organization_name
        self._organization = None
        self.client = Github(github_token)
        self.contributors = {}

    def store_contributor_last_commit(self, commit):
        last_commit_date = self.contributors.get(commit.author.login, datetime.min)
        if commit.commit.author.date > last_commit_date:
            self.contributors[commit.author.login] = commit.commit.author.date

    def get_last_contribution_dates(self, filter_repos=None):
        self.contributors = {}

        log(f"Fetching all repositories for {self.organization_name}...")
        repositories = self.organization.get_repos("public")

        for repository in repositories:
            if filter_repos and repository not in filter_repos:
                continue
            log(f"Updating repository {repository.name}")
            self.read_repository(repository)

        log("Contributors fetched")
        return self.contributors

    def read_repository(self, repository):
        contributors = repository.get_contributors()
        for contributor in contributors:
            self.read_contributor_commits(repository, contributor)

    def read_contributor_commits(self, repository, contributor):
        commits = repository.get_commits(author=contributor)
        for commit in commits:
            if self.is_commit_valid(commit):
                self.store_contributor_last_commit(commit)
                break

    def is_commit_valid(self, commit):
        # Commit is a merge commit
        if len(commit.parents) > 1:
            return False

        return True


class DiscordContributorBot(discord.Client):

    def __init__(self, github_reader, usermap):
        super(DiscordContributorBot, self).__init__()
        self.loop.create_task(self.role_update_task())
        self.github_reader = github_reader
        self.usermap = usermap

    @property
    def guild(self):
        return self.get_server(DISCORD_SERVER_ID)

    @property
    def notify_channel(self):
        return self.get_channel(DISCORD_NOTIFY_CHANNEL_ID)

    @property
    def inactive_role(self):
        return discord.utils.get(self.guild.roles, id=DISCORD_INACTIVE_ROLE_ID)

    @property
    def active_role(self):
        return discord.utils.get(self.guild.roles, id=DISCORD_ACTIVE_ROLE_ID)

    async def update_roles(self, contributors):
        log("Updating roles")
        if self.guild is None or self.guild.unavailable:
            log("It seems like either we are not in the discord server or the server is down.\n")
            return

        members = list(self.guild.members)

        for member in members:
            await self.update_member(contributors, member)
        log("Roles updated")

    @asyncio.coroutine
    async def on_error(self, event, *args, **kwargs):
        message = args[0]
        await self.send_message(message.channel, f"```{traceback.format_exc()}```")

    async def log_exception(self):
        await self.send_message(self.notify_channel, f"```{traceback.format_exc()}```")

    async def update_member(self, contributors, member):
        inactive = False
        active = False

        github_name = self.usermap.get(member.id)
        if github_name and github_name in contributors:
            inactive = True

            if contributors[github_name] + timedelta(days=INACTIVE_DAYS_THRESHOLD) > datetime.now():
                active = True

        await self.update_role(member, self.active_role, active)
        await self.update_role(member, self.inactive_role, inactive)

    async def update_role(self, member, role, should_be_in):
        already_in = role in member.roles
        if already_in and not should_be_in:
            log(f"Removed role {role.name} from member {member.name}")
            await self.remove_roles(member, role)
            await self.send_message(self.notify_channel, f"Removed role {role.name} from {member.mention}")
        if not already_in and should_be_in:
            log(f"Added role {role.name} for member {member.name}")
            await self.add_roles(member, role)
            await self.send_message(self.notify_channel, f"Added role {role.name} to {member.mention}")

    @asyncio.coroutine
    async def on_message(self, message):
        if message.author.id != DISCORD_ADMIN_ID:
            return

        if message.content.lower().startswith('!update_roles'):
            contributors = self.github_reader.get_last_contribution_dates()
            await self.update_roles(contributors)
            await self.add_reaction(message, "✅")

    async def role_update_task(self):
        await self.wait_until_ready()

        while True:
            try:
                contributors = self.github_reader.get_last_contribution_dates()
                await self.update_roles(contributors)
            except GithubException:
                pass
            except Exception:
                try:
                    await self.log_exception()
                except Exception:
                    pass
            await asyncio.sleep(900)  # 15 minutes


def log(message, end="\n"):
    message = str(message)
    sys.stdout.write(message + end)
    sys.stdout.flush()


def read_csv(filename):
    return {row[0]: row[1] for row in csv.reader(open(filename))}


def main():
    usermap = read_csv("usermap.csv")

    github_reader = GithubContributorReader(
        organization_name=ORGANIZATION,
        github_token=GITHUB_TOKEN,
    )
    discord_bot = DiscordContributorBot(
        github_reader=github_reader,
        usermap=usermap,
    )
    discord_bot.run(DISCORD_TOKEN)
