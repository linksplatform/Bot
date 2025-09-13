# AST (Abstract Syntax Tree) Integration with Links Store

This module implements ANTLR-based AST transformation functionality that maps code into an Abstract Syntax Tree and stores it in the Links Platform data store, with each AST node mapped to its exact position in the source code text.

## Features

- **Exact Position Mapping**: Each AST node is mapped to its exact position in the source code (line, column, start/end positions)
- **Links Store Integration**: AST nodes are stored in the Platform.Data.Doublets links store
- **Hierarchical Structure**: Parent-child relationships between AST nodes are preserved
- **C# Code Support**: Basic C# language constructs are recognized and parsed

## Classes

### AstNode
Represents an AST node with complete position information:
- `NodeType`: The type of AST node (e.g., "CompilationUnit", "ClassDeclaration")
- `Text`: The source text content for this node
- `StartPosition`/`EndPosition`: Character positions in source text
- `StartLine`/`EndLine`: Line numbers (1-based)
- `StartColumn`/`EndColumn`: Column positions (0-based)
- `Parent`/`Children`: Tree structure relationships

### CSharpAstTransformer
Transforms C# source code into AST nodes:
- `TransformCode(string code)`: Main transformation method
- `GetAllNodesWithPositions(AstNode root)`: Flattens tree into position-ordered list

### FileStorage Extensions
New methods added to FileStorage for AST functionality:
- `TransformCodeToAst(string code)`: Transform and store AST in links
- `GetAllAstNodes()`: Retrieve all stored AST nodes
- `GetAstNodeInfo(TLinkAddress link)`: Get information about a specific AST node

## Usage Example

```csharp
using var storage = new FileStorage("ast_data.db");

var code = @"using System;
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}";

// Transform code into AST and store in links
var rootNodeLink = storage.TransformCodeToAst(code);

// Retrieve all AST nodes
var allNodes = storage.GetAllAstNodes();

// Get detailed information about each node
foreach (var nodeLink in allNodes)
{
    var info = storage.GetAstNodeInfo(nodeLink);
    Console.WriteLine($"{info["NodeType"]}: {info["Text"]}");
}
```

## Links Store Structure

AST nodes are stored in the links database with the following structure:
- AST_NODE_MARKER -> (NODE_TYPE -> (TEXT -> POSITION))
- Position information includes: StartPos, EndPos, StartLine, StartCol, EndLine, EndCol
- Child relationships are stored as additional links

## Testing

The implementation includes comprehensive tests:
- Basic AST transformation and storage
- Position mapping accuracy
- Parent-child relationships
- Error handling
- Integration with links store

Run tests with:
```bash
dotnet test csharp/Storage.Tests/
```

## Requirements

- .NET 8
- Antlr4.Runtime.Standard package
- Platform.Data.Doublets for links store functionality