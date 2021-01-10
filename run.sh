#!/bin/bash
set -a
. ~/.twitch-cli/.twitch-cli.env
set +a
export LOGIN='xx'
/opt/twitchtools/TwitchTools "$@"
