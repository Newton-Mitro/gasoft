namespace Gasoft.Application.Parties;

public sealed class CurrencyNotConfiguredException(string currencyCode)
    : Exception($"Currency '{currencyCode}' is not configured.");
