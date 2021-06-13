using System;
using Storage;


namespace FileManager
{
    public class DeleteHandler : IInputHandler
    {
        public string Trigger => "delete";

        public bool Run(string[] args, FileStorage fileManager)
        {
            if (fileManager.LinkExist(args[1]))
            {
                fileManager.Delete(args[1]);
                return true;
            }
            else
            {
                Console.WriteLine("File does not exist");
                return false;
            }
        }
    }
}
