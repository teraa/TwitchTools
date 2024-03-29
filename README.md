# TwitchTools
Command line tools for Twitch

## Usage
```
Usage:
  TwitchTools [options] [command]

Options:
  --version         Show version information
  -?, -h, --help    Show help and usage information

Commands:
  follows <From|To> <user>                   Get a list of follows
  info <user>                                Get info of a user
  infobatch <users>                          Get info of multiple users
  bantool <channel> <command> <arguments>    Execute commands in a channel for each specified user
                                             [command: ban, arguments: ]
```
For detailed usage use `-h` or equivalent option for each command.

## Environment variables
Each command supports providing all arguments directly from the command line. Following values will fall back to environment variables if not provided on the command line.

Variable      | Option        | Commands                        | Description
------------- | ------------- | ------------------------------- | --------------------------
`ACCESSTOKEN` | `--token`     | `follows`, `info`, `infobatch`  | Access token for Helix API
`CLIENTID`    | `--client-id` | `follows`, `info`, `infobatch`  | Client ID for Helix API
`CHAT_LOGIN`  | `--login`     | `bantool`                       | Login username for chat
`CHAT_TOKEN`  | `--token`     | `bantool`                       | Token for chat
