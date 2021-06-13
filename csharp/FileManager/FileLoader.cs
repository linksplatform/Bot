using System.IO;

namespace FileManager
{
    class FileLoader
    {
        public static string LoadContent(string pathToFile)
        {
            using StreamReader sr = new(pathToFile);
            return sr.ReadToEnd();
        }
    }
}
