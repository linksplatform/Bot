namespace TraderBot.Interfaces;

public interface IAccount
{
    string Id { get; }
    string Name { get; }
    DateTime OpenedDate { get; }
}