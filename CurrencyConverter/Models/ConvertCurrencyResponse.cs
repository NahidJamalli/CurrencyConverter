using Newtonsoft.Json;

namespace CurrencyConverter.Models
{
    public class ConvertCurrencyResponse
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("base")]
        public string BaseCurrency { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("rates")]
        public Dictionary<string, decimal> Rates { get; set; }
    }
}
