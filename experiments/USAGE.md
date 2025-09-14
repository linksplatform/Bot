# Bot Message Auto-Deletion Feature

This document explains the enhanced message deletion functionality for the VK bot.

## Overview

The bot now supports automatically deleting all bot messages after a configurable period, not just karma-related messages. This helps keep chats clean and reduces clutter from bot responses.

## Configuration

### New Configuration Options

Add these settings to your `config.py`:

```python
# Delete ALL bot messages after certain period (in minutes, set to 0 to disable)
BOT_MESSAGE_DELETE_DELAY_MINUTES = 60  # Delete bot messages after 1 hour

# Chats where bot messages will be auto-deleted (leave empty to use CHATS_DELETING)
BOT_MESSAGE_DELETE_CHATS = []
```

### Configuration Options

1. **BOT_MESSAGE_DELETE_DELAY_MINUTES**
   - Set to `0` to disable auto-deletion of bot messages
   - Set to any positive number to enable deletion after that many minutes
   - Examples: `5` (5 minutes), `60` (1 hour), `1440` (24 hours)

2. **BOT_MESSAGE_DELETE_CHATS** 
   - List of chat IDs where bot messages should be auto-deleted
   - If empty (`[]`), falls back to using `CHATS_DELETING` configuration
   - Example: `[2000000001, 2000000006]`

### Required Configuration

For message deletion to work, you must also configure:

1. **USERBOT_CHATS** - Maps VK chat IDs to userbot peer IDs
   ```python
   USERBOT_CHATS = {
       2000000001: 477,  # VK chat ID: userbot peer ID
       2000000006: 423
   }
   ```

2. A working userbot that has permission to delete messages in the target chats

## How It Works

1. **Message Sending**: When the bot sends any message using `send_msg()`, it checks if auto-deletion is enabled
2. **Scheduling**: If enabled and the chat is configured for deletion, the message gets scheduled for deletion
3. **Background Cleanup**: The existing message cleanup mechanism processes scheduled deletions
4. **Deletion**: The userbot deletes messages when their scheduled time arrives

## Backward Compatibility

This enhancement is fully backward compatible:

- Existing karma message deletion (2-second delay) continues to work unchanged
- If `BOT_MESSAGE_DELETE_DELAY_MINUTES = 0`, no bot messages are auto-deleted
- Existing `CHATS_DELETING` configuration is respected as a fallback

## Examples

### Example 1: Delete all bot messages after 30 minutes
```python
BOT_MESSAGE_DELETE_DELAY_MINUTES = 30
BOT_MESSAGE_DELETE_CHATS = [2000000001, 2000000006]
```

### Example 2: Delete all bot messages after 2 hours, use existing chat config
```python
BOT_MESSAGE_DELETE_DELAY_MINUTES = 120
BOT_MESSAGE_DELETE_CHATS = []  # Uses CHATS_DELETING
```

### Example 3: Disable auto-deletion of bot messages
```python
BOT_MESSAGE_DELETE_DELAY_MINUTES = 0
```

## Testing

Run the test script to validate the configuration:
```bash
python3 experiments/test_config_logic.py
```

The test validates:
- Configuration values are properly set
- Message deletion logic works correctly  
- Time calculations are accurate
- Chat selection logic functions as expected