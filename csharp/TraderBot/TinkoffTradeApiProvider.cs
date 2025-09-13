using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace TraderBot;

public class TinkoffTradeApiProvider : ITradeApiProvider
{
    private readonly InvestApiClient _apiClient;
    private readonly string _accountId;
    private readonly string _figi;
    private readonly ILogger<TinkoffTradeApiProvider> _logger;

    public TinkoffTradeApiProvider(InvestApiClient apiClient, string accountId, string figi, ILogger<TinkoffTradeApiProvider> logger)
    {
        _apiClient = apiClient;
        _accountId = accountId;
        _figi = figi;
        _logger = logger;
    }

    public async Task<decimal> GetCurrentPrice(string symbol)
    {
        try
        {
            var request = new GetLastPricesRequest();
            request.Figi.Add(_figi);
            
            var response = await _apiClient.MarketData.GetLastPricesAsync(request);
            if (response.LastPrices.Count > 0)
            {
                var price = response.LastPrices[0].Price;
                return ConvertQuotationToDecimal(price);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current price for {Symbol}", symbol);
        }
        
        return 0;
    }

    public async Task<string> PlaceBuyOrder(string symbol, int lots, decimal price)
    {
        try
        {
            var request = new PostOrderRequest
            {
                Figi = _figi,
                Quantity = lots,
                Price = ConvertToQuotation(price),
                Direction = OrderDirection.Buy,
                AccountId = _accountId,
                OrderType = Tinkoff.InvestApi.V1.OrderType.Limit,
                OrderId = Guid.NewGuid().ToString()
            };

            var response = await _apiClient.Orders.PostOrderAsync(request);
            return response.OrderId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place buy order for {Symbol}", symbol);
            return string.Empty;
        }
    }

    public async Task<string> PlaceSellOrder(string symbol, int lots, decimal price)
    {
        try
        {
            var request = new PostOrderRequest
            {
                Figi = _figi,
                Quantity = lots,
                Price = ConvertToQuotation(price),
                Direction = OrderDirection.Sell,
                AccountId = _accountId,
                OrderType = Tinkoff.InvestApi.V1.OrderType.Limit,
                OrderId = Guid.NewGuid().ToString()
            };

            var response = await _apiClient.Orders.PostOrderAsync(request);
            return response.OrderId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place sell order for {Symbol}", symbol);
            return string.Empty;
        }
    }

    public async Task<bool> CancelOrder(string orderId)
    {
        try
        {
            var request = new CancelOrderRequest
            {
                AccountId = _accountId,
                OrderId = orderId
            };

            await _apiClient.Orders.CancelOrderAsync(request);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);
            return false;
        }
    }

    public async Task<OrderState> GetOrderStatus(string orderId)
    {
        try
        {
            var request = new GetOrderStateRequest
            {
                AccountId = _accountId,
                OrderId = orderId
            };

            var response = await _apiClient.Orders.GetOrderStateAsync(request);
            return ConvertOrderExecutionStatus(response.ExecutionReportStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order status for {OrderId}", orderId);
            return OrderState.Rejected;
        }
    }

    public async Task<(decimal Free, decimal Locked)> GetBalance(string currency)
    {
        try
        {
            var request = new PositionsRequest
            {
                AccountId = _accountId
            };

            var response = await _apiClient.Operations.GetPositionsAsync(request);
            
            var money = response.Money.FirstOrDefault(m => m.Currency.ToLower() == currency.ToLower());
            if (money != null)
            {
                return (ConvertMoneyValue(money), 0); // Tinkoff API doesn't separate free/locked easily
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balance for {Currency}", currency);
        }
        
        return (0, 0);
    }

    public async Task<List<OrderBook>> GetOrderBook(string symbol, int depth)
    {
        try
        {
            var request = new GetOrderBookRequest
            {
                Figi = _figi,
                Depth = depth
            };

            var response = await _apiClient.MarketData.GetOrderBookAsync(request);
            var orderBook = new List<OrderBook>();

            foreach (var bid in response.Bids)
            {
                orderBook.Add(new OrderBook
                {
                    Price = ConvertQuotationToDecimal(bid.Price),
                    Quantity = bid.Quantity,
                    Type = OrderType.Buy
                });
            }

            foreach (var ask in response.Asks)
            {
                orderBook.Add(new OrderBook
                {
                    Price = ConvertQuotationToDecimal(ask.Price),
                    Quantity = ask.Quantity,
                    Type = OrderType.Sell
                });
            }

            return orderBook;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order book for {Symbol}", symbol);
            return new List<OrderBook>();
        }
    }

    private static decimal ConvertMoneyValue(MoneyValue moneyValue)
    {
        return moneyValue.Units + (decimal)moneyValue.Nano / 1_000_000_000;
    }

    private static decimal ConvertQuotationToDecimal(Quotation quotation)
    {
        return quotation.Units + (decimal)quotation.Nano / 1_000_000_000;
    }

    private static Quotation ConvertToQuotation(decimal value)
    {
        var units = (long)Math.Floor(value);
        var nano = (int)((value - units) * 1_000_000_000);
        
        return new Quotation
        {
            Units = units,
            Nano = nano
        };
    }

    private static OrderState ConvertOrderExecutionStatus(OrderExecutionReportStatus status)
    {
        return status switch
        {
            OrderExecutionReportStatus.ExecutionReportStatusNew => OrderState.New,
            OrderExecutionReportStatus.ExecutionReportStatusPartiallyfill => OrderState.PartiallyFilled,
            OrderExecutionReportStatus.ExecutionReportStatusFill => OrderState.Filled,
            OrderExecutionReportStatus.ExecutionReportStatusCancelled => OrderState.Cancelled,
            OrderExecutionReportStatus.ExecutionReportStatusRejected => OrderState.Rejected,
            _ => OrderState.Rejected
        };
    }
}