# Message Logging Feature

This feature allows the bot to forward all messages from specified main chats to a dedicated logging chat for monitoring and archival purposes.

## Configuration

To enable message logging, edit `config.py` and configure the following variables:

### MAIN_CHATS
List of chat IDs from which messages should be forwarded to the logging chat.

```python
MAIN_CHATS = [
    2000000001,  # Example main chat ID
    2000000011   # Another main chat ID
]
```

### LOGGING_CHAT_ID
The chat ID where all messages from main chats will be forwarded for logging.

```python
LOGGING_CHAT_ID = 2000000020  # Example logging chat ID
```

## How to Find Chat IDs

1. Go to the VK conversation you want to use
2. Look at the URL in your browser address bar
3. For group chats, the URL will look like: `https://vk.com/im?peers=c477` 
4. The chat ID is `2000000000 + 477 = 2000000477`

## Message Format

Messages forwarded to the logging chat include:

- **Timestamp**: When the original message was sent
- **Chat title**: Name of the source chat
- **User name**: Who sent the message
- **Message content**: The actual message text
- **Attachments**: List of attachment types (if any)
- **Special indicators**: For forwarded messages and replies

Example logged message:
```
[2024-01-01 12:30:45] Development Chat
John Doe: Hello, how is everyone doing?
Attachments: [photo], [doc]
[Reply to message]
```

## Features

### What Gets Logged
- All text messages from configured main chats
- Information about attachments (type only, not content)
- Indication of forwarded messages
- Indication of replies to other messages
- User names and chat titles for context

### What Doesn't Get Logged
- Messages from non-main chats
- Bot messages (to avoid infinite loops)
- Messages when logging is disabled or misconfigured
- Actual attachment content (for privacy and performance)

### Error Handling
- If logging fails for any reason, an error is printed to console
- The bot continues to function normally even if logging fails
- Invalid configurations are silently ignored

## Security Considerations

- The logging chat should have restricted access
- Only trusted administrators should have access to logged messages
- Consider the privacy implications of logging all messages
- Ensure the logging chat is properly secured against unauthorized access

## Disabling Logging

To disable logging, set either:
- `LOGGING_CHAT_ID = None`
- `MAIN_CHATS = []`

The bot will automatically skip logging when these are not properly configured.