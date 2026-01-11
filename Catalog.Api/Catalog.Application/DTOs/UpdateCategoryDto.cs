using System.ComponentModel.DataAnnotations;

namespace Catalog.Application.DTOs;

public record UpdateCategoryDto
{
    [Required]
    public Guid Id { get; init; }
    
    [Required(ErrorMessage = "Kategori adı zorunludur")]
    [StringLength(200, ErrorMessage = "Kategori adı en fazla 200 karakter olabilir")]
    public string Name { get; init; } = default!;
    
    [Required(ErrorMessage = "Slug zorunludur")]
    [StringLength(250, ErrorMessage = "Slug en fazla 250 karakter olabilir")]
    public string Slug { get; init; } = default!;
    
    [StringLength(50, ErrorMessage = "Seviye en fazla 50 karakter olabilir")]
    public string? Level { get; init; }
    
    public Guid? ParentId { get; init; }
    
    [StringLength(200, ErrorMessage = "Üst kategori adı en fazla 200 karakter olabilir")]
    public string? ParentName { get; init; }
    
    [StringLength(5000, ErrorMessage = "Açıklama en fazla 5000 karakter olabilir")]
    public string? Description { get; init; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Sıra numarası 0 veya daha büyük olmalıdır")]
    public int DisplayOrder { get; init; }
}
