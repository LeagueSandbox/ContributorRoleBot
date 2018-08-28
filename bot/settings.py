import os

from dotenv import load_dotenv


load_dotenv()

ORGANIZATION = os.getenv("ORGANIZATION")
GITHUB_TOKEN = os.getenv("GITHUB_TOKEN")
DISCORD_TOKEN = os.getenv("DISCORD_TOKEN")
