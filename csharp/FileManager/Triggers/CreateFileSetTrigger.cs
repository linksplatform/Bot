using FileManager;
using Interfaces;
using Storage.Local;
using System;
using System.Collections.Generic;

namespace FileManager
{
    class CreateFileSetTrigger : ITrigger<Context>
    {
        public void Action(Context arguments)
        {
            List<IFile> files = new();
            for(int i =2; i < arguments.Args.Length-1; i+=2)
            {
               files.Add(new File() { Path = arguments.Args[i], Content = System.IO.File.ReadAllText(arguments.Args[i + 1])});
            }
            arguments.FileStorage.CreateFileSet(files, arguments.Args[1]);
        }

        public bool Condition(Context arguments)
        {
            return arguments.Args[0].ToLower() == "createfileset";
        }
    }
}
