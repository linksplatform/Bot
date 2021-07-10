using Interfaces;
using System;

namespace FileManager
{
    public class HelpTrigger : ITrigger<Context>
    {
        public bool Condition(Context arguments)
        {
            return arguments.Args[0].ToLower() == "help";
        }

        public void Action(Context arguments)
        {
            Console.WriteLine(@"Use this program to manage links in your links repository. For close just press CTRL+C. 
                Avalible commands:
                1. Delete [address]
                2. Create [address] [path to file]
                3. Help
                4. Print
                5. Show [file number]
                6. CreateFileSet [File set name] {[Path to file in remote storage] [path to file in local storage]}
                7. GetFilesByFilesSetName [File set name]");
        }
    }
}
