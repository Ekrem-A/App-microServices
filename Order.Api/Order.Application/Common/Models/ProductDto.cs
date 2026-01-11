namespace Order.Application.Common.Models;

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    decimal Price,
    int StockQuantity,
    bool IsAvailable);

