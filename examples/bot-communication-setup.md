# Bot Communication Setup

This example shows how to set up the private messaging system for the bot.

## Prerequisites

1. Create a private repository called `bot-communications` in your organization
2. Make sure the bot has access to this repository

## How it works

When a trigger implements `IPrivateMessageTrigger<Issue>` instead of `ITrigger<Issue>`:

1. The bot generates the message content privately
2. Creates an issue in the `bot-communications` repository with the message
3. Mentions the target user in that issue (so they get email notification)
4. Posts a minimal comment in the original issue with a link to the private message

## Example Usage

```csharp
// Old way - creates big public comments
internal class MyTrigger : ITrigger<Issue> 
{
    public async Task Action(Issue issue) 
    {
        await _github.CreateIssueComment(issue.Repository.Id, issue.Number, "Very long message...");
    }
}

// New way - sends private messages
internal class MyTrigger : IPrivateMessageTrigger<Issue> 
{
    public string GetTargetUserLogin(Issue issue) => issue.User.Login;
    public string GetMessageSubject(Issue issue) => "Report Title";
    public async Task<string> GetPrivateMessageContent(Issue issue) => "Very long message...";
    public async Task Action(Issue issue) 
    {
        // Only handle issue closing, messaging is automatic
        await _github.Client.Issue.Update(issue.Repository.Owner.Login, issue.Repository.Name, issue.Number, new IssueUpdate() { State = ItemState.Closed });
    }
}
```

## Benefits

- ✅ Reduces clutter in main issue threads
- ✅ Users still get notified via email mentions
- ✅ Messages are preserved in a dedicated space
- ✅ Original issues stay clean and readable