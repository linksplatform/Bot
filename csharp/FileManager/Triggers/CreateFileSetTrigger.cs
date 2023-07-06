using Interfaces;
using Storage.Local;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileManager
{
    /// <summary>
    /// <para>
    /// Represents the create file set trigger.
    /// </para>
    /// <para></para>
    /// </summary>
    /// <seealso cref="ITrigger{Context}"/>
    public class CreateFileSetTrigger : ITrigger<Context>
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
        public async Task<bool> Condition(Context context) => context.Args[0].ToLower() == "createfileset";

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
            List<File> files = new();
            for (var i = 2; i < context.Args.Length - 1; i += 2)
            {
                files.Add(new File()
                {
                    Path = context.Args[i],
                    Content = System.IO.File.ReadAllText(context.Args[i + 1])
                });
            }
            var set = context.FileStorage.CreateFileSet(context.Args[1]);
            foreach (var file in files)
            {
                context.FileStorage.AddFileToSet(set, context.FileStorage.AddFile(file.Content), file.Path);
            }
            Console.WriteLine(set);
        }
    }
}
