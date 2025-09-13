# API Testing Bot Example

This example demonstrates how the new API Testing Bot trigger works.

## How to trigger the bot

Create an issue with a title that contains both "test" and "bot" keywords, and a body that contains "api". For example:

**Issue Title:** "API Test Bot - Analyze public methods"

**Issue Body:** "Please analyze the public API methods in this repository and generate tests for any potential issues found."

## What the bot does

1. **Discovers Public APIs**: Uses Roslyn (C# compiler API) to analyze all C# files in the repository and extract public methods from public classes.

2. **Random Testing**: Randomly selects a subset of discovered APIs (up to 10) and simulates testing them with various parameter combinations.

3. **Issue Detection**: Identifies potential problems such as:
   - Methods that might not handle null parameters properly
   - Array parameters that could cause null/empty array issues
   - Simulates various exception scenarios (ArgumentNullException, InvalidOperationException, NotImplementedException)

4. **Test Generation**: For any issues found, generates complete unit test files using xUnit framework that:
   - Create reproducible test cases
   - Include proper arrange/act/assert patterns
   - Document the specific exception that was caught

5. **Repository Integration**: 
   - Creates generated test files in `Tests/Generated/` folder
   - Posts comprehensive results as GitHub issue comments
   - Asks maintainers whether the found behavior is expected

## Example Generated Test

```csharp
using System;
using Xunit;
using Platform.Bot.Services;

namespace Platform.Bot.Services.Tests
{
    public class ApiDiscoveryServiceTests
    {
        [Fact]
        public void TestDiscoverPublicApis_ShouldThrowException()
        {
            // This test was auto-generated because an exception was caught during random testing
            // Exception: ArgumentNullException: Simulated null argument

            // Arrange
            var instance = new ApiDiscoveryService();
            var repositoryPath = "TestString123";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                instance.DiscoverPublicApis(repositoryPath);
            });
        }
    }
}
```

## Bot Response Format

The bot provides detailed feedback in GitHub issue comments:

```markdown
## 🧪 API Testing Results

**Summary:**
- APIs Tested: 5
- Errors Found: 2
- Tests Generated: 2

**Detailed Results:**
✅ **MyClass.Method1**: Executed successfully with random parameters
❌ **MyClass.Method2**: Caught exception: ArgumentNullException
   Generated test: `Tests/Generated/MyClassMethod2Test.cs`
✅ **MyClass.Method3**: Executed successfully with random parameters

⚠️ **Found 2 potential issues!**

I've generated 2 test case(s) to reproduce the unexpected behavior.

**Question for maintainers:** Are these exceptions expected behavior? If not, these might be bugs that need fixing. If they are expected, please update the tests with proper assertions.
```

This approach helps maintain code quality by:
- Automatically discovering potential edge cases
- Creating reproducible test cases for issues
- Engaging maintainers in discussions about expected behavior
- Building up comprehensive test coverage over time