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
