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
        public static List<ITrigger<Arguments>> Handlers = new() { }; 

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
            new HelpHandler().Action(new Arguments { FileStorage = dbContext, Args = args });
            try
            {
                Handlers.FirstOrDefault(Handler => Handler.Condition(new Arguments { FileStorage = dbContext, Args = args })).Action(new Arguments { FileStorage = dbContext, Args = args });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToStringWithAllInnerExceptions());
            }
        }
    }
}
