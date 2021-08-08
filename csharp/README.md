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

## Configure

For the bot to work, you need to get a token. You can do this here: https://github.com/settings/tokens/new.  
Arguments:

0. Your username
1. Your token
2. App Name. can be anything, it is not necessary to register
3. Path to a database file.
4. The default file set name is `HelloWorldSet`

## Run

```Shell
dotnet run
```
