using Tinkoff.InvestApi.V1;
using TraderBot.Interfaces;

namespace TraderBot.Providers.Tinkoff;

public class TinkoffAccount : IAccount
{
    private readonly Account _account;

    public TinkoffAccount(Account account)
    {
        _account = account;
    }

    public string Id => _account.Id;
    public string Name => _account.Name;
    public DateTime OpenedDate => _account.OpenedDate.ToDateTime();
}