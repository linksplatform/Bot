# LinksPlatform Discord Bot - Programming Language Role Synchronization

This Discord bot automatically synchronizes programming language roles with the languages users actually know based on their GitHub repositories.

## Features

- **Automatic Role Assignment**: Assigns Discord roles based on programming languages detected from GitHub repositories
- **GitHub Integration**: Analyzes public repositories to determine language expertise
- **User-Friendly Commands**: Simple commands for role synchronization and language checking
- **Configurable**: Supports custom role mappings and thresholds
- **Logging**: Comprehensive logging for monitoring and debugging

## Commands

- `!sync-roles <github-username>` - Links GitHub account and synchronizes Discord roles
- `!my-sync` - Re-synchronizes roles using previously linked GitHub account
- `!check-languages <github-username>` - Shows detected programming languages without changing roles
- `!help` - Shows available commands

## Setup

### Prerequisites

1. **Discord Bot Token**: Create a bot application in Discord Developer Portal
2. **GitHub Token**: Generate a personal access token for GitHub API access
3. **Discord Server**: Bot needs to be added to your Discord server with appropriate permissions

### Required Permissions

The bot needs the following Discord permissions:
- View Channels
- Send Messages
- Manage Roles
- Use Slash Commands
- Read Message History

### Configuration

1. Copy `appsettings.json` and update the following values:
   - `Token`: Your Discord bot token
   - `GitHubToken`: Your GitHub personal access token
   - `GuildId`: Your Discord server (guild) ID
   - `LanguageRoles`: Map programming languages to Discord role IDs

2. Create the corresponding roles in your Discord server and note their IDs

### Running the Bot

```bash
cd csharp/DiscordBot
dotnet run
```

## How It Works

1. **Language Detection**: The bot analyzes a user's public GitHub repositories to determine which programming languages they use
2. **Role Mapping**: Based on the detected languages and configured thresholds, the bot determines which roles the user should have
3. **Role Synchronization**: The bot adds relevant roles and removes roles for languages the user doesn't actively use
4. **Persistence**: User-GitHub mappings are stored locally for future synchronizations

## Language Support

The bot can detect and assign roles for the following languages:
- C#, C++, JavaScript, TypeScript, Python, Java
- Go, Rust, Ruby, PHP, Swift, Kotlin
- Dart, Scala, F#, Clojure, Haskell
- And more (easily configurable)

## Architecture

- **DiscordBotService**: Main bot service handling Discord events and commands
- **GitHubLanguageDetectionService**: Analyzes GitHub repositories for programming languages
- **ProgrammingLanguageRoleService**: Manages role synchronization logic
- **LanguageRoleModule**: Discord command handlers
- **Storage Integration**: Uses existing FileStorage for persistence

## Contributing

This bot follows the existing LinksPlatform Bot patterns and integrates with the existing storage and interface systems.