using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bot.Interfaces;

namespace Storage
{
    /// <summary>
    /// Console application demonstrating the GitHub Copilot queue system.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("GitHub Copilot Queue System Demo");
            Console.WriteLine("================================");

            // Setup dependency injection
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IDataService>(provider => 
                        new CopilotDataService("copilot_queue.db"));
                    
                    services.AddSingleton<CopilotQueueManager>(provider =>
                    {
                        var dataService = provider.GetRequiredService<IDataService>();
                        var logger = provider.GetService<ILogger<CopilotQueueManager>>();
                        return new CopilotQueueManager(dataService, logger);
                    });

                    services.AddHostedService<CopilotQueueService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });

            using var host = hostBuilder.Build();

            // Start the background service
            var hostTask = host.RunAsync();

            // Demo the queue system
            await DemonstrateQueueSystem(host.Services);

            // Wait for user input to exit
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();

            // Stop the host
            await host.StopAsync();
            await hostTask;
        }

        private static async Task DemonstrateQueueSystem(IServiceProvider services)
        {
            var dataService = services.GetRequiredService<IDataService>();
            var queueManager = services.GetRequiredService<CopilotQueueManager>();

            Console.WriteLine("\n--- Queue System Demonstration ---");

            try
            {
                // Add some sample requests
                Console.WriteLine("Adding sample requests to the queue...");
                
                var request1 = await dataService.EnqueueCopilotRequestAsync(
                    1001, "Python", "Create a function to calculate fibonacci numbers", DateTime.UtcNow);
                Console.WriteLine($"Added request {request1}: Python Fibonacci function");

                var request2 = await dataService.EnqueueCopilotRequestAsync(
                    1002, "JavaScript", "Create a REST API endpoint for user authentication", DateTime.UtcNow);
                Console.WriteLine($"Added request {request2}: JavaScript REST API");

                var request3 = await dataService.EnqueueCopilotRequestAsync(
                    1001, "C#", "Implement a binary search algorithm", DateTime.UtcNow);
                Console.WriteLine($"Added request {request3}: C# Binary search");

                // Check queue status
                await Task.Delay(1000); // Give it a moment to process
                
                var queueLength = await dataService.GetQueueLengthAsync();
                Console.WriteLine($"\nCurrent queue length: {queueLength}");

                var userRequests = await dataService.GetUserPendingRequestsAsync(1001);
                Console.WriteLine($"Pending requests for user 1001: {userRequests.Count}");

                var position = await dataService.GetQueuePositionAsync(request1);
                Console.WriteLine($"Queue position for request {request1}: {position}");

                // Show statistics
                var stats = await queueManager.GetStatisticsAsync();
                Console.WriteLine($"\nQueue Statistics:");
                Console.WriteLine($"  Pending requests: {stats.PendingRequests}");
                Console.WriteLine($"  Processing interval: {stats.ProcessingInterval}");
                Console.WriteLine($"  Currently processing: {stats.IsProcessing}");

                Console.WriteLine("\nThe background service will process these requests automatically.");
                Console.WriteLine("Watch the logs above to see the processing in action.");

                // Wait a bit to let processing happen
                await Task.Delay(10000);

                // Check final queue status
                var finalQueueLength = await dataService.GetQueueLengthAsync();
                Console.WriteLine($"\nFinal queue length: {finalQueueLength}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during demonstration: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}