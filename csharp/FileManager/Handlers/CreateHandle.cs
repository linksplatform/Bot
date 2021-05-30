
namespace FileManager
{
    class CreateHandle : IInputHandler
    {
        public string Trigger => "Create";

        public bool Run(string[] args,Manager fileManager)
        {
            fileManager.AddFile(args[1], FileLoader.LoadContent(args[2]));
            return true;
        }
    }
}
