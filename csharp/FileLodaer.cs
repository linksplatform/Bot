using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csharp
{
    class FileLodaer
    {
        public static File LoadFile(File file)
        {
            using (StreamReader sr = new StreamReader(file.LocalPath))
            {
                file.Content = sr.ReadToEnd();
            }
            return file;
        }
    }
}
