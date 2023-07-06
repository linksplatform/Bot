using Interfaces;
using System;
using System.Threading.Tasks;

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
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The bool</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(Context context) => context.Args[0].ToLower() == "getfilesbyfilessetname";

        /// <summary>
        /// <para>
        /// Actions the context.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        public async Task Action(Context context)
        {
            var files = context.FileStorage.GetFilesFromSet(context.Args[1]);
            foreach (var file in files)
            {
                Console.WriteLine($"Path: {file.Path}\nContent: {file.Content}");
            }
        }
    }
}
