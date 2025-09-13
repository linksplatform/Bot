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

The bot supports several triggers that respond to specific GitHub issues:

### Commit Collection Feature

To collect all commit links for a specific user in chronological order, create an issue with one of these titles:
- `Collect commits for user <username>`
- `User commits <username>`
- `Collect user commits`
- `Collect all user commits`

You can also specify the username in the issue body using `@username` syntax.

**Example:**
1. Create an issue titled: "Collect commits for user konard"
2. The bot will automatically:
   - Find all commits by that user across all organization repositories
   - Sort them chronologically (oldest first)
   - Generate a detailed report with links to all commits
   - Post the results as a comment on the issue
   - Close the issue when complete

The output includes:
- Total number of commits found
- Commits grouped by repository
- Each commit with timestamp and direct link to GitHub
- Formatted as markdown for easy reading
