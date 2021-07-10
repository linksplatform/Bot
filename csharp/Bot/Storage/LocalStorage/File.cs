using Interfaces;

namespace Storage.Local
{
    public class File : IFile
    {
        public string Path { get; set; }

        public string Content { get; set; }
    }
}
