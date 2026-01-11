using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Models;
using System.Net.Http.Json;

namespace Order.Infrastructure.ExternalServices;

public class CartServiceClient : ICartService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CartServiceClient> _logger;

    public CartServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CartServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = configuration["ExternalServices:CartService:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }
    }

    public async Task<CartDto?> GetCartAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/cart/{userId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get cart for user {UserId}. Status: {StatusCode}", userId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CartDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for user {UserId}", userId);
            return null;
        }
    }

    public async Task ClearCartAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/cart/{userId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to clear cart for user {UserId}. Status: {StatusCode}", userId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
        }
    }
}

