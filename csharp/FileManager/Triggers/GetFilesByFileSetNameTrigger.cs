using Interfaces;
using System;

namespace FileManager
{
    /// <summary>
    /// <para>
    /// Represents the get files by file set name trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{Context}"/>
    public class GetFilesByFileSetNameTrigger : ITrigger<Context>
    {
        /// <summary>
        /// <para>
        /// Determines whether this instance condition.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="arguments">
        /// <para>The arguments.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The bool</para>
        /// <para></para>
        /// </returns>
        public bool Condition(Context arguments) => arguments.Args[0].ToLower() == "getfilesbyfilessetname";

        /// <summary>
        /// <para>
        /// Actions the arguments.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="arguments">
        /// <para>The arguments.</para>
        /// <para></para>
        /// </param>
        public void Action(Context arguments)
        {
            var files = arguments.FileStorage.GetFilesFromSet(arguments.Args[1]);
            foreach (var file in files)
            {
                Console.WriteLine($"Path: {file.Path}\nContent: {file.Content}");
            }
        }
    }
}
