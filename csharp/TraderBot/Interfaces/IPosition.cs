namespace TraderBot.Interfaces;

public interface IPosition
{
    string Figi { get; }
    decimal Balance { get; }
}

public interface IPortfolio
{
    IEnumerable<IPortfolioPosition> Positions { get; }
    IEnumerable<IMoneyValue> Money { get; }
    IEnumerable<IMoneyValue> Blocked { get; }
}

public interface IPortfolioPosition
{
    string Figi { get; }
    decimal Quantity { get; }
}

public interface IMoneyValue
{
    string Currency { get; }
    decimal Value { get; }
}