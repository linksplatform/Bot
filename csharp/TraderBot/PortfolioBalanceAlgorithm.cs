using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using Microsoft.Extensions.Logging;

namespace TraderBot;

public class PortfolioBalanceAlgorithm
{
    private readonly FinancialStorage _storage;
    private readonly InvestApiClient _investApi;
    private readonly ILogger<PortfolioBalanceAlgorithm> _logger;
    private readonly PortfolioBalanceSettings _settings;
    private readonly Account _account;

    public PortfolioBalanceAlgorithm(
        FinancialStorage storage, 
        InvestApiClient investApi, 
        ILogger<PortfolioBalanceAlgorithm> logger,
        PortfolioBalanceSettings settings,
        Account account)
    {
        _storage = storage;
        _investApi = investApi;
        _logger = logger;
        _settings = settings;
        _account = account;
    }

    public async Task<List<RebalanceAction>> AnalyzePortfolioBalance()
    {
        _logger.LogInformation("Starting portfolio balance analysis");
        
        var currentPortfolio = await GetCurrentPortfolio();
        var totalPortfolioValue = currentPortfolio.Values.Sum();
        
        if (totalPortfolioValue <= 0)
        {
            _logger.LogWarning("Portfolio has no value, skipping rebalance");
            return new List<RebalanceAction>();
        }

        var rebalanceActions = new List<RebalanceAction>();
        
        foreach (var allocation in _settings.AssetAllocations)
        {
            // Store allocation data in Deep storage using FinancialStorage
            var assetTickerLink = _storage.StringToUnicodeSequenceConverter.Convert(allocation.Ticker);
            var targetPercentRational = _storage.DecimalToRationalConverter.Convert(allocation.TargetPercent);
            
            var currentValue = currentPortfolio.GetValueOrDefault(allocation.Ticker, 0);
            var currentPercent = totalPortfolioValue > 0 ? (currentValue / totalPortfolioValue) * 100 : 0;
            var targetPercent = allocation.TargetPercent;
            var deviation = Math.Abs(currentPercent - targetPercent);
            
            // Store current allocation in Deep storage
            var currentPercentRational = _storage.DecimalToRationalConverter.Convert(currentPercent);
            
            _logger.LogInformation($"Asset {allocation.Ticker}: Current {currentPercent:F2}%, Target {targetPercent:F2}%, Deviation {deviation:F2}%");
            
            if (deviation > _settings.RebalanceThresholdPercent)
            {
                var targetValue = totalPortfolioValue * (targetPercent / 100);
                var amountToRebalance = targetValue - currentValue;
                
                var rebalanceAction = new RebalanceAction
                {
                    Ticker = allocation.Ticker,
                    AssetType = allocation.AssetType,
                    Instrument = allocation.Instrument,
                    CurrentValue = currentValue,
                    TargetValue = targetValue,
                    AmountToRebalance = amountToRebalance,
                    CurrentPercent = currentPercent,
                    TargetPercent = targetPercent,
                    Action = amountToRebalance > 0 ? RebalanceActionType.Buy : RebalanceActionType.Sell
                };
                
                rebalanceActions.Add(rebalanceAction);
                
                // Store rebalance information in Deep storage
                var rebalanceAmountRational = _storage.DecimalToRationalConverter.Convert(Math.Abs(amountToRebalance));
                
                _logger.LogInformation($"Rebalance needed for {allocation.Ticker}: {rebalanceAction.Action} {Math.Abs(amountToRebalance):F2} RUB");
            }
        }
        
        _logger.LogInformation($"Portfolio analysis complete. {rebalanceActions.Count} rebalance actions identified");
        return rebalanceActions;
    }

    private async Task<Dictionary<string, decimal>> GetCurrentPortfolio()
    {
        var portfolio = new Dictionary<string, decimal>();
        
        try
        {
            var portfolioResponse = await _investApi.Operations.GetPortfolioAsync(new PortfolioRequest 
            { 
                AccountId = _account.Id 
            });
            
            foreach (var position in portfolioResponse.Positions)
            {
                try 
                {
                    var currentValue = TradingService.MoneyValueToDecimal(position.CurrentPrice) * position.Quantity;
                    // Use position.Figi as identifier since we don't need to resolve to ticker for this demo
                    var ticker = await GetTickerByFigi(position.Figi) ?? position.Figi;
                    portfolio[ticker] = currentValue;
                    
                    _logger.LogInformation($"Position {ticker}: {position.Quantity} units, value {currentValue:F2} RUB");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error processing position {position.Figi}");
                }
            }
            
            var cashPositions = await _investApi.Operations.GetPositionsAsync(new PositionsRequest
            {
                AccountId = _account.Id
            });
            
            foreach (var money in cashPositions.Money)
            {
                if (money.Currency.ToLower() == "rub")
                {
                    var cashValue = TradingService.MoneyValueToDecimal(money);
                    portfolio["CASH_RUB"] = cashValue;
                    _logger.LogInformation($"Cash position RUB: {cashValue:F2}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current portfolio");
        }
        
        return portfolio;
    }

    private async Task<string?> GetTickerByFigi(string figi)
    {
        try
        {
            var etfs = await _investApi.Instruments.EtfsAsync();
            var etf = etfs.Instruments.FirstOrDefault(e => e.Figi == figi);
            if (etf != null) return etf.Ticker;
            
            var shares = await _investApi.Instruments.SharesAsync();
            var share = shares.Instruments.FirstOrDefault(s => s.Figi == figi);
            if (share != null) return share.Ticker;
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting ticker by FIGI {figi}");
            return null;
        }
    }
}

public class RebalanceAction
{
    public string Ticker { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public Instrument Instrument { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal AmountToRebalance { get; set; }
    public decimal CurrentPercent { get; set; }
    public decimal TargetPercent { get; set; }
    public RebalanceActionType Action { get; set; }
}

public enum RebalanceActionType
{
    Buy,
    Sell,
    Hold
}