using CurrencyConverter.Models;
using CurrencyConverter.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CurrencyController(CurrencyService _currencyService) : ControllerBase
{
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestRates([FromQuery] string? baseCurrency = null)
    {
        var rates = await _currencyService.GetLatestRatesAsync(baseCurrency);
        return Ok(rates);
    }

    [HttpPost("convert")]
    public async Task<IActionResult> ConvertCurrency([FromBody] ConversionRequest request)
    {
        var result = await _currencyService.ConvertCurrencyAsync(request.FromCurrency, request.ToCurrency, request.Amount);
        return Ok(result);
    }

    [HttpGet("historical/{baseCurrency}")]
    public async Task<IActionResult> GetHistoricalRates(string baseCurrency, [FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _currencyService.GetHistoricalRatesAsync(baseCurrency, startDate, endDate, page, pageSize);
        return Ok(result);
    }
}
