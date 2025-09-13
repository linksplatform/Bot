using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

// Create minimal host to test logging
var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddNLog();
});

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Test different log levels
logger.LogInformation("This is an information message");
logger.LogWarning("This is a warning message");
logger.LogError("This is an error message that should be logged to file");

try
{
    throw new Exception("Test exception for logging");
}
catch (Exception ex)
{
    logger.LogError(ex, "Test exception caught and logged");
}

Console.WriteLine("Logging test completed. Check the logs directory for files.");