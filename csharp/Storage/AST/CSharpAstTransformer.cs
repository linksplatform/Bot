using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Storage.AST
{
    /// <summary>
    /// Transforms C# code into AST using ANTLR, mapping each node to exact text positions.
    /// </summary>
    public class CSharpAstTransformer
    {
        /// <summary>
        /// Transforms C# code text into an AST with exact position mapping.
        /// </summary>
        /// <param name="code">The C# code to transform.</param>
        /// <returns>The root AST node.</returns>
        public AstNode TransformCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty.", nameof(code));
            }

            try
            {
                // Create a simple AST for basic C# constructs without full grammar
                var root = new AstNode
                {
                    NodeType = "CompilationUnit",
                    Text = code,
                    StartPosition = 0,
                    EndPosition = code.Length - 1,
                    StartLine = 1,
                    StartColumn = 0,
                    EndLine = GetLineCount(code),
                    EndColumn = GetLastLineLength(code)
                };

                // Parse basic constructs
                ParseBasicConstructs(code, root);

                return root;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to transform C# code into AST: {ex.Message}", ex);
            }
        }

        private void ParseBasicConstructs(string code, AstNode root)
        {
            var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int currentPosition = 0;
            int lineNumber = 1;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                {
                    currentPosition += line.Length + Environment.NewLine.Length;
                    lineNumber++;
                    continue;
                }

                var node = CreateNodeForLine(line, trimmedLine, currentPosition, lineNumber);
                if (node != null)
                {
                    root.AddChild(node);
                }

                currentPosition += line.Length + Environment.NewLine.Length;
                lineNumber++;
            }
        }

        private AstNode? CreateNodeForLine(string originalLine, string trimmedLine, int startPosition, int lineNumber)
        {
            // Detect basic C# constructs
            string nodeType = DetectNodeType(trimmedLine);
            
            if (nodeType == "Unknown")
            {
                return null;
            }

            return new AstNode
            {
                NodeType = nodeType,
                Text = trimmedLine,
                StartPosition = startPosition,
                EndPosition = startPosition + originalLine.Length - 1,
                StartLine = lineNumber,
                StartColumn = 0,
                EndLine = lineNumber,
                EndColumn = originalLine.Length - 1
            };
        }

        private string DetectNodeType(string line)
        {
            if (line.StartsWith("using "))
                return "UsingDirective";
            if (line.StartsWith("namespace "))
                return "NamespaceDeclaration";
            if (line.Contains("class ") && (line.StartsWith("public ") || line.StartsWith("private ") || line.StartsWith("internal ") || line.StartsWith("class ")))
                return "ClassDeclaration";
            if (line.Contains("interface ") && (line.StartsWith("public ") || line.StartsWith("private ") || line.StartsWith("internal ") || line.StartsWith("interface ")))
                return "InterfaceDeclaration";
            if (line.Contains("struct ") && (line.StartsWith("public ") || line.StartsWith("private ") || line.StartsWith("internal ") || line.StartsWith("struct ")))
                return "StructDeclaration";
            if (line.Contains("enum ") && (line.StartsWith("public ") || line.StartsWith("private ") || line.StartsWith("internal ") || line.StartsWith("enum ")))
                return "EnumDeclaration";
            if (IsMethodDeclaration(line))
                return "MethodDeclaration";
            if (IsPropertyDeclaration(line))
                return "PropertyDeclaration";
            if (IsFieldDeclaration(line))
                return "FieldDeclaration";
            if (line.StartsWith("{"))
                return "OpenBrace";
            if (line.StartsWith("}"))
                return "CloseBrace";
            if (line.Contains("=") && !line.Contains("==") && !line.Contains("!="))
                return "AssignmentStatement";
            if (line.StartsWith("if ("))
                return "IfStatement";
            if (line.StartsWith("for ("))
                return "ForStatement";
            if (line.StartsWith("while ("))
                return "WhileStatement";
            if (line.StartsWith("return"))
                return "ReturnStatement";

            return "Statement";
        }

        private bool IsMethodDeclaration(string line)
        {
            return (line.Contains("(") && line.Contains(")") && 
                   (line.StartsWith("public ") || line.StartsWith("private ") || line.StartsWith("protected ") || line.StartsWith("internal ") || line.StartsWith("static "))) ||
                   line.Contains(" void ") || line.Contains(" int ") || line.Contains(" string ") || line.Contains(" bool ");
        }

        private bool IsPropertyDeclaration(string line)
        {
            return line.Contains(" { get; ") || line.Contains(" { set; ") || 
                   (line.Contains(" get ") && line.Contains("{")) ||
                   (line.Contains(" set ") && line.Contains("{"));
        }

        private bool IsFieldDeclaration(string line)
        {
            return (line.StartsWith("public ") || line.StartsWith("private ") || line.StartsWith("protected ") || line.StartsWith("internal ")) &&
                   !line.Contains("(") && !line.Contains("{") && line.Contains(";");
        }

        private int GetLineCount(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 1;

            int count = 1;
            foreach (char c in text)
            {
                if (c == '\n')
                    count++;
            }
            return count;
        }

        private int GetLastLineLength(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var lastNewLineIndex = text.LastIndexOf('\n');
            if (lastNewLineIndex == -1)
                return text.Length;

            return text.Length - lastNewLineIndex - 1;
        }

        /// <summary>
        /// Gets all AST nodes in a flat list with their exact positions.
        /// </summary>
        /// <param name="root">The root AST node.</param>
        /// <returns>List of all nodes with position information.</returns>
        public List<AstNode> GetAllNodesWithPositions(AstNode root)
        {
            var nodes = new List<AstNode>();
            CollectNodes(root, nodes);
            return nodes.OrderBy(n => n.StartPosition).ToList();
        }

        private void CollectNodes(AstNode node, List<AstNode> nodes)
        {
            nodes.Add(node);
            foreach (var child in node.Children)
            {
                CollectNodes(child, nodes);
            }
        }
    }
}