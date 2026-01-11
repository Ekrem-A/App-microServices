using Catalog.Domain.Entities;

namespace Catalog.Infrastructure.Data.Seeds;

public static class BrandSeedData
{
    public static List<Brand> GetBrands()
    {
        var brands = new List<Brand>
        {
            CreateBrand("Apple", "Apple Inc. teknoloji ürünleri", "https://example.com/logos/apple.png", "https://www.apple.com"),
            CreateBrand("Samsung", "Samsung elektronik ürünleri", "https://example.com/logos/samsung.png", "https://www.samsung.com"),
            CreateBrand("Dell", "Dell bilgisayar ve sunucu çözümleri", "https://example.com/logos/dell.png", "https://www.dell.com"),
            CreateBrand("HP", "HP yazıcı ve bilgisayar ürünleri", "https://example.com/logos/hp.png", "https://www.hp.com"),
            CreateBrand("Lenovo", "Lenovo laptop ve masaüstü bilgisayarlar", "https://example.com/logos/lenovo.png", "https://www.lenovo.com"),
            CreateBrand("ASUS", "ASUS gaming ve bilgisayar bileşenleri", "https://example.com/logos/asus.png", "https://www.asus.com"),
            CreateBrand("Logitech", "Logitech çevre birimleri", "https://example.com/logos/logitech.png", "https://www.logitech.com"),
            CreateBrand("Canon", "Canon yazıcı ve kamera ürünleri", "https://example.com/logos/canon.png", "https://www.canon.com"),
            CreateBrand("Epson", "Epson yazıcı ve projeksiyon cihazları", "https://example.com/logos/epson.png", "https://www.epson.com"),
            CreateBrand("TP-Link", "TP-Link ağ ürünleri", "https://example.com/logos/tplink.png", "https://www.tp-link.com"),
            CreateBrand("Hikvision", "Hikvision güvenlik kameraları", "https://example.com/logos/hikvision.png", "https://www.hikvision.com"),
            CreateBrand("Zebra", "Zebra barkod yazıcıları", "https://example.com/logos/zebra.png", "https://www.zebra.com"),
            CreateBrand("Microsoft", "Microsoft yazılım ve donanım", "https://example.com/logos/microsoft.png", "https://www.microsoft.com"),
            CreateBrand("Intel", "Intel işlemci ve donanım", "https://example.com/logos/intel.png", "https://www.intel.com"),
            CreateBrand("AMD", "AMD işlemci ve ekran kartları", "https://example.com/logos/amd.png", "https://www.amd.com"),
            CreateBrand("NVIDIA", "NVIDIA ekran kartları", "https://example.com/logos/nvidia.png", "https://www.nvidia.com"),
            CreateBrand("Corsair", "Corsair gaming donanımları", "https://example.com/logos/corsair.png", "https://www.corsair.com"),
            CreateBrand("SteelSeries", "SteelSeries gaming aksesuarları", "https://example.com/logos/steelseries.png", "https://www.steelseries.com"),
            CreateBrand("SanDisk", "SanDisk bellek kartları", "https://example.com/logos/sandisk.png", "https://www.sandisk.com"),
            CreateBrand("LG", "LG monitör ve elektronik ürünler", "https://example.com/logos/lg.png", "https://www.lg.com")
        };

        return brands;
    }

    private static Brand CreateBrand(string name, string description, string logoUrl, string websiteUrl)
    {
        var brand = new Brand
        {
            Name = name,
            Slug = GenerateSlug(name),
            Description = description,
            LogoUrl = logoUrl,
            WebsiteUrl = websiteUrl,
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Set Id using reflection
        var brandId = CreateDeterministicGuid($"Brand.{name}");
        var idProperty = typeof(Brand).BaseType?.GetProperty("Id", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (idProperty != null && idProperty.CanWrite)
        {
            var setMethod = idProperty.GetSetMethod(nonPublic: true);
            setMethod?.Invoke(brand, new object[] { brandId });
        }

        return brand;
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace("ı", "i")
            .Replace("ğ", "g")
            .Replace("ü", "u")
            .Replace("ş", "s")
            .Replace("ö", "o")
            .Replace("ç", "c")
            .Replace("İ", "i")
            .Replace("Ğ", "g")
            .Replace("Ü", "u")
            .Replace("Ş", "s")
            .Replace("Ö", "o")
            .Replace("Ç", "c")
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("&", "ve")
            .Replace("(", "")
            .Replace(")", "");

        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        return slug;
    }

    private static Guid CreateDeterministicGuid(string input)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }
    }
}

