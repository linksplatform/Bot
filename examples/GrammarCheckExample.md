# Grammar Check Bot Example

This document demonstrates how to use the GitHub bot's grammar checking functionality.

## Overview

The bot includes a `GrammarCheckTrigger` that automatically checks issues for grammar and spelling errors using the LanguageTool API. When triggered, the bot will:

1. Analyze the issue description for grammar and spelling mistakes
2. Post a comment with suggested corrections
3. Provide context and replacement suggestions for each error found

## How to Trigger

The grammar checker will activate when you create an issue with a title containing any of these keywords:
- "grammar check"
- "spell check" 
- "proofread"
- "grammar"
- "spelling"

## Example Usage

### Issue Title Examples
- "Grammar check please"
- "Can you proofread this text?"
- "Check spelling errors in this document"
- "Grammar and spelling review needed"

### Expected Bot Response

If the issue description contains text like:
```
This is a test with grammer mistakes and speling erors.
```

The bot will respond with:
```
## Grammar and Spelling Check Results

Found **3** potential issue(s):

**1.** Possible spelling mistake found.
   - Context: "This is a test with **grammer** mistakes and speling erors."
   - Suggested correction(s): grammar, grimmer, rammer, crammer, g rammer

**2.** Possible spelling mistake found.
   - Context: "...his is a test with grammer mistakes and **speling** erors."
   - Suggested correction(s): spelling, spewing, spieling

**3.** Possible spelling mistake found.
   - Context: "... test with grammer mistakes and speling **erors**."
   - Suggested correction(s): errors, Eros, errs

---
*Grammar checking powered by LanguageTool API*
```

## Technical Details

- **API**: Uses LanguageTool public API (https://api.languagetool.org/v2/check)
- **Language**: Defaults to English (US)
- **Trigger Logic**: Located in `csharp/Platform.Bot/Triggers/GrammarCheckTrigger.cs`
- **Integration**: Added to the main issue tracker in `Program.cs`

## Configuration

The trigger is automatically enabled when the bot starts. No additional configuration is required.

## Limitations

- Uses the free LanguageTool API (limited requests per day)
- Only checks the issue description, not comments
- Currently supports English language only
- Requires internet connectivity to reach the LanguageTool API