using System.ComponentModel.DataAnnotations;

namespace Catalog.Application.DTOs;

public record CreateProductDto
{
    [Required(ErrorMessage = "Ürün adı zorunludur")]
    [StringLength(500, ErrorMessage = "Ürün adı en fazla 500 karakter olabilir")]
    public string Name { get; init; } = default!;
    
    [Required(ErrorMessage = "Slug zorunludur")]
    [StringLength(550, ErrorMessage = "Slug en fazla 550 karakter olabilir")]
    public string Slug { get; init; } = default!;
    
    [Required(ErrorMessage = "Marka seçimi zorunludur")]
    public Guid BrandId { get; init; }
    
    public Guid? CategoryId { get; init; }
    
    [Required(ErrorMessage = "Fiyat zorunludur")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
    public decimal Price { get; init; }
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Orijinal fiyat 0'dan büyük olmalıdır")]
    public decimal? OriginalPrice { get; init; }
    
    [StringLength(10000, ErrorMessage = "Açıklama en fazla 10000 karakter olabilir")]
    public string? Description { get; init; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Stok miktarı 0 veya daha büyük olmalıdır")]
    public int StockQuantity { get; init; }
    
    public List<string>? Images { get; init; }
    public List<string>? Tags { get; init; }
    
    public bool Featured { get; init; }
    public bool IsCampaign { get; init; }
    
    [Range(0, 100, ErrorMessage = "İndirim oranı 0-100 arasında olmalıdır")]
    public int DiscountPercentage { get; init; }
    
    public DateTimeOffset? CampaignEndDate { get; init; }
}
