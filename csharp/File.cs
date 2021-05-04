using Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csharp
{
    class File : IFile
    {
        public string Path { get; set; }

        public string Content { get; set; }

        public string LocalPath { get; set; }

        public string Trigger { get; set; }
    }
}
