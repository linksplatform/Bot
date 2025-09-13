using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;
using Storage.Remote.GitHub;

namespace Platform.Bot.Services
{
    /// <summary>
    /// <para>
    /// Represents the code duplication analysis service.
    /// </para>
    /// <para></para>
    /// </summary>
    public class CodeDuplicationAnalysisService
    {
        private readonly GitHubStorage _storage;
        private const int MinimumCodeFragmentLength = 3;
        private const int MinimumSimilarityThreshold = 80;

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="CodeDuplicationAnalysisService"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>A git hub storage.</para>
        /// <para></para>
        /// </param>
        public CodeDuplicationAnalysisService(GitHubStorage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// <para>
        /// Represents code fragment information.
        /// </para>
        /// <para></para>
        /// </summary>
        public class CodeFragment
        {
            public string Content { get; set; }
            public string FilePath { get; set; }
            public int StartLine { get; set; }
            public int EndLine { get; set; }
            public string Hash { get; set; }

            public CodeFragment(string content, string filePath, int startLine, int endLine)
            {
                Content = content;
                FilePath = filePath;
                StartLine = startLine;
                EndLine = endLine;
                Hash = ComputeHash(content);
            }

            private static string ComputeHash(string content)
            {
                var normalized = NormalizeCode(content);
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                return Convert.ToBase64String(hash);
            }

            private static string NormalizeCode(string code)
            {
                code = Regex.Replace(code, @"\s+", " ");
                code = Regex.Replace(code, @"//.*", "");
                code = Regex.Replace(code, @"/\*.*?\*/", "", RegexOptions.Singleline);
                return code.Trim();
            }
        }

        /// <summary>
        /// <para>
        /// Represents duplication group information.
        /// </para>
        /// <para></para>
        /// </summary>
        public class DuplicationGroup
        {
            public List<CodeFragment> Fragments { get; set; } = new();
            public int Count => Fragments.Count;
            public double SimilarityScore { get; set; }
            public string SuggestedMethodName { get; set; } = string.Empty;
        }

        /// <summary>
        /// <para>
        /// Analyzes repository for code duplications.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="repository">
        /// <para>The repository.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The list of duplication groups</para>
        /// <para></para>
        /// </returns>
        public async Task<List<DuplicationGroup>> AnalyzeRepositoryAsync(Repository repository)
        {
            var codeFragments = await ExtractCodeFragmentsAsync(repository);
            var duplications = FindDuplications(codeFragments);
            return duplications;
        }

        private async Task<List<CodeFragment>> ExtractCodeFragmentsAsync(Repository repository)
        {
            var fragments = new List<CodeFragment>();
            var contents = await GetRepositoryContentsAsync(repository);

            foreach (var content in contents)
            {
                if (IsCodeFile(content.Name))
                {
                    var fileContent = await GetFileContentAsync(repository, content.Path);
                    var fileFragments = ExtractFragmentsFromFile(fileContent, content.Path);
                    fragments.AddRange(fileFragments);
                }
            }

            return fragments;
        }

        private async Task<IReadOnlyList<RepositoryContent>> GetRepositoryContentsAsync(Repository repository)
        {
            try
            {
                return await GetAllContentsRecursively(repository, "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting repository contents: {ex.Message}");
                return new List<RepositoryContent>();
            }
        }

        private async Task<List<RepositoryContent>> GetAllContentsRecursively(Repository repository, string path)
        {
            var allContents = new List<RepositoryContent>();
            
            try
            {
                var contents = await _storage.Client.Repository.Content.GetAllContents(repository.Id, path);
                
                foreach (var content in contents)
                {
                    if (content.Type == ContentType.File)
                    {
                        allContents.Add(content);
                    }
                    else if (content.Type == ContentType.Dir)
                    {
                        var subContents = await GetAllContentsRecursively(repository, content.Path);
                        allContents.AddRange(subContents);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting contents for path {path}: {ex.Message}");
            }

            return allContents;
        }

        private async Task<string> GetFileContentAsync(Repository repository, string path)
        {
            try
            {
                var contents = await _storage.Client.Repository.Content.GetAllContents(repository.Id, path);
                return contents.First().Content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting file content for {path}: {ex.Message}");
                return string.Empty;
            }
        }

        private static bool IsCodeFile(string fileName)
        {
            var codeExtensions = new[] { ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h", ".php", ".rb", ".go", ".rs", ".swift" };
            return codeExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        private List<CodeFragment> ExtractFragmentsFromFile(string content, string filePath)
        {
            var fragments = new List<CodeFragment>();
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length - MinimumCodeFragmentLength + 1; i++)
            {
                for (int length = MinimumCodeFragmentLength; length <= Math.Min(10, lines.Length - i); length++)
                {
                    var fragmentLines = lines.Skip(i).Take(length).ToArray();
                    var fragmentContent = string.Join("\n", fragmentLines);
                    
                    if (IsValidCodeFragment(fragmentContent))
                    {
                        fragments.Add(new CodeFragment(fragmentContent, filePath, i + 1, i + length));
                    }
                }
            }

            return fragments;
        }

        private static bool IsValidCodeFragment(string content)
        {
            content = content.Trim();
            if (string.IsNullOrWhiteSpace(content)) return false;
            if (content.Length < 50) return false;
            
            var lines = content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (lines.Length < MinimumCodeFragmentLength) return false;
            
            var codeLineCount = lines.Count(line => 
                !line.Trim().StartsWith("//") && 
                !line.Trim().StartsWith("/*") && 
                !line.Trim().StartsWith("*") &&
                line.Trim() != "{" &&
                line.Trim() != "}");
                
            return codeLineCount >= MinimumCodeFragmentLength;
        }

        private List<DuplicationGroup> FindDuplications(List<CodeFragment> fragments)
        {
            var groups = new Dictionary<string, DuplicationGroup>();

            foreach (var fragment in fragments)
            {
                if (groups.ContainsKey(fragment.Hash))
                {
                    groups[fragment.Hash].Fragments.Add(fragment);
                }
                else
                {
                    groups[fragment.Hash] = new DuplicationGroup
                    {
                        Fragments = new List<CodeFragment> { fragment },
                        SimilarityScore = 100.0
                    };
                }
            }

            var duplications = groups.Values
                .Where(g => g.Count > 1)
                .OrderByDescending(g => g.Count)
                .ThenByDescending(g => g.Fragments.First().Content.Length)
                .ToList();

            foreach (var group in duplications)
            {
                group.SuggestedMethodName = GenerateMethodName(group.Fragments.First().Content);
            }

            return duplications;
        }

        private static string GenerateMethodName(string content)
        {
            var words = new List<string>();
            var normalizedContent = content.ToLowerInvariant();
            
            var keywords = new[] { "get", "set", "create", "update", "delete", "find", "search", "calculate", "process", "validate", "convert" };
            var foundKeyword = keywords.FirstOrDefault(k => normalizedContent.Contains(k));
            
            if (!string.IsNullOrEmpty(foundKeyword))
            {
                words.Add(char.ToUpper(foundKeyword[0]) + foundKeyword[1..]);
            }
            else
            {
                words.Add("Process");
            }

            var identifierMatches = Regex.Matches(content, @"\b[A-Z][a-z]+\b");
            foreach (Match match in identifierMatches.Take(2))
            {
                if (!words.Contains(match.Value))
                {
                    words.Add(match.Value);
                }
            }

            if (words.Count == 1)
            {
                words.Add("Data");
            }

            return string.Join("", words);
        }
    }
}