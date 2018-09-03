import pytest

from github.GithubException import RateLimitExceededException

from controbot.main import log, read_csv, GithubContributorReader, DiscordContributorBot


def test_log(capsys):
    log("test")
    captured = capsys.readouterr()
    assert captured.out == "test\n"
    log("test", end="kek")
    captured = capsys.readouterr()
    assert captured.out == "testkek"


def test_read_csv():
    result = read_csv("usermap.csv")
    assert "MythicManiac" in result.values()


def test_github():
    github_reader = GithubContributorReader(
        organization_name="LeagueSandbox",
        github_token=None,
    )
    try:
        result = github_reader.get_last_contribution_dates(
            filter_repos=["Specifications"]
        )
        assert "MythicManiac" in result
    except RateLimitExceededException:
        pytest.skip("GitHub rate limit exceeded")


def test_discord():
    discord_bot = DiscordContributorBot(
        github_reader=None,
        usermap=None,
    )
    assert discord_bot.usermap is None
