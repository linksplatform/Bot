using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace csharp
{
    class FileHandler
    {
        //bad name, i know
        public static async Task<List<File>> HandleAsync(List<string> NameOfFiles)
        {
            List<File> files = new List<File>() { };
            foreach(var f in NameOfFiles)
            {
                using FileStream openStream = System.IO.File.OpenRead(f);
                var file = await JsonSerializer.DeserializeAsync<File>(openStream);
                file = FileLodaer.LoadFile(file);
                files.Add(file);
            }
            return files;
        }
    }
}
