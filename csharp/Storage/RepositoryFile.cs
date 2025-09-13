namespace Storage.Analysis
{
    /// <summary>
    /// <para>
    /// Represents a file in the repository with its content and metadata.
    /// </para>
    /// </summary>
    public class RepositoryFile
    {
        /// <summary>
        /// The relative path of the file in the repository.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// The content of the file.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// The file extension (e.g., ".cs", ".js", ".py").
        /// </summary>
        public string Extension => System.IO.Path.GetExtension(Path);

        /// <summary>
        /// The file name without path.
        /// </summary>
        public string FileName => System.IO.Path.GetFileName(Path);

        /// <summary>
        /// Whether this file is likely an entry point (main file, test file, etc.).
        /// </summary>
        public bool IsEntryPoint { get; set; }

        /// <summary>
        /// Whether this file is a test file.
        /// </summary>
        public bool IsTestFile { get; set; }

        /// <summary>
        /// Whether this file is a configuration file.
        /// </summary>
        public bool IsConfigFile { get; set; }

        /// <summary>
        /// Size of the file content in characters.
        /// </summary>
        public int Size => Content.Length;

        /// <summary>
        /// Programming language of the file (inferred from extension).
        /// </summary>
        public string Language => InferLanguage(Extension);

        private string InferLanguage(string extension)
        {
            return extension.ToLower() switch
            {
                ".cs" => "csharp",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".py" => "python",
                ".cpp" or ".cc" or ".cxx" => "cpp",
                ".c" => "c",
                ".h" or ".hpp" => "header",
                ".java" => "java",
                ".rs" => "rust",
                ".go" => "go",
                ".php" => "php",
                ".rb" => "ruby",
                ".json" => "json",
                ".xml" => "xml",
                ".yaml" or ".yml" => "yaml",
                ".toml" => "toml",
                ".md" => "markdown",
                ".txt" => "text",
                _ => "unknown"
            };
        }
    }
}