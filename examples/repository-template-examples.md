# Repository Template Examples

This document demonstrates how to use the GitHub bot's repository template creation feature.

## Available Template Types

The bot supports creating repositories from the following templates:

1. **Hello World Template** - Basic "Hello World" project
2. **Console Application Template** - Console application structure  
3. **Library Template** - Automatically published library with CI/CD

## Usage

To create a repository from a template, create an issue with one of the following titles and specify the repository name in the issue body:

### Hello World Template

**Issue Title:** `Hello World Template`

**Issue Body:**
```
I need a new hello world project.

repository: my-hello-world-project
```

### Console Application Template  

**Issue Title:** `Console App Template`

**Issue Body:**
```
Please create a new console application.

repository: my-console-app
```

### Library Template

**Issue Title:** `Library Template`

**Issue Body:**
```
Create a new library with automatic publishing.

repository: my-awesome-library
```

## Expected Templates

The bot will look for these template repositories in the `linksplatform` organization:

- `linksplatform/Template.HelloWorld`
- `linksplatform/Template.ConsoleApp`  
- `linksplatform/Template.Library`

## Bot Response

After processing the request, the bot will:

1. Create a new repository from the specified template
2. Comment on the issue with the new repository URL
3. Close the issue automatically

If there are any errors (e.g., invalid repository name or template not found), the bot will comment with error details and keep the issue open.