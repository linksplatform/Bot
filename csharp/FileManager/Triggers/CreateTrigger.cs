using Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FileManager
{
    /// <summary>
    /// <para>
    /// Represents the create trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{Context}"/>
    public class CreateTrigger : ITrigger<Context>
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
        public async Task<bool> Condition(Context context) => context.Args[0].ToLower() == "create";

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
        public async Task Action(Context context) => Console.WriteLine(context.FileStorage.AddFile(File.ReadAllText(context.Args[2])));
    }
}
