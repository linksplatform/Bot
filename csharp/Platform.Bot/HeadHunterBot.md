# HeadHunter Bot

## Overview

The HeadHunter Bot is designed to help recruit programmers to join the LinksPlatform team by asking a simple question: **"Would you like to become a part of LinksPlatform team?"**

As specified in the requirements, the bot focuses only on users who answer "yes" and ignores those who answer "no" to save time.

## Features

### 1. HeadHunterTrigger
- **Trigger Condition**: Issues with titles containing "headhunter", "recruit", or "join team"
- **Action**: Posts a recruitment question with clear yes/no options
- **Question**: "Would you like to become a part of LinksPlatform team?"

### 2. HeadHunterResponseTrigger  
- **Trigger Condition**: Issues that have received the HeadHunter question and have user responses
- **Action**: 
  - **For "Yes" responses**: Provides next steps for joining the team
  - **For "No" responses**: Thanks the user and closes the issue

## Usage

### Triggering the HeadHunter Bot

Create a GitHub issue with a title containing:
- "headhunter"
- "recruit" 
- "join team"

Example issue titles:
- "HeadHunter Request"
- "Recruit new developers"
- "Looking for team members to join"

### Bot Response Flow

1. **Initial Question**: Bot posts the recruitment question with clear options
2. **User Response**: User responds with "Yes" ✅ or "No" ❌
3. **Bot Action**:
   - **Yes Response**: Provides detailed next steps for joining
   - **No Response**: Politely closes the issue

## Implementation Details

### Files Created
- `HeadHunterTrigger.cs` - Main trigger for posting recruitment questions
- `HeadHunterResponseTrigger.cs` - Processes user responses
- Integration in `Program.cs` - Registers the triggers with the bot system

### Dependencies
- Uses existing `GitHubStorage` class for GitHub API interactions
- Implements `ITrigger<Issue>` interface following the established pattern
- Integrates with existing `IssueTracker` system

## Benefits

1. **Time Efficient**: Automatically ignores "no" responses as specified
2. **Focused Recruitment**: Only processes interested candidates  
3. **Consistent Process**: Standardized approach to team recruitment
4. **GitHub Integration**: Works seamlessly within existing GitHub workflows

## Example Interaction

```
User creates issue: "HeadHunter - Looking for C# developers"
↓
Bot responds: "Would you like to become a part of LinksPlatform team?"
↓
User responds: "Yes ✅"
↓
Bot provides next steps with contact information and requirements
```

This implementation fulfills the requirement to create a bot that asks programmers about joining the LinksPlatform team while focusing only on positive responses to save time.