
namespace Interfaces
{
    interface IFile
    {
        public string LocalPath { get; set; }

        public string Path { get; set; }

        public string Content { get; set; }
    }
}
