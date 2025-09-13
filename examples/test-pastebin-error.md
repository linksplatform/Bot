# Example Issue with Pastebin Error

Another example showing pastebin support:

Here's the stack trace: https://pastebin.com/xyz789

## Sample pastebin content that would trigger the bot:

```
TypeError: Cannot read properties of undefined (reading 'length')
    at processArray (/app/src/utils.js:15:20)
    at main (/app/src/index.js:8:5)
    at Object.<anonymous> (/app/src/index.js:25:1)
```

The bot would detect the JavaScript "TypeError" and provide relevant search links for JavaScript debugging resources.

## Error patterns the bot can detect:

- Exception types (NullReferenceException, TypeError, etc.)
- Compilation errors (error CS1234, error C2065)
- Runtime errors (Uncaught Exception)
- HTTP errors (404 Not Found, 500 Internal Server Error)
- Build failures (Build FAILED)
- Database errors (SQL error)