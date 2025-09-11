namespace TraderBot;

public class ApiProviderSettings
{
    public string Provider { get; set; } = "Tinkoff";
}

public class InvestApiSettings
{
    public string? AccessToken { get; set; }
    public string? AppName { get; set; }
}