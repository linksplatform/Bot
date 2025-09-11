using Tinkoff.InvestApi.V1;
using TraderBot.Interfaces;

namespace TraderBot.Providers.Tinkoff;

public class TinkoffPosition : IPosition
{
    private readonly PositionsSecurities _position;

    public TinkoffPosition(PositionsSecurities position)
    {
        _position = position;
    }

    public string Figi => _position.Figi;
    public decimal Balance => _position.Balance;
}

public class TinkoffPortfolio : IPortfolio
{
    private readonly PortfolioResponse _portfolio;

    public TinkoffPortfolio(PortfolioResponse portfolio)
    {
        _portfolio = portfolio;
    }

    public IEnumerable<IPortfolioPosition> Positions => _portfolio.Positions.Select(p => new TinkoffPortfolioPosition(p));
    public IEnumerable<IMoneyValue> Money => _portfolio.Money.Select(m => new TinkoffMoneyValue(m));
    public IEnumerable<IMoneyValue> Blocked => _portfolio.Blocked.Select(b => new TinkoffMoneyValue(b));
}

public class TinkoffPortfolioPosition : IPortfolioPosition
{
    private readonly PortfolioPosition _position;

    public TinkoffPortfolioPosition(PortfolioPosition position)
    {
        _position = position;
    }

    public string Figi => _position.Figi;
    public decimal Quantity => MoneyValueToDecimal(_position.Quantity);

    private static decimal MoneyValueToDecimal(MoneyValue value) => value.Units + value.Nano / 1000000000m;
}

public class TinkoffMoneyValue : IMoneyValue
{
    private readonly MoneyValue _moneyValue;

    public TinkoffMoneyValue(MoneyValue moneyValue)
    {
        _moneyValue = moneyValue;
    }

    public string Currency => _moneyValue.Currency;
    public decimal Value => _moneyValue.Units + _moneyValue.Nano / 1000000000m;
}