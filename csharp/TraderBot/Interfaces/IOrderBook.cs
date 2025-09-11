namespace TraderBot.Interfaces;

public interface IOrderBookStream : IAsyncEnumerable<IOrderBook>
{
}

public interface IOrderBook
{
    string Figi { get; }
    IEnumerable<IOrderBookEntry> Bids { get; }
    IEnumerable<IOrderBookEntry> Asks { get; }
}

public interface IOrderBookEntry
{
    decimal Price { get; }
    long Quantity { get; }
}