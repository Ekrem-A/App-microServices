using AutoMapper;
using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using System.Text.Json;

namespace Catalog.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ========================================
        // Product Mappings
        // ========================================
        
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.Name : null))
            .ForMember(dest => dest.BrandLogoUrl, opt => opt.MapFrom(src => src.Brand != null ? src.Brand.LogoUrl : null))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.Images, opt => opt.MapFrom<ImageListResolver>())
            .ForMember(dest => dest.Tags, opt => opt.MapFrom<TagListResolver>());

        CreateMap<CreateProductDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Brand, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.InStock, opt => opt.MapFrom(src => src.StockQuantity > 0))
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.Rating, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewCount, opt => opt.Ignore())
            .ForMember(dest => dest.ProductSource, opt => opt.MapFrom(src => "own"))
            .ForMember(dest => dest.LastSyncedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateProductDto, Product>()
            .ForMember(dest => dest.Brand, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.InStock, opt => opt.MapFrom(src => src.StockQuantity > 0))
            .ForMember(dest => dest.Images, opt => opt.Ignore())
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.Rating, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewCount, opt => opt.Ignore())
            .ForMember(dest => dest.ProductSource, opt => opt.Ignore())
            .ForMember(dest => dest.LastSyncedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // ========================================
        // Brand Mappings
        // ========================================
        
        CreateMap<Brand, BrandDto>()
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products != null ? src.Products.Count : 0));

        CreateMap<CreateBrandDto, Brand>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateBrandDto, Brand>()
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // ========================================
        // Category Mappings
        // ========================================
        
        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products != null ? src.Products.Count : 0))
            .ForMember(dest => dest.Level, opt => opt.MapFrom(src => CalculateCategoryLevel(src)))
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null))
            .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId));

        CreateMap<CreateCategoryDto, Category>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateCategoryDto, Category>()
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }

    private static string? CalculateCategoryLevel(Category category)
    {
        if (category.ParentId == null)
            return "1";
        
        int level = 1;
        var current = category.Parent;
        while (current != null)
        {
            level++;
            current = current.Parent;
        }
        return level.ToString();
    }
}

// ========================================
// Custom Value Resolvers
// ========================================

/// <summary>
/// Resolves JSON string to List<string> for Images
/// </summary>
public class ImageListResolver : IValueResolver<Product, ProductDto, List<string>?>
{
    public List<string>? Resolve(Product source, ProductDto destination, List<string>? destMember, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(source.Images))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(source.Images);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Resolves JSON string to List<string> for Tags
/// </summary>
public class TagListResolver : IValueResolver<Product, ProductDto, List<string>?>
{
    public List<string>? Resolve(Product source, ProductDto destination, List<string>? destMember, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(source.Tags))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(source.Tags);
        }
        catch
        {
            return null;
        }
    }
}
