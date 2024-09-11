using Newtonsoft.Json;

namespace CurrencyConverter.Models;

public class HistoricalRatesResponse
{
    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("base")]
    public string BaseCurrency { get; set; }

    [JsonProperty("start_date")]
    public DateTime StartDate { get; set; }

    [JsonProperty("end_date")]
    public DateTime EndDate { get; set; }

    [JsonProperty("rates")]
    public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; }
}
