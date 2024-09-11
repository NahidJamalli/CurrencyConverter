using CurrencyConverter.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;

namespace CurrencyConverter.Services;

public class CurrencyService(HttpClient _httpClient, IMemoryCache _cache, IOptions<CurrencyApiSettings> options)
{
    readonly string _baseUrl = options.Value.BaseUrl;
    const int MaxRetryAttempts = 3;
    private static IAsyncPolicy _circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, timespan) =>
                {
                    Console.WriteLine($"Circuit breaker opened: {exception.Message}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });

    public async Task<LatestRateCurrencyResponse?> GetLatestRatesAsync(string? baseCurrency)
    {
        return await _circuitBreakerPolicy.ExecuteAsync(async () =>
        {
            if (_cache.TryGetValue($"LatestRates_{baseCurrency}", out LatestRateCurrencyResponse cachedRates))
            {
                return cachedRates;
            }

            var result = await RetryPolicyAsync(async () =>
            {
                string response = string.Empty;

                if (!string.IsNullOrWhiteSpace(baseCurrency)) response = await _httpClient.GetStringAsync($"{_baseUrl}latest?base={baseCurrency}");
                else response = await _httpClient.GetStringAsync($"{_baseUrl}latest");

                return JsonConvert.DeserializeObject<LatestRateCurrencyResponse>(response);
            });

            _cache.Set($"LatestRates_{baseCurrency}", result, TimeSpan.FromHours(1));

            return result;
        });
    }

    public async Task<ConvertCurrencyResponse?> ConvertCurrencyAsync(string from, string to, decimal amount)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            throw new ArgumentException("From and To currencies are required.");
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");
        if (new[] { "TRY", "PLN", "THB", "MXN" }.Contains(to))
            throw new InvalidOperationException("Conversion not supported for specified currencies.");

        return await _circuitBreakerPolicy.ExecuteAsync(async () =>
        {
            return await RetryPolicyAsync(async () =>
            {
                var response = await _httpClient.GetStringAsync($"{_baseUrl}latest?base={from}&symbols={to}");
                return JsonConvert.DeserializeObject<ConvertCurrencyResponse>(response);
            });
        });
    }

    public async Task<HistoricalRatesResponse> GetHistoricalRatesAsync(string baseCurrency, string startDate, string endDate, int page, int pageSize)
    {
        if (string.IsNullOrEmpty(baseCurrency))
            throw new ArgumentException("Base currency is required.");
        if (!DateTime.TryParse(startDate, out DateTime _))
            throw new ArgumentException("Invalid start date.");
        if (!DateTime.TryParse(endDate, out DateTime _))
            throw new ArgumentException("Invalid end date.");

        return await _circuitBreakerPolicy.ExecuteAsync(async () =>
        {
            return await RetryPolicyAsync(async () =>
            {
                var response = await _httpClient.GetStringAsync($"{_baseUrl}{startDate}..{endDate}?base={baseCurrency}");
                var result = JsonConvert.DeserializeObject<HistoricalRatesResponse>(response);

                var paginatedRates = result.Rates
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                return new HistoricalRatesResponse
                {
                    Amount = result.Amount,
                    BaseCurrency = result.BaseCurrency,
                    Rates = paginatedRates,
                    StartDate = result.StartDate,
                    EndDate = result.EndDate
                };
            });
        });
    }

    private async Task<T> RetryPolicyAsync<T>(Func<Task<T>> func)
    {
        int attempt = 0;
        var delay = TimeSpan.FromSeconds(2);

        while (attempt < MaxRetryAttempts)
        {
            try
            {
                return await func();
            }
            catch (HttpRequestException) when (attempt < MaxRetryAttempts)
            {
                attempt++;
                await Task.Delay(delay);
                delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            }
        }

        throw new Exception("Failed to execute the operation after multiple attempts.");
    }
}
