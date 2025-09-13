# Bug Reproduction Simplification - Usage Guide

## Overview

The Bug Reproduction Simplification feature helps developers automatically identify and extract the minimal code necessary to reproduce a bug, making it easier to understand, debug, and fix issues.

## How It Works

The system uses static analysis techniques similar to code coverage tools to:

1. **Identify Entry Points**: Finds main files, test files, and configuration files
2. **Analyze Dependencies**: Maps relationships between files using import/using statements
3. **Parse Bug Reports**: Extracts file and method references from issue descriptions
4. **Build Dependency Graph**: Creates a graph of critical code paths
5. **Remove Non-Critical Code**: Eliminates files that don't affect bug reproduction

## Usage

### 1. Create an Issue

Create a GitHub issue with a title containing one of these phrases:
- "simplify bug reproduction"
- "minimize reproduction" 
- "reduce bug example"

### 2. Issue Description

Include details about the bug in the issue description. The bot will analyze the text to identify relevant files and error patterns.

Example:
```
Title: Simplify bug reproduction for authentication failure

Description:
The AuthenticationService.cs file has a bug in the ValidateToken method that causes 
a NullReferenceException when the token is null. The bug can be reproduced by 
running the LoginTest.cs test file.

Steps to reproduce:
1. Run the application
2. Try to login with empty credentials
3. Observe the NullReferenceException in the logs

Expected: Proper error handling
Actual: Application crashes
```

### 3. Bot Processing

The bot will:
1. Analyze all repository files
2. Identify critical files for bug reproduction
3. Create a new branch with the simplified code
4. Generate a detailed report
5. Comment on the issue with results

### 4. Review Results

The bot will create a comment with:
- Summary statistics (files kept vs removed)
- List of critical files with reasons
- List of removed files with explanations
- Analysis confidence score
- Warnings about potential issues

## Example Output

```markdown
## Bug Reproduction Simplification Complete

### Summary
- **Original files**: 45
- **Simplified files**: 8
- **Removed files**: 37 (82.2%)

### Files Kept (Critical for bug reproduction)
- `src/AuthenticationService.cs` - Referenced in bug report
- `src/Models/User.cs` - Dependency of AuthenticationService.cs
- `tests/LoginTest.cs` - Test file - likely demonstrates the bug
- `Program.cs` - Entry point - main executable file
- `appsettings.json` - Configuration file - affects execution environment

### Files Removed (Non-critical)
- `README.md` - Documentation file - not needed for bug reproduction
- `docs/` - Documentation file - not needed for bug reproduction
- `examples/` - Example file - not part of core functionality

### Analysis Details
- **Entry points detected**: 2
- **Critical dependencies**: 5
- **Test files preserved**: 1

### Next Steps
1. Check the `simplified-bug-reproduction-20231215-143022` branch for the minimal reproduction case
2. Verify that the bug still reproduces with the simplified code
3. Use this simplified version for easier debugging and issue reporting
```

## Advanced Features

### File Classification

The analyzer classifies files into categories:

- **Entry Points**: Main files, test files, configuration files
- **Dependencies**: Files imported/used by entry points
- **Documentation**: README, markdown files, comments
- **Examples**: Sample code, demos, tutorials
- **Build Tools**: Makefiles, build scripts, CI configs

### Language Support

Currently supports:
- C# (.cs files)
- JavaScript (.js files) 
- TypeScript (.ts files)
- Python (.py files)
- Java (.java files)

### Confidence Scoring

The analyzer provides a confidence score (0.0 to 1.0) based on:
- Presence of entry points
- Presence of test files
- File removal rate (not too aggressive/conservative)
- References found in bug report

## Best Practices

### For Bug Reports
1. **Be Specific**: Mention specific files, methods, or classes
2. **Include Error Messages**: Copy exact error text
3. **Provide Context**: Explain what you were trying to do
4. **Add Steps**: Clear reproduction steps

### For Repository Structure
1. **Clear Entry Points**: Have obvious main files
2. **Good Test Coverage**: Include test files that demonstrate bugs
3. **Logical Structure**: Organize code in clear directories
4. **Meaningful Names**: Use descriptive file and method names

## Troubleshooting

### Low Confidence Score
- **Issue**: Confidence < 0.6
- **Solutions**: 
  - Add more specific file references to bug report
  - Ensure clear entry points exist
  - Add test files that reproduce the bug

### Too Many Files Removed
- **Issue**: > 80% of files removed
- **Solutions**:
  - Check if critical dependencies were missed
  - Verify the simplified code still reproduces the bug
  - Manual review recommended

### Too Few Files Removed  
- **Issue**: < 10% of files removed
- **Solutions**:
  - Repository may already be minimal
  - Check if analysis correctly identified entry points
  - Consider manual cleanup of documentation/examples

## Technical Details

### Algorithm Steps
1. **File Discovery**: Scan repository tree, classify files
2. **Entry Point Detection**: Find main(), test methods, config files
3. **Dependency Analysis**: Parse import/using statements with regex
4. **Bug Report Analysis**: Extract file/method references from issue text
5. **Graph Traversal**: BFS from entry points following dependencies
6. **Simplification**: Keep critical files, remove others
7. **Validation**: Calculate confidence, generate warnings

### Supported Patterns

#### C# Import Patterns
- `using System.Collections;`
- `namespace MyApp.Services`
- `class AuthService`

#### JavaScript/TypeScript
- `import React from 'react';`
- `require('./utils');`
- `export default Component;`

#### Python
- `import numpy as np`
- `from utils import helper`
- `class MyClass:`

## Limitations

1. **Static Analysis Only**: Cannot detect runtime dependencies
2. **Language Specific**: Limited to supported programming languages  
3. **Pattern Based**: May miss complex or dynamic imports
4. **No Execution**: Doesn't verify the simplified code actually works
5. **Repository Scope**: Only analyzes files in the current repository

## Future Enhancements

- Dynamic analysis support
- More programming languages
- Integration with test runners
- Automatic verification of simplified code
- Support for multi-repository dependencies
- IDE integration