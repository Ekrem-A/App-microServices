using Catalog.Domain.Entities;

namespace Catalog.Infrastructure.Data.Seeds;

public static class CategorySeedData
{
    public static List<Category> GetCategories()
    {
        var categories = new List<Category>();
        int displayOrder = 0;

        // Kişisel Bilgisayarlar
        var kisiselBilgisayarlar = CreateCategory("Kişisel Bilgisayarlar", null, displayOrder++);
        categories.Add(kisiselBilgisayarlar);
        categories.AddRange(new[]
        {
            CreateCategory("Notebook", kisiselBilgisayarlar.Id, displayOrder++),
            CreateCategory("Notebook Aksesuar", kisiselBilgisayarlar.Id, displayOrder++),
            CreateCategory("Tablet", kisiselBilgisayarlar.Id, displayOrder++),
            CreateCategory("All In One Pc", kisiselBilgisayarlar.Id, displayOrder++),
            CreateCategory("Masaüstü PC", kisiselBilgisayarlar.Id, displayOrder++),
            CreateCategory("Mini PC", kisiselBilgisayarlar.Id, displayOrder++),
            CreateCategory("Tablet Aksesuar", kisiselBilgisayarlar.Id, displayOrder++)
        });

        // Bilgisayar Bileşenleri
        var bilgisayarBilesenleri = CreateCategory("Bilgisayar Bileşenleri", null, displayOrder++);
        categories.Add(bilgisayarBilesenleri);
        categories.AddRange(new[]
        {
            CreateCategory("Anakartlar", bilgisayarBilesenleri.Id, displayOrder++),
            CreateCategory("Bellekler", bilgisayarBilesenleri.Id, displayOrder++),
            CreateCategory("Ekran Kartları", bilgisayarBilesenleri.Id, displayOrder++),
            CreateCategory("Hard Diskler", bilgisayarBilesenleri.Id, displayOrder++),
            CreateCategory("İşlemciler", bilgisayarBilesenleri.Id, displayOrder++),
            CreateCategory("Bilgisayar Kasası", bilgisayarBilesenleri.Id, displayOrder++),
            CreateCategory("Optik Sürücüler", bilgisayarBilesenleri.Id, displayOrder++),
            CreateCategory("Soğutma /Overclock", bilgisayarBilesenleri.Id, displayOrder++),
            CreateCategory("Diğer Kartlar", bilgisayarBilesenleri.Id, displayOrder++),
            CreateCategory("Kart Okuyucular", bilgisayarBilesenleri.Id, displayOrder++),
            CreateCategory("Kablolar ve Adaptörler", bilgisayarBilesenleri.Id, displayOrder++)
        });

        // Çevre Birimleri
        var cevreBirimleri = CreateCategory("Çevre Birimleri", null, displayOrder++);
        categories.Add(cevreBirimleri);
        categories.AddRange(new[]
        {
            CreateCategory("Monitörler", cevreBirimleri.Id, displayOrder++),
            CreateCategory("Kesintisiz Enerji", cevreBirimleri.Id, displayOrder++),
            CreateCategory("Klavye", cevreBirimleri.Id, displayOrder++),
            CreateCategory("Mouse", cevreBirimleri.Id, displayOrder++),
            CreateCategory("Hoparlör", cevreBirimleri.Id, displayOrder++),
            CreateCategory("Kulaklık ve Mikrofon", cevreBirimleri.Id, displayOrder++),
            CreateCategory("Web Kamerası", cevreBirimleri.Id, displayOrder++),
            CreateCategory("Usb Bellekler", cevreBirimleri.Id, displayOrder++),
            CreateCategory("Akıllı Tahta", cevreBirimleri.Id, displayOrder++),
            CreateCategory("Monitör Aksesuar", cevreBirimleri.Id, displayOrder++),
            CreateCategory("Klavye & Mouse Set", cevreBirimleri.Id, displayOrder++),
            CreateCategory("Mouse Pad", cevreBirimleri.Id, displayOrder++)
        });

        // Baskı Çözümleri
        var baskiCozumleri = CreateCategory("Baskı Çözümleri", null, displayOrder++);
        categories.Add(baskiCozumleri);
        categories.AddRange(new[]
        {
            CreateCategory("Lazer Yazıcılar", baskiCozumleri.Id, displayOrder++),
            CreateCategory("Mürekkep Püskürtmeli Yazıcılar", baskiCozumleri.Id, displayOrder++),
            CreateCategory("Nokta Vuruşlu Yazıcı", baskiCozumleri.Id, displayOrder++),
            CreateCategory("Tarayıcı", baskiCozumleri.Id, displayOrder++),
            CreateCategory("Yazıcı Aksesuar", baskiCozumleri.Id, displayOrder++)
        });

        // Kurumsal Ürünler
        var kurumsalUrunler = CreateCategory("Kurumsal Ürünler", null, displayOrder++);
        categories.Add(kurumsalUrunler);
        categories.AddRange(new[]
        {
            CreateCategory("Sunucular", kurumsalUrunler.Id, displayOrder++),
            CreateCategory("İş İstasyonları", kurumsalUrunler.Id, displayOrder++),
            CreateCategory("Sunucu Yazılımları", kurumsalUrunler.Id, displayOrder++),
            CreateCategory("Depolama Üniteleri", kurumsalUrunler.Id, displayOrder++),
            CreateCategory("Güvenlik Duvarı (Firewall)", kurumsalUrunler.Id, displayOrder++),
            CreateCategory("Sunucu Aksesuarları", kurumsalUrunler.Id, displayOrder++)
        });

        // Ağ Ürünleri
        var agUrunleri = CreateCategory("Ağ Ürünleri", null, displayOrder++);
        categories.Add(agUrunleri);
        categories.AddRange(new[]
        {
            CreateCategory("Modem", agUrunleri.Id, displayOrder++),
            CreateCategory("Access Point", agUrunleri.Id, displayOrder++),
            CreateCategory("Routerler", agUrunleri.Id, displayOrder++),
            CreateCategory("Menzil Arttırıcı", agUrunleri.Id, displayOrder++),
            CreateCategory("Kablosuz Ağ Ürünleri", agUrunleri.Id, displayOrder++),
            CreateCategory("Switch", agUrunleri.Id, displayOrder++),
            CreateCategory("Powerline Ürünleri", agUrunleri.Id, displayOrder++),
            CreateCategory("Ağ Kartları", agUrunleri.Id, displayOrder++),
            CreateCategory("Controller", agUrunleri.Id, displayOrder++),
            CreateCategory("Kablolar", agUrunleri.Id, displayOrder++),
            CreateCategory("Ağ Aksesuar", agUrunleri.Id, displayOrder++),
            CreateCategory("Antenler ve Aksesuarları", agUrunleri.Id, displayOrder++),
            CreateCategory("KVM Switch", agUrunleri.Id, displayOrder++),
            CreateCategory("Bluetooth Ürünleri", agUrunleri.Id, displayOrder++)
        });

        // Telefonlar
        var telefonlar = CreateCategory("Telefonlar", null, displayOrder++);
        categories.Add(telefonlar);
        categories.AddRange(new[]
        {
            CreateCategory("Cep Telefonu", telefonlar.Id, displayOrder++),
            CreateCategory("Cep Telefonu Aksesuarları", telefonlar.Id, displayOrder++)
        });

        // Barkod Ürünleri
        var barkodUrunleri = CreateCategory("Barkod Ürünleri", null, displayOrder++);
        categories.Add(barkodUrunleri);
        categories.AddRange(new[]
        {
            CreateCategory("Kablolu Okuyucu", barkodUrunleri.Id, displayOrder++),
            CreateCategory("Kablosuz Okuyucu", barkodUrunleri.Id, displayOrder++),
            CreateCategory("Karekod Okuyucu", barkodUrunleri.Id, displayOrder++),
            CreateCategory("El Terminali", barkodUrunleri.Id, displayOrder++),
            CreateCategory("Masaüstü Okuyucu", barkodUrunleri.Id, displayOrder++),
            CreateCategory("Barkod Yazıcıları", barkodUrunleri.Id, displayOrder++),
            CreateCategory("Pos ve Slip Yazıcılar", barkodUrunleri.Id, displayOrder++),
            CreateCategory("Teraziler", barkodUrunleri.Id, displayOrder++),
            CreateCategory("Dokunmatik POS PC", barkodUrunleri.Id, displayOrder++)
        });

        // Güvenlik Ürünleri
        var guvenlikUrunleri = CreateCategory("Güvenlik Ürünleri", null, displayOrder++);
        categories.Add(guvenlikUrunleri);
        categories.AddRange(new[]
        {
            CreateCategory("IP / AHD Kameralar", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("NVR / AHD Kayıt Cihazları", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Hırsız Alarm Sistemleri", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("İnterkom Sistemleri", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Geçiş Kontrol Sistemleri", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("CCTV Kablo", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Switch / Adaptör / AP", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Sarf Malzeme ve Aksesuarlar", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Güvenlik Monitörleri", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Mobil CCTV Ürünleri", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("CCTV IP Set", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("CCTV AHD Set", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Termal ve Solar Kameralar", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Araç İçi Kamera", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Aksesuar ve Sarf Malzemeler", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Solar Panel", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("İnterkom Setler", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Kameralı Kapı Zili", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Seslendirme ve Acil Anons", guvenlikUrunleri.Id, displayOrder++),
            CreateCategory("Yangın Algılama Sistemleri", guvenlikUrunleri.Id, displayOrder++)
        });

        // Tüketici Elektroniği
        var tuketiciElektronik = CreateCategory("Tüketici Elektroniği", null, displayOrder++);
        categories.Add(tuketiciElektronik);
        categories.AddRange(new[]
        {
            CreateCategory("Televizyonlar", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Projeksiyon Cihazları", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Klimalar", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Süpürge", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Akıllı Saat", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Aksiyon Kameraları", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Kart Bellekler", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Robot Süpürge", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Televizyon Aksesuar", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Projeksiyon Aksesuarları", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Mikrodalga Fırın", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Küçük Mutfak Aletleri", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Vantilatör", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Drone Multikopter", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Dijital Fotoğraf Makineleri", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Kişisel Bakım", tuketiciElektronik.Id, displayOrder++),
            CreateCategory("Akıllı Ev Yaşam Ürünleri", tuketiciElektronik.Id, displayOrder++)
        });

        // Ofis & Tüketim Ürünleri
        var ofisTuketim = CreateCategory("Ofis & Tüketim Ürünleri", null, displayOrder++);
        categories.Add(ofisTuketim);
        categories.Add(CreateCategory("Yazıcı Kartuşları", ofisTuketim.Id, displayOrder++));

        // Yazılımlar
        categories.Add(CreateCategory("Yazılımlar", null, displayOrder++));

        // Outlet
        categories.Add(CreateCategory("Outlet", null, displayOrder++));

        return categories;
    }

    private static Category CreateCategory(string name, Guid? parentId, int displayOrder, Guid? id = null)
    {
        // Create a deterministic GUID from the name for consistent seeding
        var categoryId = id ?? CreateDeterministicGuid(name);
        
        var category = new Category
        {
            Name = name,
            Slug = GenerateSlug(name),
            ParentId = parentId,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Set Id using reflection since it's protected set
        // Need to use NonPublic flag to access protected setter
        var idProperty = typeof(Category).BaseType?.GetProperty("Id", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (idProperty != null && idProperty.CanWrite)
        {
            var setMethod = idProperty.GetSetMethod(nonPublic: true);
            setMethod?.Invoke(category, new object[] { categoryId });
        }
        
        return category;
    }

    private static Guid CreateDeterministicGuid(string input)
    {
        // Create a deterministic GUID from string input
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes("Catalog.Category." + input));
            return new Guid(hash);
        }
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

        // Remove any non-alphanumeric characters except hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        return slug;
    }
}

