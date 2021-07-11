using Interfaces;

namespace FileManager
{
    public class DeleteTrigger : ITrigger<Context>
    {
        public bool Condition(Context arguments) => arguments.Args[0].ToLower() == "delete";

        public void Action(Context arguments) => arguments.FileStorage.Delete(ulong.Parse(arguments.Args[1]));
    }
}
