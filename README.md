# Contributor Role Bot
[![Build Status](https://travis-ci.org/LeagueSandbox/ContributorRoleBot.svg?branch=master)](https://travis-ci.org/LeagueSandbox/ContributorRoleBot)
[![codecov](https://codecov.io/gh/LeagueSandbox/ContributorRoleBot/branch/master/graph/badge.svg)](https://codecov.io/gh/LeagueSandbox/ContributorRoleBot)

## What is it

Contributor Role Bot is a simple bot built in Python that grants Discord roles based on contribution activity to a
GitHub organization.

This is used on the LeagueSandbox Discord to grant contributors either the "active contributor" or the
"inactive contributor" role, depending on how long ago was the last contribution made.

## Setup

* Install python
* Install requirements (`pip install -r requirements.txt` while in the `bot` directory)
* Set the `DISCORD_TOKEN`, `GITHUB_TOKEN`, and `ORGANIZATION` environment variables
* Edit the `settings.py` as required
* Run `python main.py`

## Deployment

To perform a deployment to Kubernetes, good instructions can be found from [the deployment readme](kubernetes/README.md).

All updates merged to the master branch of this repository will be automatically deployed.
