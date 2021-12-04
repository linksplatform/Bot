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

## Run

CLI Arguments:

0. Your username.
1. Your token.
2. The name of your GitHub App (required by GitHub to login).
3. Path to a database file.
4. File set name (the default is ``HelloWorldSet``).
```Shell
dotnet run
```

## Example

```Shell
dotnet run nickname token BestAppEver db.links HelloWorldSet
```

