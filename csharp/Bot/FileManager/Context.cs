using Interfaces;
using Storage.Local;

namespace FileManager
{
    public class Context
    {
        public string[] Args { get; set; }

        public FileStorage FileStorage { get; set; }
    }
}
