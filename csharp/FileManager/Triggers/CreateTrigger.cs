using Interfaces;
using System;
using System.IO;

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
        /// <param name="arguments">
        /// <para>The arguments.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The bool</para>
        /// <para></para>
        /// </returns>
        public bool Condition(Context arguments) => arguments.Args[0].ToLower() == "create";

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
        public void Action(Context arguments) => Console.WriteLine(arguments.FileStorage.AddFile(File.ReadAllText(arguments.Args[2])));
    }
}
