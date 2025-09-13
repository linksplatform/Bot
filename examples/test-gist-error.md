# Example Issue with Gist Error

This is an example of how the GoogleSearchErrorTrigger would work.

Here's my error in a gist: https://gist.github.com/example/abc123

The expected behavior:
1. The bot detects the gist URL in the issue
2. Extracts error messages from the gist content
3. Searches Google for solutions on whitelisted sites
4. Replies with helpful search links

## Sample gist content that would trigger the bot:

```
System.NullReferenceException: Object reference not set to an instance of an object
   at MyApp.Controllers.HomeController.Index() in HomeController.cs:line 25
   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.Execute()
```

The bot would detect the "System.NullReferenceException" and provide search links for Stack Overflow, docs.microsoft.com, etc.