# Repository Splitting by Entities - Usage Example

This example demonstrates how to use the new repository splitting functionality in the GitHub Bot.

## Overview

The `SplitRepositoryByEntitiesTrigger` allows you to automatically split a repository into smaller repositories, with each repository containing entities (classes, interfaces, enums) from a single namespace.

## How to Use

1. **Create an Issue**: Create a new issue in your repository with the title containing "Split repository by entities"

2. **Ensure Permissions**: The issue author must have admin permissions on the repository

3. **Trigger Execution**: The bot will automatically detect the issue and begin the splitting process

## Example Issue Title

```
Split repository by entities
```

## What Happens

When the trigger executes, it will:

1. **Scan the Repository**: Analyze all `.cs` files in the repository
2. **Extract Entities**: Find all classes, interfaces, and enums
3. **Group by Namespace**: Group entities by their namespace
4. **Create Repositories**: Create a new repository for each unique namespace
5. **Populate Repositories**: Copy the relevant files to each new repository
6. **Report Results**: Post a detailed comment on the issue with the results

## Example Output

```
🚀 Starting repository splitting by entities...
📁 Analyzing repository: myorg/MyProject
📄 Found 15 C# files to analyze
🔍 Extracted 23 entities (classes, interfaces, enums)
📂 Found 4 unique namespaces
✅ Created repository: myorg/myproject-core
  📝 Added Class UserService to UserService.cs
  📝 Added Interface IUserRepository to IUserRepository.cs
✅ Created repository: myorg/myproject-data
  📝 Added Class DatabaseContext to DatabaseContext.cs
  📝 Added Class UserRepository to UserRepository.cs
✅ Created repository: myorg/myproject-models
  📝 Added Class User to User.cs
  📝 Added Enum UserStatus to UserStatus.cs
✅ Created repository: myorg/myproject-utils
  📝 Added Class StringHelper to StringHelper.cs
  📝 Added Class DateHelper to DateHelper.cs

🎉 Repository splitting completed!
📊 Summary:
  - Source repository: myorg/MyProject
  - Entities processed: 23
  - Namespaces found: 4
  - Repositories created: 4
📋 Created repositories:
  - myorg/myproject-core
  - myorg/myproject-data
  - myorg/myproject-models
  - myorg/myproject-utils
```

## Repository Naming Convention

Namespaces are converted to repository names using the following rules:
- Convert to lowercase
- Replace dots (.) with hyphens (-)
- Replace spaces and underscores with hyphens (-)
- Limit to 100 characters

### Examples:
- `MyProject.Core` → `myproject-core`
- `MyProject.Data.Repositories` → `myproject-data-repositories`
- `MyProject_Utils` → `myproject-utils`

## Security Considerations

- This trigger is wrapped with `AdminAuthorIssueTriggerDecorator` to ensure only repository administrators can execute it
- The operation creates new repositories but does not delete the source repository
- Each new repository is created with default settings (public, auto-initialized)
- File contents are copied exactly as they appear in the source repository

## Limitations

- Only processes C# files (`.cs` extension)
- Entities must be properly defined with clear class/interface/enum declarations
- Namespace extraction relies on regex patterns
- Repository creation requires appropriate GitHub API permissions
- Large repositories may take significant time to process

## Error Handling

If any errors occur during the process:
- Individual file processing errors are logged but don't stop the overall process
- Repository creation errors are reported in the status comment
- The issue remains open if fatal errors occur
- Detailed error information is provided in the status comment