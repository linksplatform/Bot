# Test Plan for Plus Message Deletion Feature

## Overview
This document outlines how to test the new "+ " message deletion feature.

## Feature Description
The bot now automatically deletes comments containing only "+ " (plus space) from GitHub issues after 6 hours have passed since the last such comment was posted.

## Implementation Details
1. **GitHubStorage.cs**: Added methods to get and delete issue comments
   - `GetIssueComments(long repositoryId, int issueNumber)`
   - `DeleteIssueComment(long repositoryId, long commentId)`

2. **DeletePlusMessagesTrigger.cs**: New trigger that:
   - Checks for comments containing only "+ "
   - Verifies if 6 hours have passed since the last "+ " comment
   - Deletes all "+ " comments from the issue

3. **Program.cs**: Integrated the new trigger into the issue tracker

## Testing Steps

### Manual Testing (Recommended)
1. Create a test issue in a repository the bot monitors
2. Add several comments containing only "+ " with timestamps
3. Wait for the bot's polling interval (typically 60 seconds)
4. For comments older than 6 hours, verify they get deleted
5. For comments newer than 6 hours, verify they remain

### Unit Testing Scenarios
1. **No Plus Comments**: Issue with no "+ " comments - should not trigger
2. **Recent Plus Comments**: Issue with "+ " comments less than 6 hours old - should not delete
3. **Old Plus Comments**: Issue with "+ " comments older than 6 hours - should delete all
4. **Mixed Comments**: Issue with both "+ " and regular comments - should only delete "+ " comments
5. **Error Handling**: Test API failures and network issues

## Expected Behavior
- Only comments containing exactly "+ " (plus followed by space) are deleted
- Comments are only deleted if 6 hours have passed since the last "+ " comment
- All "+ " comments in the issue are deleted at once, not just the old ones
- Other comments remain untouched
- Bot logs successful deletions to console

## Files Modified
- `csharp/Storage/RemoteStorage/GitHubStorage.cs`: Added comment management methods
- `csharp/Platform.Bot/Triggers/DeletePlusMessagesTrigger.cs`: New trigger implementation
- `csharp/Platform.Bot/Program.cs`: Integrated new trigger