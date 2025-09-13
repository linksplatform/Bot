using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TraderBot;

public class PerformanceTracker
{
    private readonly ILogger<PerformanceTracker> _logger;
    private readonly List<PerformanceSnapshot> _snapshots = new();
    private readonly string _dataFilePath;
    private decimal _initialPortfolioValue = 0;
    private decimal _initialEtfPrice = 0;
    private DateTime _startDate;

    public PerformanceTracker(ILogger<PerformanceTracker> logger, string dataFilePath = "performance_data.json")
    {
        _logger = logger;
        _dataFilePath = dataFilePath;
        LoadPerformanceData();
    }

    public async Task RecordPortfolioValue(decimal portfolioValue, decimal etfPrice)
    {
        var snapshot = new PerformanceSnapshot
        {
            Timestamp = DateTime.UtcNow,
            PortfolioValue = portfolioValue,
            EtfPrice = etfPrice
        };

        if (_snapshots.Count == 0)
        {
            _initialPortfolioValue = portfolioValue;
            _initialEtfPrice = etfPrice;
            _startDate = snapshot.Timestamp;
            _logger.LogInformation($"Performance tracking started - Initial portfolio: {_initialPortfolioValue:F2}, Initial ETF price: {_initialEtfPrice:F2}");
        }

        _snapshots.Add(snapshot);

        // Calculate and log performance metrics
        var performance = CalculatePerformance();
        _logger.LogInformation($"Portfolio Performance: {performance.TotalReturn:P2} | ETF Performance: {performance.EtfReturn:P2} | Outperformance: {performance.Outperformance:P2}");

        // Save periodically
        if (_snapshots.Count % 10 == 0)
        {
            await SavePerformanceData();
        }

        // Check if we're meeting the 1% outperformance goal
        if (performance.DaysActive >= 365 && performance.AnnualizedOutperformance < 0.01m)
        {
            _logger.LogWarning($"Performance goal not met! Annual outperformance: {performance.AnnualizedOutperformance:P2}, Target: +1.00%");
        }
        else if (performance.DaysActive >= 365 && performance.AnnualizedOutperformance >= 0.01m)
        {
            _logger.LogInformation($"Performance goal achieved! Annual outperformance: {performance.AnnualizedOutperformance:P2}");
        }
    }

    public PerformanceMetrics CalculatePerformance()
    {
        if (_snapshots.Count == 0)
        {
            return new PerformanceMetrics();
        }

        var latest = _snapshots.Last();
        var daysActive = (latest.Timestamp - _startDate).TotalDays;

        // Calculate portfolio return
        var portfolioReturn = (_initialPortfolioValue != 0) 
            ? (latest.PortfolioValue - _initialPortfolioValue) / _initialPortfolioValue
            : 0;

        // Calculate ETF buy-and-hold return
        var etfReturn = (_initialEtfPrice != 0)
            ? (latest.EtfPrice - _initialEtfPrice) / _initialEtfPrice
            : 0;

        var outperformance = portfolioReturn - etfReturn;

        // Annualize the returns
        var annualizedPortfolioReturn = daysActive > 0 
            ? (decimal)(Math.Pow((double)(1 + portfolioReturn), 365.0 / daysActive) - 1)
            : 0;
            
        var annualizedEtfReturn = daysActive > 0
            ? (decimal)(Math.Pow((double)(1 + etfReturn), 365.0 / daysActive) - 1)
            : 0;

        var annualizedOutperformance = annualizedPortfolioReturn - annualizedEtfReturn;

        return new PerformanceMetrics
        {
            TotalReturn = portfolioReturn,
            EtfReturn = etfReturn,
            Outperformance = outperformance,
            AnnualizedPortfolioReturn = annualizedPortfolioReturn,
            AnnualizedEtfReturn = annualizedEtfReturn,
            AnnualizedOutperformance = annualizedOutperformance,
            DaysActive = daysActive,
            TotalTrades = CountTrades(),
            CurrentPortfolioValue = latest.PortfolioValue,
            InitialPortfolioValue = _initialPortfolioValue
        };
    }

    private int CountTrades()
    {
        // This would be enhanced to track actual trades
        // For now, estimate based on data points
        return Math.Max(0, _snapshots.Count - 1);
    }

    private async Task SavePerformanceData()
    {
        try
        {
            var data = new PerformanceData
            {
                InitialPortfolioValue = _initialPortfolioValue,
                InitialEtfPrice = _initialEtfPrice,
                StartDate = _startDate,
                Snapshots = _snapshots
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_dataFilePath, json);
            _logger.LogDebug($"Performance data saved to {_dataFilePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save performance data");
        }
    }

    private void LoadPerformanceData()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var json = File.ReadAllText(_dataFilePath);
                var data = JsonSerializer.Deserialize<PerformanceData>(json);
                
                if (data != null)
                {
                    _initialPortfolioValue = data.InitialPortfolioValue;
                    _initialEtfPrice = data.InitialEtfPrice;
                    _startDate = data.StartDate;
                    _snapshots.AddRange(data.Snapshots);
                    
                    _logger.LogInformation($"Loaded {_snapshots.Count} performance snapshots from {_dataFilePath}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load performance data");
        }
    }

    public async Task GeneratePerformanceReport()
    {
        var metrics = CalculatePerformance();
        var report = $@"
=== TRADING BOT PERFORMANCE REPORT ===
Trading Period: {_startDate:yyyy-MM-dd} to {DateTime.UtcNow:yyyy-MM-dd} ({metrics.DaysActive:F1} days)

Portfolio Performance:
- Initial Value: {metrics.InitialPortfolioValue:C2}
- Current Value: {metrics.CurrentPortfolioValue:C2}
- Total Return: {metrics.TotalReturn:P2}
- Annualized Return: {metrics.AnnualizedPortfolioReturn:P2}

ETF Buy-and-Hold Performance:
- ETF Return: {metrics.EtfReturn:P2}
- Annualized ETF Return: {metrics.AnnualizedEtfReturn:P2}

Strategy Performance:
- Outperformance: {metrics.Outperformance:P2}
- Annualized Outperformance: {metrics.AnnualizedOutperformance:P2}
- Performance Goal (1% annually): {(metrics.AnnualizedOutperformance >= 0.01m ? "✅ ACHIEVED" : "❌ NOT MET")}

Trading Activity:
- Estimated Trades: {metrics.TotalTrades}
- Data Points: {_snapshots.Count}

=== END REPORT ===
";

        _logger.LogInformation(report);
        
        // Save detailed report to file
        var reportFilePath = $"performance_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
        await File.WriteAllTextAsync(reportFilePath, report);
        _logger.LogInformation($"Detailed performance report saved to {reportFilePath}");
    }
}

public class PerformanceSnapshot
{
    public DateTime Timestamp { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal EtfPrice { get; set; }
}

public class PerformanceData
{
    public decimal InitialPortfolioValue { get; set; }
    public decimal InitialEtfPrice { get; set; }
    public DateTime StartDate { get; set; }
    public List<PerformanceSnapshot> Snapshots { get; set; } = new();
}

public class PerformanceMetrics
{
    public decimal TotalReturn { get; set; }
    public decimal EtfReturn { get; set; }
    public decimal Outperformance { get; set; }
    public decimal AnnualizedPortfolioReturn { get; set; }
    public decimal AnnualizedEtfReturn { get; set; }
    public decimal AnnualizedOutperformance { get; set; }
    public double DaysActive { get; set; }
    public int TotalTrades { get; set; }
    public decimal CurrentPortfolioValue { get; set; }
    public decimal InitialPortfolioValue { get; set; }
}