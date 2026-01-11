using System.ComponentModel.DataAnnotations;

namespace Catalog.Application.DTOs;

public record CreateBrandDto
{
    [Required(ErrorMessage = "Marka adı zorunludur")]
    [StringLength(200, ErrorMessage = "Marka adı en fazla 200 karakter olabilir")]
    public string Name { get; init; } = default!;
    
    [Required(ErrorMessage = "Slug zorunludur")]
    [StringLength(250, ErrorMessage = "Slug en fazla 250 karakter olabilir")]
    public string Slug { get; init; } = default!;
    
    [StringLength(5000, ErrorMessage = "Açıklama en fazla 5000 karakter olabilir")]
    public string? Description { get; init; }
    
    [StringLength(500, ErrorMessage = "Logo URL en fazla 500 karakter olabilir")]
    [Url(ErrorMessage = "Geçerli bir URL giriniz")]
    public string? LogoUrl { get; init; }
    
    [StringLength(500, ErrorMessage = "Website URL en fazla 500 karakter olabilir")]
    [Url(ErrorMessage = "Geçerli bir URL giriniz")]
    public string? WebsiteUrl { get; init; }
    
    public bool IsActive { get; init; } = true;
    
    [Range(0, int.MaxValue, ErrorMessage = "Sıra numarası 0 veya daha büyük olmalıdır")]
    public int DisplayOrder { get; init; }
}
