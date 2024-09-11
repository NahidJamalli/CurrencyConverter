using CurrencyConverter.Models;
using CurrencyConverter.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class CurrencyServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly Mock<IOptions<CurrencyApiSettings>> _optionsMock;
    private readonly CurrencyService _currencyService;

    public CurrencyServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        _memoryCacheMock = new Mock<IMemoryCache>();
        _optionsMock = new Mock<IOptions<CurrencyApiSettings>>();
        _optionsMock.Setup(opt => opt.Value).Returns(new CurrencyApiSettings { BaseUrl = "https://api.frankfurter.app/" });

        _currencyService = new CurrencyService(httpClient, _memoryCacheMock.Object, _optionsMock.Object);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ShouldReturnCachedValue_IfAvailable()
    {
        // Arrange
        var expectedResponse = new LatestRateCurrencyResponse
        {
            BaseCurrency = "USD",
            Rates = new Dictionary<string, decimal> { { "EUR", 0.85M } }
        };

        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        _memoryCacheMock.Setup(mc => mc.TryGetValue("LatestRates_USD", out expectedResponse)).Returns(true);

        // Act
        var result = await _currencyService.GetLatestRatesAsync("USD");

        // Assert
        Assert.Equal("USD", result?.BaseCurrency);
        Assert.Contains("EUR", result?.Rates.Keys);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ShouldFetchAndCacheValue_IfNotInCache()
    {
        // Arrange
        var apiResponse = new LatestRateCurrencyResponse
        {
            BaseCurrency = "USD",
            Rates = new Dictionary<string, decimal> { { "EUR", 0.85M } }
        };

        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(apiResponse);

        // Mock HttpClient response
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Mock cache miss
        _memoryCacheMock.Setup(mc => mc.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny)).Returns(false);
        _memoryCacheMock.Setup(mc => mc.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _currencyService.GetLatestRatesAsync("USD");

        // Assert
        Assert.Equal("USD", result?.BaseCurrency);
        Assert.Contains("EUR", result?.Rates?.Keys);
        _memoryCacheMock.Verify(mc => mc.Set(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ShouldThrowException_WhenHttpRequestFailsAfterRetries()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Throws(new HttpRequestException());

        _memoryCacheMock.Setup(mc => mc.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny)).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await _currencyService.GetLatestRatesAsync("USD"));
    }
}
