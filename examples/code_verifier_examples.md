# Code Verifier Examples

This document provides examples of how to use the new code verifier functionality in the bot.

## Basic Usage

The code verifier allows users to execute Python code directly in the bot chat and see the results.

### Command Patterns

1. **Execute Code**: `verify <code>`, `проверить <code>`, `выполнить <code>`
2. **Reset Environment**: `reset code`, `сбросить код`, `reset env`, `сбросить среду`

### Example 1: Simple Code Execution

**User input:**
```
verify print("Hello, World!")
```

**Bot response:**
```
✅ Код выполнен успешно:

Hello, World!
```

### Example 2: Variables and Calculations

**User input:**
```
verify 
x = 10
y = 20
print(f"Sum: {x + y}")
print(f"Product: {x * y}")
```

**Bot response:**
```
✅ Код выполнен успешно:

Sum: 30
Product: 200
```

### Example 3: Error Handling

**User input:**
```
verify print(undefined_variable)
```

**Bot response:**
```
❌ Ошибка выполнения кода:

Traceback (most recent call last):
  File "<string>", line 1, in <module>
NameError: name 'undefined_variable' is not defined
```

### Example 4: Environment Persistence

**First execution:**
```
verify data = {"name": "John", "age": 30}
```

**Bot response:**
```
✅ Код выполнен успешно (no output)
```

**Second execution:**
```
verify print(f"Name: {data['name']}, Age: {data['age']}")
```

**Bot response:**
```
✅ Код выполнен успешно:

Name: John, Age: 30
```

### Example 5: Reset Environment

**User input:**
```
reset code
```

**Bot response:**
```
🔄 Среда выполнения кода сброшена.
```

Now trying to access the previous variable:

**User input:**
```
verify print(data)
```

**Bot response:**
```
❌ Ошибка выполнения кода:

Traceback (most recent call last):
  File "<string>", line 1, in <module>
NameError: name 'data' is not defined
```

## Security Features

The code verifier includes several security measures:

1. **Isolated Environment**: Each execution runs in a controlled environment with limited globals
2. **Error Handling**: All exceptions are caught and reported safely
3. **Output Capture**: Both stdout and stderr are captured to prevent unwanted side effects
4. **Environment Reset**: Users can reset their execution environment to start fresh

## Integration with AI Assistants

This feature is particularly useful when combined with AI code generation tools:

1. Generate code using GitHub Copilot command
2. Verify the generated code using the verify command
3. If there are errors, the AI can analyze them and provide fixes
4. Iteratively improve the code until it works correctly

This implements the workflow described in the referenced Habr article about creating a code-generating AI assistant with verification capabilities.