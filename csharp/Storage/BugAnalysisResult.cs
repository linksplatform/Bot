using System.Collections.Generic;

namespace Storage.Analysis
{
    /// <summary>
    /// <para>
    /// Represents the result of bug reproduction analysis.
    /// </para>
    /// <para>Contains information about which files are critical for bug reproduction and which can be removed.</para>
    /// </summary>
    public class BugAnalysisResult
    {
        /// <summary>
        /// Files that are critical for reproducing the bug.
        /// </summary>
        public HashSet<string> CriticalFiles { get; set; } = new HashSet<string>();

        /// <summary>
        /// Files that can be safely removed without affecting bug reproduction.
        /// </summary>
        public HashSet<string> RemovedFiles { get; set; } = new HashSet<string>();

        /// <summary>
        /// Entry point files (main files, test files that demonstrate the bug).
        /// </summary>
        public HashSet<string> EntryPoints { get; set; } = new HashSet<string>();

        /// <summary>
        /// Critical dependency relationships between files.
        /// </summary>
        public Dictionary<string, HashSet<string>> CriticalDependencies { get; set; } = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Test files that should be preserved to demonstrate the bug.
        /// </summary>
        public HashSet<string> TestFiles { get; set; } = new HashSet<string>();

        /// <summary>
        /// Configuration files that affect the bug reproduction.
        /// </summary>
        public HashSet<string> ConfigFiles { get; set; } = new HashSet<string>();

        /// <summary>
        /// Reasons why specific files are considered critical.
        /// </summary>
        public Dictionary<string, string> FileReasons { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Reasons why specific files were removed.
        /// </summary>
        public Dictionary<string, string> RemovalReasons { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Confidence score for the analysis (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Warning messages about potential issues with the simplification.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}