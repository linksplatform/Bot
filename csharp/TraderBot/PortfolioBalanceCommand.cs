using System.Text.Json;

namespace TraderBot;

/// <summary>Explicit offline entry point. It does not construct the broker client or trading host.</summary>
public static class PortfolioBalanceCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length != 2)
        {
            error.WriteLine("Usage: --balance-portfolio <portfolio.json>");
            return 1;
        }
        try
        {
            var request = JsonSerializer.Deserialize<PortfolioBalanceRequest>(File.ReadAllText(args[1]))
                ?? throw new ArgumentException("A portfolio request is required.");
            var store = new PortfolioAllocationStorage(new FinancialStorage());
            var snapshot = store.Save(request.PortfolioName, request.TargetPercentages);
            var plan = store.Calculate(snapshot, request.CurrentValues);
            output.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException or IOException or UnauthorizedAccessException or OverflowException)
        {
            error.WriteLine($"Cannot balance portfolio: {exception.Message}");
            return 1;
        }
    }

    public sealed class PortfolioBalanceRequest
    {
        public string PortfolioName { get; set; } = "";
        public Dictionary<string, decimal> TargetPercentages { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, decimal> CurrentValues { get; set; } = new(StringComparer.Ordinal);
    }
}
