from controbot import settings


def test_settings():
    keys = [
        "DISCORD_TOKEN",
        "GITHUB_TOKEN",
        "ORGANIZATION",
        "DISCORD_SERVER_ID",
        "DISCORD_ACTIVE_ROLE_ID",
        "DISCORD_INACTIVE_ROLE_ID",
        "DISCORD_NOTIFY_CHANNEL_ID",
        "DISCORD_ADMIN_ID",
        "INACTIVE_DAYS_THRESHOLD",
    ]

    for key in keys:
        assert hasattr(settings, key)
