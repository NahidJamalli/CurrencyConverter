namespace CurrencyConverter.Models;

public class ConversionRequest
{
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal Amount { get; set; }
}
