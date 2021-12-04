using Interfaces;
using Storage.Local;
using System;
using System.Collections.Generic;

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
        /// <param name="arguments">
        /// <para>The arguments.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The bool</para>
        /// <para></para>
        /// </returns>
        public bool Condition(Context arguments) => arguments.Args[0].ToLower() == "createfileset";

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
            List<File> files = new();
            for (var i = 2; i < arguments.Args.Length - 1; i += 2)
            {
                files.Add(new File()
                {
                    Path = arguments.Args[i],
                    Content = System.IO.File.ReadAllText(arguments.Args[i + 1])
                });
            }
            var set = arguments.FileStorage.CreateFileSet(arguments.Args[1]);
            foreach (var file in files)
            {
                arguments.FileStorage.AddFileToSet(set, arguments.FileStorage.AddFile(file.Content), file.Path);
            }
            Console.WriteLine(set);
        }
    }
}
