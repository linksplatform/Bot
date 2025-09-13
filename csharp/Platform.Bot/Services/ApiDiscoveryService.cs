using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Bot.Services
{
    public class ApiDiscoveryService
    {
        public class MethodInfo
        {
            public string Name { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
            public string Namespace { get; set; } = string.Empty;
            public List<ParameterInfo> Parameters { get; set; } = new();
            public string ReturnType { get; set; } = string.Empty;
            public bool IsStatic { get; set; }
            public bool IsPublic { get; set; }
            public string FilePath { get; set; } = string.Empty;
        }

        public class ParameterInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public bool HasDefaultValue { get; set; }
            public string? DefaultValue { get; set; }
        }

        public async Task<List<MethodInfo>> DiscoverPublicApis(string repositoryPath)
        {
            var methods = new List<MethodInfo>();
            var csFiles = Directory.GetFiles(repositoryPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            foreach (var filePath in csFiles)
            {
                try
                {
                    var code = await File.ReadAllTextAsync(filePath);
                    var tree = CSharpSyntaxTree.ParseText(code);
                    var root = await tree.GetRootAsync();

                    var classMethods = ExtractPublicMethods(root, filePath);
                    methods.AddRange(classMethods);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error analyzing file {filePath}: {ex.Message}");
                }
            }

            return methods;
        }

        private List<MethodInfo> ExtractPublicMethods(SyntaxNode root, string filePath)
        {
            var methods = new List<MethodInfo>();
            var namespaceDeclarations = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();
            
            foreach (var namespaceDecl in namespaceDeclarations)
            {
                var namespaceName = namespaceDecl.Name.ToString();
                var classDeclarations = namespaceDecl.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDecl in classDeclarations)
                {
                    if (!IsPublicClass(classDecl)) continue;

                    var className = classDecl.Identifier.ValueText;
                    var methodDeclarations = classDecl.Members.OfType<MethodDeclarationSyntax>();

                    foreach (var methodDecl in methodDeclarations)
                    {
                        if (!IsPublicMethod(methodDecl)) continue;

                        var methodInfo = new MethodInfo
                        {
                            Name = methodDecl.Identifier.ValueText,
                            ClassName = className,
                            Namespace = namespaceName,
                            ReturnType = methodDecl.ReturnType.ToString(),
                            IsStatic = methodDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
                            IsPublic = true,
                            FilePath = filePath,
                            Parameters = ExtractParameters(methodDecl)
                        };

                        methods.Add(methodInfo);
                    }
                }
            }

            return methods;
        }

        private bool IsPublicClass(ClassDeclarationSyntax classDecl)
        {
            return classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
        }

        private bool IsPublicMethod(MethodDeclarationSyntax methodDecl)
        {
            return methodDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
        }

        private List<ParameterInfo> ExtractParameters(MethodDeclarationSyntax methodDecl)
        {
            var parameters = new List<ParameterInfo>();
            
            foreach (var param in methodDecl.ParameterList.Parameters)
            {
                var paramInfo = new ParameterInfo
                {
                    Name = param.Identifier.ValueText,
                    Type = param.Type?.ToString() ?? "object",
                    HasDefaultValue = param.Default != null,
                    DefaultValue = param.Default?.Value?.ToString()
                };
                parameters.Add(paramInfo);
            }

            return parameters;
        }
    }
}