using System;
using System.IO;
using Xunit;
using Storage.Local;
using Storage.AST;

namespace Storage.Tests
{
    public class AstTransformationTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly FileStorage _storage;

        public AstTransformationTests()
        {
            _testDbPath = Path.GetTempFileName();
            _storage = new FileStorage(_testDbPath);
        }

        [Fact]
        public void TransformCodeToAst_WithSimpleCode_ShouldCreateAstInLinksStore()
        {
            // Arrange
            var code = @"using System;
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            Console.WriteLine(""Hello World"");
        }
    }
}";

            // Act
            var rootAstNodeLink = _storage.TransformCodeToAst(code);

            // Assert
            Assert.NotEqual(0UL, rootAstNodeLink);
            
            var astNodes = _storage.GetAllAstNodes();
            Assert.NotEmpty(astNodes);
            
            var rootNodeInfo = _storage.GetAstNodeInfo(rootAstNodeLink);
            Assert.Equal("CompilationUnit", rootNodeInfo["NodeType"]);
            Assert.False(rootNodeInfo.ContainsKey("Error"));
        }

        [Fact]
        public void CSharpAstTransformer_WithSimpleCode_ShouldMapNodesToExactPositions()
        {
            // Arrange
            var transformer = new CSharpAstTransformer();
            var code = "using System;\nnamespace Test {}";

            // Act
            var rootNode = transformer.TransformCode(code);

            // Assert
            Assert.Equal("CompilationUnit", rootNode.NodeType);
            Assert.Equal(0, rootNode.StartPosition);
            Assert.Equal(code.Length - 1, rootNode.EndPosition);
            Assert.Equal(1, rootNode.StartLine);
            Assert.Equal(2, rootNode.EndLine);
            
            // Check that children are created
            Assert.NotEmpty(rootNode.Children);
            
            // Verify all nodes have position information
            var allNodes = transformer.GetAllNodesWithPositions(rootNode);
            foreach (var node in allNodes)
            {
                Assert.True(node.StartPosition >= 0);
                Assert.True(node.EndPosition >= node.StartPosition);
                Assert.True(node.StartLine >= 1);
                Assert.True(node.StartColumn >= 0);
            }
        }

        [Fact]
        public void AstNode_WithChildNodes_ShouldMaintainParentChildRelationships()
        {
            // Arrange
            var parent = new AstNode { NodeType = "Parent", Text = "parent content" };
            var child1 = new AstNode { NodeType = "Child1", Text = "child1 content" };
            var child2 = new AstNode { NodeType = "Child2", Text = "child2 content" };

            // Act
            parent.AddChild(child1);
            parent.AddChild(child2);

            // Assert
            Assert.Equal(2, parent.Children.Count);
            Assert.Equal(parent, child1.Parent);
            Assert.Equal(parent, child2.Parent);
            Assert.Equal(0, parent.Depth);
            Assert.Equal(1, child1.Depth);
            Assert.Equal(1, child2.Depth);
        }

        [Fact]
        public void TransformCodeToAst_WithEmptyCode_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _storage.TransformCodeToAst(""));
            Assert.Throws<ArgumentException>(() => _storage.TransformCodeToAst(null));
        }

        [Fact]
        public void GetAstNodeInfo_WithValidAstNode_ShouldReturnNodeInformation()
        {
            // Arrange
            var code = "public class TestClass {}";
            var rootAstNodeLink = _storage.TransformCodeToAst(code);

            // Act
            var nodeInfo = _storage.GetAstNodeInfo(rootAstNodeLink);

            // Assert
            Assert.Contains("NodeType", nodeInfo.Keys);
            Assert.Contains("Text", nodeInfo.Keys);
            Assert.Contains("LinkAddress", nodeInfo.Keys);
            Assert.Contains("HasPositionInfo", nodeInfo.Keys);
            Assert.False(nodeInfo.ContainsKey("Error"));
        }

        [Fact]
        public void GetAllAstNodes_AfterTransformation_ShouldReturnStoredNodes()
        {
            // Arrange
            var code1 = "public class Class1 {}";
            var code2 = "public interface ITest {}";

            // Act
            _storage.TransformCodeToAst(code1);
            _storage.TransformCodeToAst(code2);
            var allAstNodes = _storage.GetAllAstNodes();

            // Assert
            Assert.True(allAstNodes.Count >= 2); // At least 2 root nodes
        }

        public void Dispose()
        {
            _storage?.Dispose();
            if (System.IO.File.Exists(_testDbPath))
            {
                System.IO.File.Delete(_testDbPath);
            }
        }
    }
}