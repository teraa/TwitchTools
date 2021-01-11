#!/bin/bash
set -a
. ~/.twitch-cli/.twitch-cli.env
set +a
export LOGIN='xx'
export CHAT_TOKEN='xx'
/opt/twitchtools/TwitchTools "$@"
