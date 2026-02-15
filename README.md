# 🛒 E-Commerce Microservices

<div align="center">

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=csharp&logoColor=white)
![MSSQL](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![Kafka](https://img.shields.io/badge/Apache%20Kafka-7.5-231F20?style=for-the-badge&logo=apachekafka&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Elasticsearch](https://img.shields.io/badge/Elasticsearch-7.17-005571?style=for-the-badge&logo=elasticsearch&logoColor=white)
![Grafana](https://img.shields.io/badge/Grafana-10.3-F46800?style=for-the-badge&logo=grafana&logoColor=white)
![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-1.9-7B5EA7?style=for-the-badge&logo=opentelemetry&logoColor=white)

**Modern, ölçeklenebilir ve event-driven mimari ile geliştirilmiş e-ticaret mikroservis çözümü**

[Özellikler](#-özellikler) • [Teknolojiler](#-teknolojiler) • [Mimari](#️-mimari) • [Kurulum](#-kurulum) • [Observability](#-observability) • [API Dokümantasyonu](#-api-dokümantasyonu)

</div>

---

## 📋 İçindekiler

- [Özellikler](#-özellikler)
- [Mikroservisler](#-mikroservisler)
- [Teknolojiler](#-teknolojiler)
- [Mimari ve Patternler](#️-mimari-ve-patternler)
- [Kurulum](#-kurulum)
- [Observability](#-observability)
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
- �️ **MSSQL Server 2022** - Güçlü ve güvenilir veri depolama
- 🌐 **API Gateway** - YARP reverse proxy ile merkezi giriş noktası
- 📡 **OpenTelemetry** - Distributed tracing, metrics ve logging
- 📊 **Elastic Stack** - Elasticsearch + Kibana + APM Server ile observability
- 📈 **Prometheus + Grafana** - Metrik toplama ve görselleştirme
- 🐳 **Docker Support** - Containerized deployment
- ☸️ **Kubernetes Ready** - K8s deployment desteği
- 📋 **Health Checks** - Servis sağlık kontrolü
- 📝 **Serilog** - Yapılandırılmış loglama
- 🛡️ **Rate Limiting** - API koruma mekanizması
- ✅ **FluentValidation** - Input validasyonu
- 🗺️ **AutoMapper** - Object-to-object mapping

---

## 🔧 Mikroservisler

| Servis | Port | Veritabanı | Açıklama |
|--------|------|------------|----------|
| **Gateway.Api** | 5000 | - | YARP reverse proxy, merkezi API giriş noktası |
| **Catalog.Api** | 5001 | MSSQL :1434 | Ürün, kategori ve marka yönetimi |
| **Identity.Api** | 5002 | MSSQL :1435 | Kullanıcı kayıt, giriş, JWT token yönetimi |
| **Order.Api** | 5003 | MSSQL :1436 | Sipariş oluşturma ve takibi |
| **Payment.Api** | 5004 | MSSQL :1437 | Ödeme işlemleri |
| **Cart.Api** | 5005 | Redis :6381 | Sepet işlemleri, kupon yönetimi |

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
| Teknoloji | Versiyon | Kullanım |
|-----------|----------|----------|
| MSSQL Server | 2022 | Ana veritabanı (Catalog, Identity, Order, Payment) |
| Redis | 7-alpine | Sepet (Cart.Api) ve cache (Catalog.Api) |

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

### API Gateway
| Teknoloji | Versiyon | Kullanım |
|-----------|----------|----------|
| YARP | 2.1.0 | Reverse proxy, routing |

### Observability
| Teknoloji | Versiyon | Kullanım |
|-----------|----------|----------|
| OpenTelemetry | 1.9.0 | Distributed tracing, metrics, logging |
| OTEL Collector | 0.96.0 | Telemetri toplama ve yönlendirme |
| Elasticsearch | 7.17.18 | Trace ve metrik depolama |
| Kibana | 7.17.18 | APM görselleştirme |
| APM Server | 7.17.18 | OTLP → Elasticsearch dönüşümü |
| Prometheus | 2.49.1 | Metrik toplama |
| Grafana | 10.3.1 | Dashboard ve metrik görselleştirme |

### DevOps & Containerization
| Teknoloji | Kullanım |
|-----------|----------|
| Docker | Containerization |
| Docker Compose | Multi-container orchestration (profiles ile) |
| Kubernetes | Production deployment & orchestration |
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

### Observability Architecture
```
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│  Gateway.Api  │   │  Catalog.Api  │   │ Identity.Api  │
└──────┬───────┘   └──────┬───────┘   └──────┬───────┘
       │                    │                    │
       └────────────────────┼────────────────────┘
                             │ OTLP (gRPC :4317)
                    ┌────────▼────────┐
                    │  OTEL Collector   │
                    └────┬───────┬─────┘
                         │            │
               ┌────────▼───┐   ┌───▼────────┐
               │  APM Server  │   │  Prometheus  │
               └──────┬──────┘   └───┬────────┘
                      │                  │
              ┌───────▼──────┐   ┌────▼───────┐
              │ Elasticsearch │   │   Grafana   │
              └──────┬───────┘   └────────────┘
                     │
              ┌──────▼───────┐
              │    Kibana     │
              └──────────────┘
```

---

## 🚀 Kurulum

### Gereksinimler
- .NET 8.0 SDK
- Docker & Docker Compose
- En az 8 GB RAM (önerilen, observability stack için)

### Docker ile Hızlı Başlangıç

```bash
# Repository'yi klonlayın
git clone https://github.com/Ekrem-A/App-microServices.git
cd App-microServices

# Tüm servisleri başlatın (API + Veritabanları + Redis)
docker compose up -d

# Observability stack ile birlikte başlatın (Elasticsearch, Kibana, APM, Prometheus, Grafana)
docker compose --profile observability up -d
```

### Servis Erişim Noktaları

| Servis | URL | Açıklama |
|--------|-----|----------|
| Gateway API | http://localhost:5000 | Tüm API'lere merkezi erişim |
| Catalog API | http://localhost:5001/swagger | Ürün yönetimi |
| Identity API | http://localhost:5002/swagger | Kimlik doğrulama |
| Order API | http://localhost:5003/swagger | Sipariş yönetimi |
| Payment API | http://localhost:5004/swagger | Ödeme işlemleri |
| Cart API | http://localhost:5005/swagger | Sepet işlemleri |
| Kibana | http://localhost:5601 | APM, Traces, Logs |
| Grafana | http://localhost:3000 | Metrik dashboard'ları |
| Prometheus | http://localhost:9090 | Metrik sorgulama |
| Elasticsearch | http://localhost:9200 | Veri depolama |

### Manuel Kurulum

```bash
# Her servis için bağımlılıkları yükleyin
cd Catalog.Api
dotnet restore
dotnet build

# Migrations uygulayın (Catalog, Identity, Order, Payment için)
cd Catalog.Infrastructure
dotnet ef database update --startup-project ../Catalog.Api

# Servisi başlatın
cd ../Catalog.Api
dotnet run
```

### Ortam Değişkenleri

```env
# Database (MSSQL)
ConnectionStrings__DefaultConnection=Server=localhost,1434;Database=CatalogDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True

# Redis
Redis__ConnectionString=localhost:6381

# JWT
JwtSettings__SecretKey=your-super-secret-key-here
JwtSettings__Issuer=identity-api
JwtSettings__Audience=e-commerce-app
JwtSettings__ExpirationInMinutes=60

# Kafka
Kafka__BootstrapServers=localhost:9092

# OpenTelemetry
OpenTelemetry__Endpoint=http://otel-collector:4317
```

---

## 📖 API Dokümantasyonu

Her servis Swagger UI ile dokümante edilmiştir:

| Servis | Swagger URL | Gateway URL |
|--------|-------------|-------------|
| Gateway API | - | `http://localhost:5000` |
| Catalog API | `http://localhost:5001/swagger` | `http://localhost:5000/api/catalog/*` |
| Identity API | `http://localhost:5002/swagger` | `http://localhost:5000/api/identity/*` |
| Cart API | `http://localhost:5005/swagger` | `http://localhost:5000/api/cart/*` |
| Order API | `http://localhost:5003/swagger` | `http://localhost:5000/api/order/*` |
| Payment API | `http://localhost:5004/swagger` | `http://localhost:5000/api/payment/*` |

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
├── docker-compose.yml          # Ana orchestration (tüm servisler + observability)
├── otel-collector-config.yaml  # OpenTelemetry Collector yapılandırması
├── prometheus.yml              # Prometheus scrape config
├── OBSERVABILITY_GUIDE.md      # Observability kurulum rehberi
│
├── Gateway.Api/               # YARP Reverse Proxy
│   ├── Program.cs
│   └── appsettings.json       # Route yapılandırması
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

## 📡 Observability

Projede tüm servislerden **distributed tracing**, **metrics** ve **logs** toplayan kapsamlı bir observability altyapısı bulunmaktadır.

### Bileşenler

| Bileşen | Port | Görev |
|---------|------|-------|
| **OpenTelemetry SDK** | - | Her serviste traces, metrics, logs üretir |
| **OTEL Collector** | 4317 (gRPC), 4318 (HTTP) | Telemetri verisini toplar ve yönlendirir |
| **APM Server** | 8200 | OTLP verisini Elasticsearch formatına dönüştürür |
| **Elasticsearch** | 9200 | Trace, metrik ve log verisi depolar |
| **Kibana** | 5601 | APM UI, trace ve log görselleştirme |
| **Prometheus** | 9090 | Metrik toplama (scrape) |
| **Grafana** | 3000 | Metrik dashboard'ları ve görselleştirme |

### Veri Akışı

```
Servisler ── OTLP ──▶ OTEL Collector ──┬── OTLP ──▶ APM Server ──▶ Elasticsearch ──▶ Kibana APM
                                      │
                                      └── Scrape ─▶ Prometheus ──▶ Grafana
```

### Kibana APM

Kibana APM üzerinden görüntülenebilecekler:
- **Services** — Tüm servislerin performans özeti
- **Traces** — Distributed trace detayları, waterfall view
- **Dependencies** — Servisler arası bağımlılık haritası
- **Errors** — Hata izleme ve analiz
- **Metrics** — HTTP istek süresi, throughput, hata oranı

### Grafana

- .NET Runtime metrikleri (GC, Thread Pool, Memory)
- HTTP istek metrikleri (duration, status codes)
- Servis başına özel dashboard'lar oluşturulabilir

### Observability Stack'i Başlatma

```bash
# Sadece API servisleri (observability olmadan)
docker compose up -d

# API + Observability (Elasticsearch, Kibana, APM, Prometheus, Grafana)
docker compose --profile observability up -d

# Observability stack'i durdurma
docker compose --profile observability down
```

---

## 📊 Health Checks

Her servis health check endpoint'leri sunar:

```http
GET /health         # Genel sağlık durumu
GET /health/ready   # Readiness probe
GET /health/live    # Liveness probe
```

---

## ☸️ Kubernetes Deployment

Proje Kubernetes üzerinde deploy edilmeye hazırdır. Her servis için Dockerfile mevcut olup, K8s manifest'leri ile production ortamına deploy edilebilir.

```bash
# Docker image build
docker build -t catalog-api ./Catalog.Api/Catalog.Api
docker build -t identity-api ./Idendity.Api/Idendity.Api
docker build -t order-api ./Order.Api/Order.Api
docker build -t payment-api ./Payment.Api/Payment.Api
docker build -t cart-api ./Cart.Api/Cart.Api
docker build -t gateway-api ./Gateway.Api
```


## 👤 Geliştirici

**Ekrem-A**

- GitHub: [@Ekrem-A](https://github.com/Ekrem-A)

---

<div align="center">

⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın!

</div>
