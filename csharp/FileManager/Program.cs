using Platform.Exceptions;
using Platform.IO;
using Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileManager
{
    class Program
    {
        public static List<IInputHandler> Handlers = new() { }; 

        private static void CreateArrayOfTriggers()
        {
            Handlers.Add(new CreateHandle());
            Handlers.Add(new DeleteHandler());
            Handlers.Add(new HelpHandler());
            Handlers.Add(new LinksPrinterHandler());
            Handlers.Add(new ShowHandler());
            Handlers.Add(new HelpHandler());
        }

        static void Main(string[] args)
        {
            CreateArrayOfTriggers();
            using ConsoleCancellation cancellation = new();
            var dbContext = new FileStorage(ConsoleHelpers.GetOrReadArgument(0, "Database file name" , args));
            try
            {
                Handlers.FirstOrDefault(IInputHandler => IInputHandler.Trigger == "help").Run(args, dbContext);
                while (!cancellation.Token.IsCancellationRequested)
                {
                    var command = Console.ReadLine();
                    var exitCode = Handlers.FirstOrDefault(IInputHandler => IInputHandler.Trigger == command.Split().First().ToLower()).Run(command.Split(), dbContext);
                    if (exitCode == true)
                    {
                        Console.WriteLine("Done!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToStringWithAllInnerExceptions());
            }
        }
    }
}
