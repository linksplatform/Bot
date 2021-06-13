using Interfaces;
using Storage.Local;

namespace FileManager
{
    public class Arguments : IArguments
    {
        public string[] Args { get; set; }

        public ILocalCodeStorage FileStorage { get; set; }
    }
}
