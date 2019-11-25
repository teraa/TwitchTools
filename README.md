# TwitchTools
Command line tools for Twitch

## Usage
```
dotnet run [MODULE]
```

### Modules
```
Modules: Followers, Following
Arguments: <channel>
Options:
    -l, --limit
            number of users to fetch (default: 100)
    -o, --offset
            starting offset
    -d, --direction (default: desc)
            'asc' for ascending order or 'desc' for descending

Module: Info
Arguments: [username]
Options:
    -d, --date
            [Flag] sort users by date of creation
    -n, --name
            [Flag] sort users by name
    -c
            check namechanges (true/false)

Module: BanTool
Arguments: <channel>
Options:
    -c, --command
            command (default: ban)
    -a, --args
            command args
    -l, --limit
            maximum number of actions per period (default: 95)
    -p, --period
            period (seconds) in which a limited number of actions can be performed (default: 30)
        --login
            login username (default: tw_login environment variable)
        --token
            oauth token (default: tw_token environment variable)
```
