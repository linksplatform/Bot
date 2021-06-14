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
            for(int i =1; i < arguments.Args.Length-1; i+=2)
            {
               files.Add(new File() { Path = arguments.Args[i], Content = System.IO.File.ReadAllText(arguments.Args[i + 1])});
            }
            Console.WriteLine("Name of your file set is "+arguments.FileStorage.CreateFileSet(files));
        }

        public bool Condition(Context arguments)
        {
            return arguments.Args[0].ToLower() == "createfileset";
        }
    }
}
