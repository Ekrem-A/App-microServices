# 🛒 E-Commerce Microservices

<div align="center">

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=csharp&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?style=for-the-badge&logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![Kafka](https://img.shields.io/badge/Apache%20Kafka-7.5-231F20?style=for-the-badge&logo=apachekafka&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED?style=for-the-badge&logo=docker&logoColor=white)

**Modern, ölçeklenebilir ve event-driven mimari ile geliştirilmiş e-ticaret mikroservis çözümü**

[Özellikler](#-özellikler) • [Teknolojiler](#-teknolojiler) • [Mimari](#️-mimari) • [Kurulum](#-kurulum) • [API Dokümantasyonu](#-api-dokümantasyonu)

</div>

---

## 📋 İçindekiler

- [Özellikler](#-özellikler)
- [Mikroservisler](#-mikroservisler)
- [Teknolojiler](#-teknolojiler)
- [Mimari ve Patternler](#️-mimari-ve-patternler)
- [Kurulum](#-kurulum)
- [API Dokümantasyonu](#-api-dokümantasyonu)
- [Proje Yapısı](#-proje-yapısı)

---

## ✨ Özellikler

- 🏗️ **Mikroservis Mimarisi** - Bağımsız, ölçeklenebilir servisler
- 🔐 **JWT Authentication** - Güvenli kimlik doğrulama ve yetkilendirme
- 📦 **CQRS Pattern** - Command ve Query sorumluluk ayrımı
- 🎯 **MediatR** - In-process messaging ve request/response pattern
- 📨 **Event-Driven Architecture** - Apache Kafka ile asenkron iletişim
- ⚡ **Redis Cache** - Yüksek performanslı caching
- 🐘 **PostgreSQL** - Güçlü ve güvenilir veri depolama
- 🐳 **Docker Support** - Containerized deployment
- 🚂 **Railway Ready** - Cloud deployment desteği
- 📊 **Health Checks** - Servis sağlık kontrolü
- 📝 **Serilog** - Yapılandırılmış loglama
- 🛡️ **Rate Limiting** - API koruma mekanizması
- ✅ **FluentValidation** - Input validasyonu
- 🗺️ **AutoMapper** - Object-to-object mapping

---

## 🔧 Mikroservisler

| Servis | Port | Açıklama |
|--------|------|----------|
| **Identity.Api** | 5001 | Kullanıcı kayıt, giriş, JWT token yönetimi |
| **Catalog.Api** | 5002 | Ürün, kategori ve marka yönetimi |
| **Cart.Api** | 5003 | Sepet işlemleri, kupon yönetimi |
| **Order.Api** | 5004 | Sipariş oluşturma ve takibi |
| **Payment.Api** | 5005 | Ödeme işlemleri |

---

## 🛠 Teknolojiler

### Backend Framework
| Teknoloji | Versiyon | Açıklama |
|-----------|----------|----------|
| .NET | 8.0 | Ana framework |
| ASP.NET Core | 8.0 | Web API framework |
| Entity Framework Core | 8.0 | ORM |
| C# | 12.0 | Programlama dili |

### Veritabanları & Cache
| Teknoloji | Kullanım |
|-----------|----------|
| PostgreSQL | Ana veritabanı (Identity, Catalog, Order, Payment) |
| Redis | Sepet cache ve session yönetimi |

### Mesajlaşma & Event
| Teknoloji | Kullanım |
|-----------|----------|
| Apache Kafka | Event-driven iletişim |
| Confluent.Kafka | .NET Kafka client |

### Güvenlik & Authentication
| Teknoloji | Kullanım |
|-----------|----------|
| JWT Bearer | Token-based authentication |
| ASP.NET Core Identity | Kullanıcı yönetimi |
| AspNetCoreRateLimit | Rate limiting |

### DevOps & Containerization
| Teknoloji | Kullanım |
|-----------|----------|
| Docker | Containerization |
| Docker Compose | Multi-container orchestration |
| Railway | Cloud deployment |
| Dapr | Distributed application runtime |

### Kütüphaneler & Araçlar
| Kütüphane | Versiyon | Kullanım |
|-----------|----------|----------|
| MediatR | 12.x - 14.x | CQRS & Mediator pattern |
| FluentValidation | 11.x - 12.x | Input validation |
| AutoMapper | 13.x | Object mapping |
| Serilog | 8.x | Structured logging |
| Swashbuckle | 6.x | Swagger/OpenAPI |
| Polly | 8.x | Resilience & fault tolerance |
| StackExchange.Redis | 2.8 | Redis client |

---

## 🏛️ Mimari ve Patternler

### Clean Architecture
```
┌─────────────────────────────────────────────────────┐
│                    Presentation                      │
│                    (API Layer)                       │
├─────────────────────────────────────────────────────┤
│                    Application                       │
│            (Use Cases, CQRS Handlers)               │
├─────────────────────────────────────────────────────┤
│                      Domain                          │
│           (Entities, Aggregates, Events)            │
├─────────────────────────────────────────────────────┤
│                   Infrastructure                     │
│      (Database, Cache, External Services)           │
└─────────────────────────────────────────────────────┘
```

### Uygulanan Design Patterns

| Pattern | Açıklama | Kullanım |
|---------|----------|----------|
| **CQRS** | Command Query Responsibility Segregation | Read/Write operasyonlarının ayrılması |
| **Mediator** | MediatR ile in-process messaging | Handler-based request processing |
| **Repository** | Data access abstraction | Veritabanı işlemleri |
| **Unit of Work** | Transaction management | EF Core ile entegre |
| **Aggregate Root** | DDD pattern | Domain entities (ShoppingCart) |
| **Domain Events** | Event-driven design | Servisler arası iletişim |
| **Options Pattern** | Configuration binding | Strongly-typed settings |
| **Dependency Injection** | IoC container | Built-in .NET DI |
| **Factory Pattern** | Object creation | Entity oluşturma |
| **Decorator Pattern** | Pipeline behaviors | Validation, Logging |

### CQRS Yapısı
```
Application/
├── Commands/
│   ├── CreateProduct/
│   │   ├── CreateProductCommand.cs
│   │   ├── CreateProductCommandHandler.cs
│   │   └── CreateProductCommandValidator.cs
│   └── ...
├── Queries/
│   ├── GetProducts/
│   │   ├── GetProductsQuery.cs
│   │   ├── GetProductsQueryHandler.cs
│   │   └── ProductDto.cs
│   └── ...
└── Common/
    └── Behaviors/
        ├── ValidationBehavior.cs
        └── LoggingBehavior.cs
```

### Event-Driven Architecture
```
┌─────────────┐    Publish    ┌─────────────┐    Subscribe    ┌─────────────┐
│  Cart.Api   │──────────────▶│    Kafka    │◀───────────────│  Order.Api  │
└─────────────┘               └─────────────┘                 └─────────────┘
                                    │
                                    │ Subscribe
                                    ▼
                             ┌─────────────┐
                             │ Payment.Api │
                             └─────────────┘
```

---

## 🚀 Kurulum

### Gereksinimler
- .NET 8.0 SDK
- Docker & Docker Compose
- PostgreSQL (veya Docker ile)
- Redis (veya Docker ile)

### Docker ile Hızlı Başlangıç

```bash
# Repository'yi klonlayın
git clone https://github.com/Ekrem-A/App-microServices.git
cd App-microServices

# Cart servisi için
cd Cart.Api
docker-compose up -d

# Catalog servisi için (yeni terminal)
cd ../Catalog.Api
docker-compose up -d
```

### Manuel Kurulum

```bash
# Her servis için bağımlılıkları yükleyin
cd Cart.Api
dotnet restore
dotnet build

# Migrations uygulayın (Catalog, Identity, Order, Payment için)
cd ../Catalog.Api/Catalog.Infrastructure
dotnet ef database update --startup-project ../Catalog.Api

# Servisi başlatın
cd ../Catalog.Api
dotnet run
```

### Ortam Değişkenleri

```env
# Database
DATABASE_URL=postgres://user:password@localhost:5432/dbname
ConnectionStrings__DefaultConnection=Host=localhost;Database=mydb;Username=user;Password=pass

# Redis
REDIS_URL=redis://localhost:6379

# JWT
JwtSettings__SecretKey=your-super-secret-key-here
JwtSettings__Issuer=identity-api
JwtSettings__Audience=e-commerce-app
JwtSettings__ExpirationInMinutes=60

# Kafka
Kafka__BootstrapServers=localhost:9092

# Railway
PORT=8080
```

---

## 📖 API Dokümantasyonu

Her servis Swagger UI ile dokümante edilmiştir:

| Servis | Swagger URL |
|--------|-------------|
| Identity API | `http://localhost:5001/swagger` |
| Catalog API | `http://localhost:5002/swagger` |
| Cart API | `http://localhost:5003/swagger` |
| Order API | `http://localhost:5004/swagger` |
| Payment API | `http://localhost:5005/swagger` |

### Örnek API Endpoints

#### Identity API
```http
POST /api/auth/register    # Kullanıcı kaydı
POST /api/auth/login       # Giriş & JWT token alma
POST /api/auth/refresh     # Token yenileme
```

#### Catalog API
```http
GET    /api/products           # Ürün listesi
GET    /api/products/{id}      # Ürün detayı
POST   /api/products           # Yeni ürün (Admin)
PUT    /api/products/{id}      # Ürün güncelleme
DELETE /api/products/{id}      # Ürün silme

GET    /api/categories         # Kategori listesi
GET    /api/brands             # Marka listesi
```

#### Cart API
```http
GET    /api/cart               # Sepeti getir
POST   /api/cart/items         # Sepete ürün ekle
PUT    /api/cart/items/{id}    # Ürün miktarı güncelle
DELETE /api/cart/items/{id}    # Üründen kaldır
POST   /api/cart/coupon        # Kupon uygula
DELETE /api/cart               # Sepeti temizle
```

---

## 📁 Proje Yapısı

```
App-microServices/
│
├── Cart.Api/
│   ├── Cart.Api/              # API Layer
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   └── Program.cs
│   ├── Cart.Application/      # Application Layer
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── DTOs/
│   ├── Cart.Domain/           # Domain Layer
│   │   ├── CartAggregate/
│   │   └── Exceptions/
│   ├── Cart.Infrastructure/   # Infrastructure Layer
│   │   ├── Persistence/
│   │   └── Messaging/
│   └── docker-compose.yml
│
├── Catalog.Api/
│   ├── Catalog.Api/
│   ├── Catalog.Application/
│   │   └── Features/
│   │       ├── Products/
│   │       ├── Categories/
│   │       └── Brands/
│   ├── Catalog.Domain/
│   └── Catalog.Infrastructure/
│       ├── Data/
│       ├── Repositories/
│       └── Cache/
│
├── Identity.Api/
│   ├── Identity.Api/
│   ├── Identity.Application/
│   │   └── DTOs/
│   ├── Identity.Domain/
│   │   └── Entities/
│   └── Identity.Infrastructure/
│       ├── Identity/
│       ├── Persistence/
│       └── Services/
│
├── Order.Api/
│   ├── Order.Api/
│   ├── Order.Application/
│   ├── Order.Domain/
│   └── Order.Infrastructure/
│
└── Payment.Api/
    ├── Payment.Api/
    ├── Payment.Application/
    ├── Payment.Domain/
    └── Payment.Infrastructure/
```

---

## 🔒 Güvenlik Özellikleri

- ✅ JWT Token Authentication
- ✅ Role-based Authorization
- ✅ API Key Validation
- ✅ Rate Limiting (IP-based)
- ✅ Input Validation (FluentValidation)
- ✅ HTTPS Enforcement
- ✅ CORS Configuration
- ✅ SQL Injection Prevention (EF Core)

---

## 📊 Health Checks

Her servis health check endpoint'leri sunar:

```http
GET /health         # Genel sağlık durumu
GET /health/ready   # Readiness probe
GET /health/live    # Liveness probe
```

---

## 🚂 Railway Deployment

Projeler Railway üzerinde deploy edilmeye hazırdır:

1. Railway projesine repository'yi bağlayın
2. Her servis için ayrı bir service oluşturun
3. Ortam değişkenlerini ayarlayın
4. PostgreSQL ve Redis add-on'larını ekleyin


## 👤 Geliştirici

**Ekrem-A**

- GitHub: [@Ekrem-A](https://github.com/Ekrem-A)

---

<div align="center">

⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın!

</div>
