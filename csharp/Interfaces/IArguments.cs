
namespace Interfaces
{
    public interface IArguments
    {
        public string[] Args { get; set; }

        public ILocalCodeStorage FileStorage { get; set; }
    }
}
