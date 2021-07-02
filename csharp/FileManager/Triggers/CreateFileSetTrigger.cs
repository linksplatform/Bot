using FileManager;
using Interfaces;
using Storage.Local;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileManager
{
    class CreateFileSetTrigger : ITrigger<Context>
    {
        public void Action(Context arguments)
        {
            List<IFile> files = new();
            for(int i =2; i < arguments.Args.Length-1; i+=2)
            {
                files.Add(new File() 
                {
                    Path = arguments.Args[i], 
                    Content = System.IO.File.ReadAllText(arguments.Args[i + 1])
                });
            }
            var set = arguments.FileStorage.CreateFileSet(arguments.Args[1]);
            foreach(var file in files)
            {
                arguments.FileStorage.AddFileToSet(set,arguments.FileStorage.AddFile(file.Content),file.Path);
            }
            Console.WriteLine(set);
        }

        public bool Condition(Context arguments)
        {
            return arguments.Args[0].ToLower() == "createfileset";
        }
    }
}
