# Google Search Bot Feature Examples

This document demonstrates how the Google search functionality works in the VK bot.

## Usage Examples

### Basic Search Commands

```
search python list comprehension
google javascript async await  
найди react hooks tutorial
поиск how to use git
```

### Command Structure

- **Trigger words**: `search`, `google`, `найди`, `поиск`
- **Minimum words**: 3 words required in the query
- **Maximum results**: Up to 3 links returned
- **Whitelisted sites only**: Results filtered to trusted programming sites

### Response Format

When you send: `search python list comprehension`

The bot responds with:
```
Результаты поиска для 'python list comprehension':

1. https://docs.python.org/3/tutorial/datastructures.html#list-comprehensions
2. https://stackoverflow.com/questions/34835951/what-does-list-comprehension-mean-how-does-it-work-and-how-can-i-use-it
3. https://medium.com/@python_guide/python-list-comprehensions-explained-765fb6ca5c8a
```

### Error Cases

**Too few words:**
```
User: search python
Bot: Пожалуйста, используйте не менее 3 слов для поиска.
```

**No whitelisted results found:**
```
User: search obscure topic nobody talks about
Bot: К сожалению, не найдено ссылок с проверенных сайтов для запроса 'obscure topic nobody talks about'.
```

**Network error:**
```
User: search python programming
Bot: Произошла ошибка при поиске. Попробуйте позже.
```

## Whitelisted Sites

The bot only returns results from these trusted programming sites:

- stackoverflow.com
- github.com
- docs.python.org
- developer.mozilla.org
- w3schools.com
- medium.com
- dev.to
- geeksforgeeks.org
- tutorialspoint.com
- programiz.com

## Technical Implementation

### Configuration

The feature is configured in `config.py`:

```python
GOOGLE_SEARCH_WHITELISTED_SITES = [
    'stackoverflow.com',
    'github.com',
    # ... other trusted sites
]
GOOGLE_SEARCH_MIN_WORDS = 3
GOOGLE_SEARCH_MAX_RESULTS = 3
GOOGLE_SEARCH_TIMEOUT = 10  # seconds
```

### Pattern Matching

The command is recognized using a regex pattern in `patterns.py`:

```python
GOOGLE_SEARCH = recompile(
    r'\A\s*(search|найди|поиск|google)\s+(?P<query>[\S][\S\s]*?)\??\s*\Z', IGNORECASE)
```

### Search Process

1. **Query Validation**: Check minimum word count
2. **Google Search**: Make HTTP request to Google with proper headers
3. **Link Extraction**: Parse HTML to find result URLs
4. **Whitelist Filtering**: Keep only links from trusted domains
5. **Relevance Scoring**: Prioritize results with more matching query words
6. **Response Formatting**: Send formatted results to user

### Security Features

- **Request Limiting**: Built-in timeout protection
- **Domain Filtering**: Only whitelisted domains returned
- **Query Sanitization**: URLs properly encoded
- **Error Handling**: Graceful fallback for network issues