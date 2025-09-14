# User Links Feature Examples

This document demonstrates how to use the user links feature in the LinksPlatform Bot.

## Supported Commands

### 1. Add a link
Create a GitHub issue with the title: `add link <platform> <url>`

**Examples:**
- `add link GitHub https://github.com/username`
- `add link StackOverflow https://stackoverflow.com/users/12345/username`
- `add link GitLab https://gitlab.com/username`

### 2. Remove a link
Create a GitHub issue with the title: `remove link <platform>`

**Examples:**
- `remove link GitHub`
- `remove link StackOverflow`
- `remove link GitLab`

### 3. List your links
Create a GitHub issue with the title: `my links` or `list my links`

### 4. List another user's links
Create a GitHub issue with the title: `list links <username>`

**Example:**
- `list links konard`

## Supported Platforms

The bot currently supports these platforms with domain validation:

- **GitHub**: github.com
- **StackOverflow**: stackoverflow.com, serverfault.com, superuser.com, askubuntu.com, mathoverflow.net
- **GitLab**: gitlab.com

## Bot Responses

The bot will respond with:
- ✅ Success messages for valid operations
- ❌ Error messages for invalid platforms, URLs, or duplicate entries
- Detailed information when listing links
- Commands reference when viewing your own links

## Security Features

- **Domain Whitelist**: Only approved domains are allowed for each platform
- **URL Validation**: All URLs must be valid and match the platform's allowed domains
- **User Isolation**: Each user can only manage their own links
- **Platform Uniqueness**: Each user can have only one link per platform

## Example Workflow

1. User creates issue: "add link GitHub https://github.com/myusername"
2. Bot validates the platform and URL
3. Bot adds the link to storage
4. Bot responds with success message and closes the issue

If the user later creates issue: "my links"
- Bot shows all their registered links with platform and URL
- Bot provides command reference for managing links