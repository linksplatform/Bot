# LinksBot for Telegram

A modern Telegram bot implementation for programmers with karma tracking, programming language profiles, GitHub integration, and Wikipedia search.

## Features

- **User Profiles**: Track programming languages and GitHub profiles
- **Karma System**: Community-driven reputation system with voting
- **Programming Languages**: Support for 130+ programming languages
- **GitHub Integration**: Link your GitHub profile to your bot account
- **Wikipedia Search**: Quick access to Wikipedia information
- **Group Chat Support**: Full functionality in group chats and channels

## Commands

### General Commands
- `/start` - Start using the bot
- `/help` - Show help message with all commands
- `/info` - Show your profile information (or reply to see someone else's)
- `/update` - Update your profile information

### Programming Languages
- `/add_lang <language>` - Add a programming language to your profile
- `/remove_lang <language>` - Remove a programming language from your profile

### GitHub Profile
- `/add_github <username>` - Add your GitHub profile
- `/remove_github` - Remove your GitHub profile

### Karma System
- `/karma` - Show your karma (or reply to a message to see someone's karma)
- `/top [number]` - Show top users by karma (default: 10)
- `/bottom [number]` - Show bottom users by karma (default: 10)
- `+` (reply to message) - Vote to increase someone's karma
- `-` (reply to message) - Vote to decrease someone's karma

### Information
- `/people` - Show all chat members with their profiles
- `/what_is <query>` - Search Wikipedia for information

## Karma System

The karma system is community-driven with the following rules:

- **Positive karma**: Requires 2 votes to increase karma by 1
- **Negative karma**: Requires 3 votes to decrease karma by 1
- **Voting cooldown**: Based on your karma level (higher karma = shorter cooldown)
- **Protection**: Users with negative karma cannot be voted down further

### Voting Cooldowns
| Karma Range | Cooldown |
|-------------|----------|
| ≤ -20       | 8 hours  |
| -19 to -2   | 4 hours  |
| -1 to 2     | 2 hours  |
| 2 to 20     | 1 hour   |
| ≥ 20        | 30 min   |

## Programming Languages

The bot supports 130+ programming languages including:
- **Popular**: Python, JavaScript, TypeScript, Java, C++, C#, Go, Rust
- **Functional**: Haskell, F#, Scala, Clojure, Erlang, Elixir
- **System**: C, C++, Rust, Go, Assembly
- **Web**: JavaScript, TypeScript, PHP, Ruby, Python
- **Mobile**: Swift, Kotlin, Dart, Objective-C
- **And many more!**

## Installation

### Prerequisites
- Python 3.8 or higher
- pip (Python package manager)

### Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/linksplatform/Bot.git
   cd Bot/telegram
   ```

2. **Install dependencies**:
   ```bash
   pip install -r requirements.txt
   ```

3. **Configuration**:
   - Create a Telegram bot by messaging [@BotFather](https://t.me/botfather)
   - Get your bot token
   - Edit `config.py` and set your `BOT_TOKEN`

4. **Run the bot**:
   ```bash
   python main.py
   ```

## Configuration

Edit `telegram/config.py` to customize:

- `BOT_TOKEN` - Your Telegram bot token (required)
- `POSITIVE_VOTES_PER_KARMA` - Votes needed for positive karma (default: 2)
- `NEGATIVE_VOTES_PER_KARMA` - Votes needed for negative karma (default: 3)
- `KARMA_LIMIT_HOURS` - Cooldown periods by karma level
- `DEFAULT_PROGRAMMING_LANGUAGES` - Supported programming languages

## Data Storage

The bot uses simple JSON file storage (no database required):
- `data/users.json` - User profiles and karma
- `data/karma_votes.json` - Voting history

This approach follows the project's preference for avoiding SQL databases while maintaining clean, readable data.

## Development

The bot follows a modular architecture:

- `main.py` - Bot initialization and command routing
- `config.py` - Configuration settings
- `modules/storage.py` - Data storage management (JSON-based)
- `modules/commands.py` - Command handlers and business logic

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test the bot functionality
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

## Support

- **Repository**: https://github.com/linksplatform/Bot
- **Issues**: https://github.com/linksplatform/Bot/issues
- **Discussions**: https://github.com/linksplatform/Bot/discussions

## Architecture

The Telegram bot is designed with clean architecture principles:

- **No SQL Dependencies**: Uses simple JSON storage as preferred by the project
- **Modular Design**: Separated concerns for storage, commands, and bot logic
- **Async/Await**: Modern Python async programming with aiogram 3.x
- **Type Hints**: Full type annotations for better code quality
- **Error Handling**: Graceful error handling and user feedback

This implementation provides all the core features of the original VK bot while being optimized for the Telegram platform.