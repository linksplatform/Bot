using Platform.Exceptions;
using Platform.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileManager
{
    class Program
    {
        public static List<IInputHandler> Handlers = new List<IInputHandler> { }; 

        private static void CreateArrayOfTriggers()
        {
            Handlers.Add(new CreateHandle());
            Handlers.Add(new DeleteHandler());
            Handlers.Add(new Help());
        }

        static void Main(string[] args)
        {
            CreateArrayOfTriggers();
            using ConsoleCancellation cancellation = new ConsoleCancellation();
            var dbContext = new Manager(ConsoleHelpers.GetOrReadArgument(0, "Database file name" , args));
            try
            {
                Handlers.FirstOrDefault(IInputHandler => IInputHandler.Trigger == "Help").Run(args, dbContext);
                while (!cancellation.Token.IsCancellationRequested)
                {
                    var command = Console.ReadLine();
                    var exitCode = Handlers.FirstOrDefault(IInputHandler => IInputHandler.Trigger == command.Split().First()).Run(command.Split(), dbContext);
                    if(exitCode == true)
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
