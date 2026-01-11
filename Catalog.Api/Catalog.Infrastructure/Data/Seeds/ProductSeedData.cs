using Catalog.Domain.Entities;
using System.Text.Json;

namespace Catalog.Infrastructure.Data.Seeds;

public static class ProductSeedData
{
    public static List<Product> GetProducts(List<Category> categories, List<Brand> brands)
    {
        var products = new List<Product>();
        
        if (!categories.Any() || !brands.Any())
            return products;

        // Her kategori için bir ürün oluştur
        var random = new Random(42); // Deterministic için seed
        
        foreach (var category in categories)
        {
            // Rastgele bir brand seç
            var brand = brands[random.Next(brands.Count)];
            
            var product = CreateProduct(
                name: GetProductNameForCategory(category.Name),
                category: category,
                brand: brand,
                price: GetPriceForCategory(category.Name),
                stockQuantity: random.Next(10, 100)
            );
            
            products.Add(product);
        }

        return products;
    }

    private static Product CreateProduct(string name, Category category, Brand brand, decimal price, int stockQuantity)
    {
        var random = new Random(name.GetHashCode()); // Deterministic random
        
        var slug = GenerateSlug(name);
        var originalPrice = price * (decimal)(1.0 + random.NextDouble() * 0.3); // %0-30 daha pahalı
        var rating = (decimal)(3.5 + random.NextDouble() * 1.5); // 3.5 - 5.0 arası
        var reviewCount = random.Next(10, 500);
        var discountPercentage = random.Next(0, 30);
        var isCampaign = discountPercentage > 15;
        
        var product = new Product
        {
            Name = name,
            Slug = slug,
            BrandId = brand.Id,
            CategoryId = category.Id,
            Price = price,
            OriginalPrice = originalPrice,
            Description = GetDescriptionForProduct(name),
            InStock = stockQuantity > 0,
            StockQuantity = stockQuantity,
            Rating = Math.Round(rating, 1),
            ReviewCount = reviewCount,
            Images = JsonSerializer.Serialize(GetImagesForProduct(name)),
            Tags = JsonSerializer.Serialize(GetTagsForProduct(category.Name)),
            Featured = random.Next(0, 10) < 2, // %20 şans
            IsCampaign = isCampaign,
            DiscountPercentage = discountPercentage,
            CampaignEndDate = isCampaign ? DateTimeOffset.UtcNow.AddDays(random.Next(7, 30)) : null,
            ProductSource = "own",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Set Id using reflection
        var categoryId = CreateDeterministicGuid($"Product.{name}");
        var idProperty = typeof(Product).BaseType?.GetProperty("Id", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (idProperty != null && idProperty.CanWrite)
        {
            var setMethod = idProperty.GetSetMethod(nonPublic: true);
            setMethod?.Invoke(product, new object[] { categoryId });
        }

        return product;
    }

    private static string GetProductNameForCategory(string categoryName)
    {
        return categoryName switch
        {
            "Notebook" => "Dell XPS 15 Laptop",
            "Notebook Aksesuar" => "Laptop Çantası Premium",
            "Tablet" => "iPad Pro 12.9 inç",
            "All In One Pc" => "iMac 24 inç M3",
            "Masaüstü PC" => "Gaming PC RTX 4070",
            "Mini PC" => "Intel NUC 13 Pro",
            "Tablet Aksesuar" => "Tablet Kılıfı ve Stand",
            "Anakartlar" => "ASUS ROG Strix B650E-F",
            "Bellekler" => "Corsair Vengeance 32GB DDR5",
            "Ekran Kartları" => "NVIDIA GeForce RTX 4080",
            "Hard Diskler" => "Samsung 990 PRO 2TB SSD",
            "İşlemciler" => "AMD Ryzen 9 7950X",
            "Bilgisayar Kasası" => "Corsair 5000D Airflow",
            "Optik Sürücüler" => "LG Blu-ray Writer",
            "Soğutma /Overclock" => "Noctua NH-D15 CPU Cooler",
            "Diğer Kartlar" => "ASUS WiFi 6E PCIe Card",
            "Kart Okuyucular" => "USB 3.0 Multi Card Reader",
            "Kablolar ve Adaptörler" => "HDMI 2.1 Kablosu 3m",
            "Monitörler" => "LG UltraGear 27GP850",
            "Kesintisiz Enerji" => "APC Back-UPS 1500VA",
            "Klavye" => "Logitech MX Keys",
            "Mouse" => "Logitech MX Master 3S",
            "Hoparlör" => "Logitech Z623 2.1",
            "Kulaklık ve Mikrofon" => "SteelSeries Arctis 7P",
            "Web Kamerası" => "Logitech C920 HD Pro",
            "Usb Bellekler" => "SanDisk Ultra 128GB USB 3.0",
            "Akıllı Tahta" => "Samsung Flip 2 55 inç",
            "Monitör Aksesuar" => "Monitor Standı Ayarlanabilir",
            "Klavye & Mouse Set" => "Logitech MK850 Set",
            "Mouse Pad" => "SteelSeries QcK Gaming Mousepad",
            "Lazer Yazıcılar" => "HP LaserJet Pro M404dn",
            "Mürekkep Püskürtmeli Yazıcılar" => "Canon PIXMA G3020",
            "Nokta Vuruşlu Yazıcı" => "Epson FX-2190",
            "Tarayıcı" => "Canon CanoScan LiDE 400",
            "Yazıcı Aksesuar" => "HP 305 Mürekkep Kartuşu Set",
            "Sunucular" => "Dell PowerEdge R750 Server",
            "İş İstasyonları" => "HP Z8 G5 Workstation",
            "Sunucu Yazılımları" => "Windows Server 2022 Standard",
            "Depolama Üniteleri" => "Synology DS920+ NAS",
            "Güvenlik Duvarı (Firewall)" => "Fortinet FortiGate 60F",
            "Sunucu Aksesuarları" => "Server Rack 42U",
            "Modem" => "TP-Link Archer AX50",
            "Access Point" => "Ubiquiti UniFi AP AC Pro",
            "Routerler" => "ASUS RT-AX88U WiFi 6",
            "Menzil Arttırıcı" => "TP-Link RE650 WiFi Extender",
            "Kablosuz Ağ Ürünleri" => "Netgear Nighthawk WiFi 6",
            "Switch" => "TP-Link TL-SG1024 24 Port",
            "Powerline Ürünleri" => "TP-Link AV2000 Powerline",
            "Ağ Kartları" => "Intel Ethernet I225-V PCIe",
            "Controller" => "Ubiquiti UniFi Controller Cloud Key",
            "Kablolar" => "Cat6 Ethernet Kablosu 10m",
            "Ağ Aksesuar" => "RJ45 Connector Set",
            "Antenler ve Aksesuarları" => "WiFi Anteni 9dBi",
            "KVM Switch" => "IOGEAR 4 Port KVM Switch",
            "Bluetooth Ürünleri" => "Bluetooth 5.0 USB Adaptör",
            "Cep Telefonu" => "Samsung Galaxy S24 Ultra",
            "Cep Telefonu Aksesuarları" => "Telefon Kılıfı ve Ekran Koruyucu",
            "Kablolu Okuyucu" => "Honeywell 1900 Barcode Scanner",
            "Kablosuz Okuyucu" => "Zebra DS8178 Wireless Scanner",
            "Karekod Okuyucu" => "Datalogic QuickScan QD2430",
            "El Terminali" => "Zebra TC21 Mobile Computer",
            "Masaüstü Okuyucu" => "Symbol LS2208 Desktop Scanner",
            "Barkod Yazıcıları" => "Zebra ZD420 Thermal Printer",
            "Pos ve Slip Yazıcılar" => "Epson TM-T20II POS Printer",
            "Teraziler" => "Mettler Toledo ID7 Scale",
            "Dokunmatik POS PC" => "Elo Touchscreen POS Terminal",
            "IP / AHD Kameralar" => "Hikvision DS-2CD2043G0-I 4MP",
            "NVR / AHD Kayıt Cihazları" => "Hikvision DS-7608NI-I2 NVR",
            "Hırsız Alarm Sistemleri" => "Paradox SP5500 Alarm Sistemi",
            "İnterkom Sistemleri" => "Aiphone GT-1C Intercom",
            "Geçiş Kontrol Sistemleri" => "HID ProxCard Reader",
            "CCTV Kablo" => "RG59 Coaxial Cable 100m",
            "Switch / Adaptör / AP" => "PoE Switch 8 Port",
            "Sarf Malzeme ve Aksesuarlar" => "CCTV Lens Set",
            "Güvenlik Monitörleri" => "Samsung 32 inç Security Monitor",
            "Mobil CCTV Ürünleri" => "4G LTE Security Camera",
            "CCTV IP Set" => "8 Kamera IP Güvenlik Seti",
            "CCTV AHD Set" => "4 Kamera AHD Güvenlik Seti",
            "Termal ve Solar Kameralar" => "Hikvision Thermal Camera",
            "Araç İçi Kamera" => "Vehicle Dash Camera 4K",
            "Aksesuar ve Sarf Malzemeler" => "CCTV Mounting Bracket",
            "Solar Panel" => "Solar Panel 100W CCTV",
            "İnterkom Setler" => "Aiphone 2 Station Set",
            "Kameralı Kapı Zili" => "Ring Video Doorbell Pro",
            "Seslendirme ve Acil Anons" => "Bosch Praesideo PA System",
            "Yangın Algılama Sistemleri" => "Honeywell Fire Alarm Panel",
            "Televizyonlar" => "Samsung 55 inç QLED TV",
            "Projeksiyon Cihazları" => "Epson Home Cinema 3800",
            "Klimalar" => "Daikin Inverter Klima 12000 BTU",
            "Süpürge" => "Dyson V15 Detect",
            "Akıllı Saat" => "Apple Watch Series 9",
            "Aksiyon Kameraları" => "GoPro Hero 12",
            "Kart Bellekler" => "SanDisk Extreme 256GB SD Card",
            "Robot Süpürge" => "iRobot Roomba j7+",
            "Televizyon Aksesuar" => "TV Duvar Montajı",
            "Projeksiyon Aksesuarları" => "Projeksiyon Perdesi 100 inç",
            "Mikrodalga Fırın" => "Samsung 23L Mikrodalga",
            "Küçük Mutfak Aletleri" => "Tefal Blender Set",
            "Vantilatör" => "Dyson Pure Cool Fan",
            "Drone Multikopter" => "DJI Mini 4 Pro",
            "Dijital Fotoğraf Makineleri" => "Canon EOS R6 Mark II",
            "Kişisel Bakım" => "Philips Sonicare Diş Fırçası",
            "Akıllı Ev Yaşam Ürünleri" => "Philips Hue Smart Light Set",
            "Yazıcı Kartuşları" => "HP 305 Original Ink Cartridge",
            "Yazılımlar" => "Microsoft Office 2021",
            "Outlet" => "Outlet Ürünü - Özel Fiyat",
            _ => $"{categoryName} Ürünü"
        };
    }

    private static decimal GetPriceForCategory(string categoryName)
    {
        return categoryName switch
        {
            "Notebook" => 25000m,
            "Tablet" => 15000m,
            "All In One Pc" => 35000m,
            "Masaüstü PC" => 40000m,
            "Ekran Kartları" => 30000m,
            "İşlemciler" => 15000m,
            "Monitörler" => 8000m,
            "Klavye" => 1500m,
            "Mouse" => 1200m,
            "Lazer Yazıcılar" => 5000m,
            "Sunucular" => 50000m,
            "Routerler" => 3000m,
            "Cep Telefonu" => 35000m,
            "Barkod Yazıcıları" => 4000m,
            "IP / AHD Kameralar" => 2000m,
            "Televizyonlar" => 25000m,
            "Klimalar" => 18000m,
            "Akıllı Saat" => 8000m,
            "Drone Multikopter" => 20000m,
            "Dijital Fotoğraf Makineleri" => 45000m,
            "Yazılımlar" => 3000m,
            _ => 1000m
        };
    }

    private static string GetDescriptionForProduct(string productName)
    {
        return $"{productName} - Yüksek kaliteli, dayanıklı ve performanslı ürün. Müşteri memnuniyeti garantili. Hızlı kargo ve güvenli ödeme seçenekleri ile kapınızda.";
    }

    private static List<string> GetImagesForProduct(string productName)
    {
        return new List<string>
        {
            $"https://example.com/images/{GenerateSlug(productName)}-1.jpg",
            $"https://example.com/images/{GenerateSlug(productName)}-2.jpg",
            $"https://example.com/images/{GenerateSlug(productName)}-3.jpg"
        };
    }

    private static List<string> GetTagsForProduct(string categoryName)
    {
        var tags = new List<string> { categoryName };
        
        if (categoryName.Contains("Gaming") || categoryName.Contains("Oyun"))
            tags.Add("Gaming");
        if (categoryName.Contains("Pro") || categoryName.Contains("Professional"))
            tags.Add("Professional");
        if (categoryName.Contains("Wireless") || categoryName.Contains("Kablosuz"))
            tags.Add("Wireless");
        
        return tags;
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

