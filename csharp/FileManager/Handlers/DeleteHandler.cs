
namespace FileManager
{
    class DeleteHandler : IInputHandler
    {
        public string Trigger => "Delete";

        public bool Run(string[] args, Manager fileManager)
        {
            fileManager.Delete(args[1]);
            return true;
        }
    }
}
