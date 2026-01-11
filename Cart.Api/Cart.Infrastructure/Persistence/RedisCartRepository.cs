using Cart.Application.Abstractions;
using Cart.Domain.CartAggregate;
using Cart.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Cart.Infrastructure.Persistence;

public class RedisCartRepository : ICartRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _redisOptions;
    private readonly CartOptions _cartOptions;
    private readonly ILogger<RedisCartRepository> _logger;

    public RedisCartRepository(
        IConnectionMultiplexer redis,
        IOptions<RedisOptions> redisOptions,
        IOptions<CartOptions> cartOptions,
        ILogger<RedisCartRepository> logger)
    {
        _redis = redis;
        _redisOptions = redisOptions.Value;
        _cartOptions = cartOptions.Value;
        _logger = logger;
    }

    private string GetKey(string userId) => $"{_redisOptions.InstanceName}{userId}";

    public async Task<ShoppingCart?> GetCartAsync(string userId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(userId);

        var json = await db.StringGetAsync(key);

        if (json.IsNullOrEmpty)
        {
            _logger.LogDebug("Cart not found for user {UserId}", userId);
            return null;
        }

        _logger.LogDebug("Cart retrieved for user {UserId}", userId);
        return CartSerializationHelper.Deserialize(json!);
    }

    public async Task SaveCartAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(cart.UserId);
        var json = CartSerializationHelper.Serialize(cart);
        var ttl = TimeSpan.FromDays(_cartOptions.TtlDays);

        await db.StringSetAsync(key, json, ttl);

        _logger.LogDebug("Cart saved for user {UserId} with TTL {TtlDays} days", cart.UserId, _cartOptions.TtlDays);
    }

    public async Task DeleteCartAsync(string userId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(userId);

        await db.KeyDeleteAsync(key);

        _logger.LogDebug("Cart deleted for user {UserId}", userId);
    }
}

