namespace TraderBot.Interfaces;

public interface ITradingApiProvider
{
    Task InitializeAsync();
    Task<IEnumerable<IAccount>> GetAccountsAsync();
    Task<IInstrument> GetInstrumentAsync(string ticker, TradingInstrumentType instrumentType);
    Task<IOrderBookStream> SubscribeToOrderBookAsync(string figi, int depth);
    Task<ITradesStream> SubscribeToTradesAsync(string accountId);
    Task<IEnumerable<IOrder>> GetOrdersAsync(string accountId);
    Task<IEnumerable<IPosition>> GetPositionsAsync(string accountId);
    Task<IPortfolio> GetPortfolioAsync(string accountId);
    Task<IEnumerable<IOperation>> GetOperationsAsync(string accountId, string figi, DateTime from, DateTime to);
    Task<IOrderResponse> PlaceOrderAsync(IOrderRequest orderRequest);
    Task<ICancelOrderResponse> CancelOrderAsync(string accountId, string orderId);
}