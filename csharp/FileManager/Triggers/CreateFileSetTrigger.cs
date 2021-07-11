using Interfaces;
using Storage.Local;
using System;
using System.Collections.Generic;

namespace FileManager
{
    public class CreateFileSetTrigger : ITrigger<Context>
    {
        public bool Condition(Context arguments) => arguments.Args[0].ToLower() == "createfileset";

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
