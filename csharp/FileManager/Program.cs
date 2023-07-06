using Interfaces;
using Platform.Exceptions;
using Platform.IO;
using Storage.Local;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileManager
{
    /// <summary>
    /// <para>
    /// Represents the program.
    /// </para>
    /// <para></para>
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// <para>
        /// The get files by file set name trigger.
        /// </para>
        /// <para></para>
        /// </summary>
        public static List<ITrigger<Context>> Handlers = new()
        {
            new CreateTrigger(),
            new DeleteTrigger(),
            new HelpTrigger(),
            new LinksPrinterTrigger(),
            new ShowTrigger(),
            new HelpTrigger(),
            new CreateFileSetTrigger(),
            new GetFilesByFileSetNameTrigger()
        };
        private static async Task Main(string[] args)
        {
            using ConsoleCancellation cancellation = new();
            var dbContext = new FileStorage(ConsoleHelpers.GetOrReadArgument(0, "Database file name", args));
            new HelpTrigger().Action(new Context { FileStorage = dbContext, Args = args });
            try
            {
                while (!cancellation.Token.IsCancellationRequested)
                {
                    var input = Console.ReadLine();
                    var Context = new Context { FileStorage = dbContext, Args = input.Split() };
                    foreach (var handler in Handlers)
                    {
                        if (await handler.Condition(Context))
                        {
                            handler.Action(Context);
                        }
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
