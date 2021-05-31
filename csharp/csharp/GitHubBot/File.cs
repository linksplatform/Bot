using Interfaces;

namespace csharp
{
    class File : IFile
    {
        public string Path { get; set; }

        public string Content { get; set; }
    }
}