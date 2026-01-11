namespace Catalog.Domain.Events;

public static class KafkaTopics
{
    public const string ProductCreated = "catalog.product.created";
    public const string ProductUpdated = "catalog.product.updated";
    public const string ProductDeleted = "catalog.product.deleted";
    public const string ProductPriceChanged = "catalog.product.price-changed";
    public const string ProductStockUpdated = "catalog.product.stock-updated";
    public const string CategoryCreated = "catalog.category.created";
    public const string CategoryUpdated = "catalog.category.updated";
    public const string BrandCreated = "catalog.brand.created";
    public const string BrandUpdated = "catalog.brand.updated";
}
