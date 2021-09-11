using Interfaces;
using System;

namespace FileManager
{
    /// <summary>
    /// <para>
    /// Represents the show trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{Context}"/>
    public class ShowTrigger : ITrigger<Context>
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
        public bool Condition(Context arguments) => arguments.Args[0].ToLower() == "show";

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
            if (arguments.Args[1] == "allFiles")
            {
                foreach (var file in arguments.FileStorage.GetAllFiles())
                {
                    if (file.Content.Length < 50)
                    {
                        Console.WriteLine($"{file.Path}: {file.Content} (Hash: {file.Content.GetHashCode()})");
                    }
                    else
                    {
                        Console.WriteLine($"{file.Path}: {file.Content.Substring(0, 50)} ... {file.Content.Substring(file.Content.Length - 50, 50)} (Hash: {file.Content.GetHashCode()})");
                    }
                }
            }
            else
            {
                Console.WriteLine(arguments.FileStorage.GetFileContent(ulong.Parse(arguments.Args[1])));
            }
        }
    }
}
