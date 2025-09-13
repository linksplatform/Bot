using Platform.Bot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Platform.Bot.Services
{
    public class TestGeneratorService
    {
        private readonly System.Random _random = new();

        public string GenerateUnitTest(ApiDiscoveryService.MethodInfo method, Exception? caughtException = null)
        {
            var testClass = $"{method.ClassName}Tests";
            var testMethodName = $"Test{method.Name}_Should{(caughtException != null ? "ThrowException" : "ExecuteSuccessfully")}";
            
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using Xunit;");
            sb.AppendLine($"using {method.Namespace};");
            sb.AppendLine();
            sb.AppendLine($"namespace {method.Namespace}.Tests");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {testClass}");
            sb.AppendLine("    {");
            sb.AppendLine($"        [Fact]");
            sb.AppendLine($"        public void {testMethodName}()");
            sb.AppendLine("        {");
            
            if (caughtException != null)
            {
                sb.AppendLine("            // This test was auto-generated because an exception was caught during random testing");
                sb.AppendLine($"            // Exception: {caughtException.GetType().Name}: {caughtException.Message}");
                sb.AppendLine();
            }

            // Generate test setup
            if (!method.IsStatic)
            {
                sb.AppendLine($"            // Arrange");
                sb.AppendLine($"            var instance = new {method.ClassName}();");
            }

            // Generate parameters
            var parameterSetup = GenerateParameterSetup(method.Parameters);
            if (!string.IsNullOrEmpty(parameterSetup))
            {
                if (method.IsStatic) sb.AppendLine($"            // Arrange");
                sb.AppendLine(parameterSetup);
            }

            sb.AppendLine();
            sb.AppendLine("            // Act & Assert");

            if (caughtException != null)
            {
                sb.AppendLine($"            Assert.Throws<{caughtException.GetType().Name}>(() =>");
                sb.AppendLine("            {");
                sb.Append("                ");
            }
            else
            {
                if (method.ReturnType != "void")
                {
                    sb.Append("            var result = ");
                }
                else
                {
                    sb.Append("            ");
                }
            }

            // Generate method call
            if (method.IsStatic)
            {
                sb.Append($"{method.ClassName}.{method.Name}(");
            }
            else
            {
                sb.Append($"instance.{method.Name}(");
            }

            var paramCalls = method.Parameters.Select(p => p.Name).ToArray();
            sb.Append(string.Join(", ", paramCalls));
            sb.Append(");");

            if (caughtException != null)
            {
                sb.AppendLine();
                sb.AppendLine("            });");
            }
            else
            {
                sb.AppendLine();
                if (method.ReturnType != "void")
                {
                    sb.AppendLine();
                    sb.AppendLine("            // Add specific assertions based on expected behavior");
                    sb.AppendLine("            Assert.NotNull(result);");
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateParameterSetup(List<ApiDiscoveryService.ParameterInfo> parameters)
        {
            if (parameters.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            foreach (var param in parameters)
            {
                var value = GenerateTestValue(param.Type);
                sb.AppendLine($"            var {param.Name} = {value};");
            }
            return sb.ToString().TrimEnd();
        }

        public object GenerateTestValue(string typeName)
        {
            return typeName.ToLower() switch
            {
                "int" or "system.int32" => _random.Next(0, 1000),
                "long" or "system.int64" => _random.NextInt64(0, 1000),
                "double" or "system.double" => _random.NextDouble() * 1000,
                "float" or "system.single" => (float)(_random.NextDouble() * 1000),
                "string" or "system.string" => $"\"TestString{_random.Next(1, 1000)}\"",
                "bool" or "system.boolean" => _random.Next(0, 2) == 1 ? "true" : "false",
                "datetime" or "system.datetime" => "DateTime.Now",
                "guid" or "system.guid" => "Guid.NewGuid()",
                _ when typeName.Contains("[]") => "new " + typeName.Replace("[]", "[0]"),
                _ when typeName.Contains("List") => $"new {typeName}()",
                _ when typeName.Contains("Dictionary") => $"new {typeName}()",
                _ => "null"
            };
        }

        public List<object> GenerateRandomParameters(List<ApiDiscoveryService.ParameterInfo> parameters)
        {
            var values = new List<object>();
            foreach (var param in parameters)
            {
                if (param.HasDefaultValue && _random.Next(0, 3) == 0)
                {
                    // Sometimes use default value
                    values.Add(Type.Missing);
                }
                else
                {
                    values.Add(GenerateTestValue(param.Type));
                }
            }
            return values;
        }
    }
}