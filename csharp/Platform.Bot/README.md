# Bot

## Prerequisites
* [Git](https://git-scm.com/downloads)
* [.NET SDK](https://dotnet.microsoft.com/download)

## Install
```
git clone https://github.com/linksplatform/Bot
cd Bot/csharp/Bot
dotnet restore
```

## Prepare 

For the bot to work, you need to get a token. You can do this here: https://github.com/settings/tokens/new.  

## Synopsis

```sh
dotnet run NICKNAME TOKEN APP_NAME [LINKS_DB] [FILE_SET_NAME] [MINIMUM_INTERACTION_INTERVAL]
```

### Parameters

0. `NICKNAME` - A username.
1. `TOKEN` - [A GitHub access token](https://github.com/settings/tokens).
2. A name of your [GitHub App](https://github.com/settings/apps).
3. Path to a database file. **Default**: `db.links`
4. File set name. **Default**: `HelloWorldSet`.

## Example

```Shell
dotnet run MyNickname ghp_123 MyAppName db.links HelloWorldSet
```

## Quick run by using sh script `run.sh`:

```shell
./run.sh NICKNAME TOKEN APP_NAME
```

## Features

The bot responds to GitHub issues with specific triggers:

- **Hello World**: Create issues with title "hello world" to test the bot
- **Organization Last Month Activity**: Create issues with title "organization last month activity" to get member activity
- **Top by Technology**: Create issues with title "Top by technology [TECHNOLOGY_NAME]" to get users ranked by their activity with specific technologies

### Top by Technology

The "Top by technology" feature analyzes repositories in your organization and ranks users based on:
- Commit activity in repositories that contain the specified technology
- Overall contribution activity weighted by technology usage
- Repository language analysis and file patterns

Example usage:
- "Top by technology CUDA" - Find users working with CUDA
- "Top by technology Qt" - Find users working with Qt
- "Top by technology Docker" - Find users working with Docker
- "Top by technology React" - Find users working with React

The bot will analyze the last 3 months of activity and return the top 10 contributors for the specified technology.
