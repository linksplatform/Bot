using System;
using System.IO;
using System.Text;
using Storage.Local;

namespace Platform.Bot.Services
{
    /// <summary>
    /// <para>
    /// Represents the file storage helper service.
    /// </para>
    /// <para></para>
    /// </summary>
    public static class FileStorageHelperService
    {
        private static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "bot_temp_storage");

        static FileStorageHelperService()
        {
            if (!Directory.Exists(TempDirectory))
            {
                Directory.CreateDirectory(TempDirectory);
            }
        }

        /// <summary>
        /// <para>
        /// Writes content to file with specified key.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="key">
        /// <para>The storage key.</para>
        /// <para></para>
        /// </param>
        /// <param name="content">
        /// <para>The content to write.</para>
        /// <para></para>
        /// </param>
        public static void WriteToFile(string key, string content)
        {
            try
            {
                var filePath = GetFilePath(key);
                System.IO.File.WriteAllText(filePath, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to file {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// <para>
        /// Reads content from file with specified key.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="key">
        /// <para>The storage key.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The file content</para>
        /// <para></para>
        /// </returns>
        public static string ReadFromFile(string key)
        {
            try
            {
                var filePath = GetFilePath(key);
                return System.IO.File.Exists(filePath) ? System.IO.File.ReadAllText(filePath, Encoding.UTF8) : string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading from file {key}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// <para>
        /// Checks if file exists with specified key.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="key">
        /// <para>The storage key.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>True if file exists</para>
        /// <para></para>
        /// </returns>
        public static bool FileExists(string key)
        {
            try
            {
                var filePath = GetFilePath(key);
                return System.IO.File.Exists(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking file existence {key}: {ex.Message}");
                return false;
            }
        }

        private static string GetFilePath(string key)
        {
            var safeFileName = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(TempDirectory, safeFileName + ".txt");
        }
    }
}