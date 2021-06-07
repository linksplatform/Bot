
namespace FileManager
{
    interface IInputHandler
    {
        public string Trigger { get; }

        public bool Run(string[] args, Manager fileManager);
    }
}
