# Enhanced Auto-Update Functionality

This document demonstrates the new enhanced auto-update functionality implemented for issue #48.

## New Commands

### 1. Enhanced Individual Profile Update
- **Command**: `обновить` / `update`
- **Description**: Updates user profile with comprehensive information from VK API
- **Features**:
  - Updates user name (first name + last name)
  - Auto-detects GitHub profile from VK site field
  - Auto-detects programming languages from activities/about sections
  - Provides detailed feedback on what was updated

**Example Usage**:
```
User: обновить
Bot: Профиль обновлен!
     Обновленные поля: имя, GitHub профиль, языки программирования (Python, JavaScript)
     
     [Shows updated profile info]
```

### 2. Bulk Profile Updates (Admin Only)
- **Command**: `обновить всех` / `update all`
- **Description**: Updates all user profiles in the current chat
- **Requirements**: High karma (50+) or admin privileges
- **Features**:
  - Only updates users with auto-update enabled
  - Processes all chat members
  - Provides summary of updated profiles

**Example Usage**:
```
Admin: обновить всех
Bot: Массовое обновление завершено!
     Обработано пользователей: 25
     Обновлено профилей: 8
```

### 3. Auto-Update Settings
- **Command**: `автообновление вкл/выкл` / `auto-update on/off`
- **Description**: Controls automatic profile updates for the user
- **Features**:
  - Enable/disable periodic auto-updates
  - Check current auto-update status

**Example Usage**:
```
User: автообновление вкл
Bot: Автообновление профиля включено.

User: автообновление выкл  
Bot: Автообновление профиля отключено.

User: автообновление
Bot: Автообновление профиля сейчас включено.
```

## Automatic Features

### 1. GitHub Profile Detection
The bot automatically detects GitHub profiles from the VK "site" field:
- Supports various GitHub URL formats
- Validates that the GitHub profile exists before updating
- Extracts username from URLs like:
  - `https://github.com/username`
  - `http://github.com/username`
  - `github.com/username`

### 2. Programming Language Detection
Automatically detects programming languages mentioned in:
- VK "activities" field
- VK "about" field
- Supports 100+ programming languages
- Case-insensitive detection
- Only adds new languages (doesn't remove existing ones)

### 3. Periodic Auto-Updates
- Runs every 6 hours automatically
- Only updates users with auto-update enabled
- Only updates profiles that haven't been updated in 24+ hours
- Processes maximum 10 users per cycle to respect API limits
- Logs all automatic updates

## Data Storage

New user profile fields:
- `auto_update_enabled` (boolean): Controls automatic updates
- `last_auto_update` (timestamp): Tracks when profile was last auto-updated

## Technical Implementation

### New Patterns Added
```python
UPDATE_ALL = recompile(
    r'\A\s*(обновить всех|update all)\s*\Z', IGNORECASE)

AUTO_UPDATE = recompile(
    r'\A\s*(авто[- ]?обновление|auto[- ]?update)\s+(вкл|on|выкл|off)\s*\Z', IGNORECASE)
```

### Enhanced VK API Usage
The enhanced update uses comprehensive VK API fields:
```python
fields='about,activities,bdate,books,career,city,contacts,education,games,interests,movies,music,personal,quotes,relation,schools,site,tv,universities'
```

### Error Handling
- Graceful handling of VK API errors
- Validation of GitHub profiles before updating
- Protection against API rate limits
- Comprehensive logging for debugging

## Security & Performance

### Admin Protection
- Bulk updates require high karma (50+) or specific user ID
- Only available in group chats, not private messages

### API Rate Limiting
- Periodic updates process maximum 10 users at a time
- 6-hour intervals between automatic update cycles
- Respects VK API rate limits

### Privacy
- Users can disable auto-updates at any time
- Only processes public VK profile information
- No storage of sensitive data

## Migration

Existing users automatically get:
- `auto_update_enabled = True` (opt-in by default)
- `last_auto_update = 0` (eligible for immediate update)

## Examples of Detection

### GitHub Profile Detection
```
VK Site Field: "Check out my code at https://github.com/developer123"
Result: GitHub profile set to "developer123"
```

### Programming Language Detection
```
VK Activities: "Coding in Python, learning JavaScript, love C++"
VK About: "Full-stack developer specializing in web technologies"
Result: Adds "Python", "JavaScript", "C++" to programming languages
```

## Help Message Update

The help command now includes information about auto-update commands:

```
🔄 Команды обновления профиля:
• обновить/update — обновить свой профиль
• обновить всех/update all — массовое обновление (для админов)  
• автообновление вкл/выкл — управление автообновлением
```