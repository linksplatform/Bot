using Interfaces;
using Platform.Exceptions;
using Platform.IO;
using Storage.Local;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileManager
{
    class Program
    {
        public static List<ITrigger<Context>> Handlers = new() { }; 

        private static void CreateArrayOfTriggers()
        {
            Handlers.Add(new CreateTrigger());
            Handlers.Add(new DeleteTrigger());
            Handlers.Add(new HelpTrigger());
            Handlers.Add(new LinksPrinterTrigger());
            Handlers.Add(new ShowTrigger());
            Handlers.Add(new HelpTrigger());
            Handlers.Add(new CreateFileSetTrigger());
            Handlers.Add(new GetFilesByFileSetNameTrigger());
        }

        static void Main(string[] args)
        {
            CreateArrayOfTriggers();
            using ConsoleCancellation cancellation = new();
            var dbContext = new FileStorage(ConsoleHelpers.GetOrReadArgument(0, "Database file name" , args));
            new HelpTrigger().Action(new Context { FileStorage = dbContext, Args = args });
            try
            {
                while (!cancellation.Token.IsCancellationRequested)
                {
                    var input = Console.ReadLine();
                    Handlers.FirstOrDefault(Handler => Handler.Condition(new Context { FileStorage = dbContext, Args = input.Split()})).Action(new Context { FileStorage = dbContext, Args = input.Split() });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToStringWithAllInnerExceptions());
            }
        }
    }
}
