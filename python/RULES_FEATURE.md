# Rules Management Feature

This document describes the new rules management functionality added to the VK bot.

## Overview

The bot now supports monitoring GitHub Gists containing chat rules and automatically updating pinned messages when rules change.

## Features

### 1. Set Rules from GitHub Gist
- **Command**: `set rules <gist_url>` or `установить правила <gist_url>`
- **Description**: Sets a GitHub Gist as the source for chat rules
- **Example**: `set rules https://gist.github.com/Konard/a7cd43f91c035e412037cbb3de75d540`
- **Requirements**: 
  - Only works in group chats (peer_id > 2e9)
  - Admin permissions recommended (simplified check in current implementation)
  - Gist must be publicly accessible

### 2. Remove Rules Monitoring
- **Command**: `remove rules` or `убрать правила`
- **Description**: Removes rules monitoring for the current chat
- **Requirements**: Only works in group chats

### 3. Check Rules Status
- **Command**: `rules status` or `статус правил`
- **Description**: Shows current rules configuration status
- **Shows**:
  - Source gist URL
  - Who set the rules
  - When rules were set
  - Last check timestamp

## How It Works

### Initial Setup
1. Admin uses `set rules <gist_url>` command
2. Bot validates the gist URL and checks accessibility
3. Bot fetches current rules content from the gist
4. Bot posts rules as a pinned message in the chat
5. Bot stores configuration for monitoring

### Automatic Monitoring
1. Background thread checks all configured chats every 5 minutes
2. For each chat, bot checks if gist content has changed
3. If content changed:
   - Bot tries to edit the existing pinned message
   - If editing fails, bot creates a new pinned message
   - Bot notifies the chat about the update

### Data Storage
Rules configuration is stored in `chat_rules.json` with the following structure:
```json
{
  "chat_id": {
    "gist_url": "https://gist.github.com/user/gist_id",
    "gist_id": "gist_id",
    "set_by": user_id,
    "set_at": "2023-01-01T12:00:00",
    "last_check": "2023-01-01T12:05:00",
    "last_content_hash": 12345,
    "pinned_message_id": 67890
  }
}
```

## Implementation Details

### New Files
- `modules/rules_service.py` - Core rules management service
- `python/test_patterns_only.py` - Test script for patterns and GitHub API
- `python/RULES_FEATURE.md` - This documentation

### Modified Files
- `python/patterns.py` - Added new command patterns
- `python/__main__.py` - Added VK API methods and monitoring thread
- `python/modules/commands.py` - Added rules command handlers
- `python/modules/commands_builder.py` - Updated help message

### New VK API Methods
- `pin_message()` - Pin a message in chat
- `unpin_message()` - Unpin a message in chat  
- `send_and_pin_message()` - Send and immediately pin a message
- `edit_message()` - Edit an existing message

### Error Handling
- Network errors when fetching gist content
- VK API errors when pinning/editing messages
- Invalid gist URLs
- Gist access permission issues
- Missing or malformed configuration files

## Usage Examples

### Setting Rules
```
set rules https://gist.github.com/Konard/a7cd43f91c035e412037cbb3de75d540
```
Bot response: "✅ Правила успешно установлены и закреплены!"

### Checking Status
```
rules status
```
Bot response:
```
📊 Статус правил:
🔗 Источник: https://gist.github.com/Konard/a7cd43f91c035e412037cbb3de75d540
👤 Установил: John Doe
📅 Дата установки: 2023-01-01
🔄 Последняя проверка: 2023-01-01T12:05:00
```

### Removing Rules
```
remove rules
```
Bot response: "✅ Мониторинг правил отключен для этого чата."

## Benefits

1. **Centralized Rules Management**: Rules are stored in GitHub Gists, making them easy to edit and version control
2. **Automatic Updates**: No need to manually update pinned messages when rules change
3. **Multi-language Support**: Commands work in both English and Russian
4. **Persistent Configuration**: Settings are saved and restored between bot restarts
5. **Error Resilience**: Comprehensive error handling for network and API issues

## Future Enhancements

1. **Admin Permission Checks**: Implement proper VK admin permission verification
2. **Multiple Gists**: Support multiple gist sources per chat
3. **Custom Update Intervals**: Allow chats to configure monitoring frequency
4. **Rich Text Formatting**: Support Markdown or HTML formatting in rules
5. **Notification Settings**: Allow chats to configure update notifications
6. **Backup/Restore**: Export/import rules configurations