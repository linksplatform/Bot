using System.Collections.Generic;

namespace Storage.AST
{
    /// <summary>
    /// Represents an AST node with exact text position mapping.
    /// </summary>
    public class AstNode
    {
        /// <summary>
        /// Gets or sets the node type (e.g., "CompilationUnit", "MethodDeclaration", etc.).
        /// </summary>
        public string NodeType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the text content of this node.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the start position in the source text.
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// Gets or sets the end position in the source text.
        /// </summary>
        public int EndPosition { get; set; }

        /// <summary>
        /// Gets or sets the start line number (1-based).
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// Gets or sets the start column number (0-based).
        /// </summary>
        public int StartColumn { get; set; }

        /// <summary>
        /// Gets or sets the end line number (1-based).
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Gets or sets the end column number (0-based).
        /// </summary>
        public int EndColumn { get; set; }

        /// <summary>
        /// Gets or sets the parent node.
        /// </summary>
        public AstNode? Parent { get; set; }

        /// <summary>
        /// Gets the child nodes.
        /// </summary>
        public List<AstNode> Children { get; set; } = new List<AstNode>();

        /// <summary>
        /// Gets or sets additional properties for this node.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Adds a child node to this node.
        /// </summary>
        /// <param name="child">The child node to add.</param>
        public void AddChild(AstNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Gets the depth of this node in the AST tree.
        /// </summary>
        public int Depth
        {
            get
            {
                int depth = 0;
                var current = Parent;
                while (current != null)
                {
                    depth++;
                    current = current.Parent;
                }
                return depth;
            }
        }

        /// <summary>
        /// Returns a string representation of this AST node.
        /// </summary>
        public override string ToString()
        {
            return $"{NodeType} [{StartLine}:{StartColumn}-{EndLine}:{EndColumn}] \"{Text.Replace("\n", "\\n").Replace("\r", "\\r")}\"";
        }
    }
}