using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Models;
using System.Net.Http.Json;

namespace Order.Infrastructure.ExternalServices;

public class CatalogServiceClient : ICatalogService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;

    public CatalogServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CatalogServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = configuration["ExternalServices:CatalogService:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }
    }

    public async Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/products/{productId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get product {ProductId}. Status: {StatusCode}", productId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", productId);
            return null;
        }
    }

    public async Task<IEnumerable<ProductDto>> GetProductsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var idsQuery = string.Join(",", productIds);
            var response = await _httpClient.GetAsync($"/api/products?ids={idsQuery}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get products. Status: {StatusCode}", response.StatusCode);
                return Enumerable.Empty<ProductDto>();
            }

            return await response.Content.ReadFromJsonAsync<IEnumerable<ProductDto>>(cancellationToken: cancellationToken)
                ?? Enumerable.Empty<ProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return Enumerable.Empty<ProductDto>();
        }
    }

    public async Task<bool> CheckStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/products/{productId}/stock?quantity={quantity}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock for product {ProductId}", productId);
            return false;
        }
    }
}

