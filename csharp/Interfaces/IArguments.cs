using Storage;

namespace Interfaces
{
    public interface IArguments
    {
        public string[] Args { get; set; }

        public FileStorage FileStorage { get; set; }
    }
}
