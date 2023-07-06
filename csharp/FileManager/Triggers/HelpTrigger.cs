using Interfaces;
using System;
using System.Threading.Tasks;

namespace FileManager
{
    /// <summary>
    /// <para>
    /// Represents the help trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{Context}"/>
    public class HelpTrigger : ITrigger<Context>
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
        public async Task<bool> Condition(Context context) => context.Args[0].ToLower() == "help";

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
        public async Task Action(Context context) => Console.WriteLine(@"Use this program to manage links in your links repository. For close just press CTRL+C. 
Avalible commands:
1. Delete [address]
2. Create [address] [path to file]
3. Help
4. Print 
5. Show [file number]
6. CreateFileSet [File set name] {[Path to file in remote storage] [path to file in local storage]}
7. GetFilesByFilesSetName [File set name]");
    }
}
