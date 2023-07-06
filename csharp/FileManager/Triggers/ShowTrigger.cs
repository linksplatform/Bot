using Interfaces;
using System;
using System.Threading.Tasks;

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
        /// <param name="context">
        /// <para>The context.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The bool</para>
        /// <para></para>
        /// </returns>
        public async Task<bool> Condition(Context context) => context.Args[0].ToLower() == "show";

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
            if (context.Args[1] == "allFiles")
            {
                foreach (var file in context.FileStorage.GetAllFiles())
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
                Console.WriteLine(context.FileStorage.GetFileContent(ulong.Parse(context.Args[1])));
            }
        }
    }
}
