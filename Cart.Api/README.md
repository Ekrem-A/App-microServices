# Cart Microservice API ??

Shopping Cart microservice for e-commerce applications built with modern .NET 8 and Clean Architecture principles.

## ?? Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Technologies](#technologies)
- [Project Structure](#project-structure)
- [API Endpoints](#api-endpoints)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Docker Support](#docker-support)

## Overview

Cart.Api is a high-performance shopping cart microservice designed for e-commerce platforms. It provides a complete cart management solution including item management, coupon application, cart merging, and checkout functionality with event-driven architecture.

## Architecture

This project follows **Clean Architecture** (also known as Onion Architecture) with clear separation of concerns:

```
???????????????????????????????????????????????????????????????
?                        Cart.Api                              ?
?                   (Presentation Layer)                       ?
???????????????????????????????????????????????????????????????
?                    Cart.Application                          ?
?              (Business Logic / Use Cases)                    ?
???????????????????????????????????????????????????????????????
?                      Cart.Domain                             ?
?                    (Domain Entities)                         ?
???????????????????????????????????????????????????????????????
?                   Cart.Infrastructure                        ?
?              (Data Access / External Services)               ?
???????????????????????????????????????????????????????????????
```

### Design Patterns Used
- **CQRS (Command Query Responsibility Segregation)** - Separates read and write operations
- **Mediator Pattern** - Decouples request handling using MediatR
- **Repository Pattern** - Abstracts data access layer
- **Domain-Driven Design (DDD)** - Rich domain model with aggregates

## Technologies

### Core Framework
| Technology | Version | Description |
|------------|---------|-------------|
| .NET | 8.0 | Target framework |
| ASP.NET Core | 8.0 | Web API framework |
| C# | 12.0 | Programming language |

### Libraries & Packages

#### Application Layer
| Package | Version | Purpose |
|---------|---------|---------|
| MediatR | 12.4.1 | CQRS & Mediator pattern implementation |
| FluentValidation | 11.11.0 | Request validation |
| FluentValidation.DependencyInjectionExtensions | 11.11.0 | DI integration for validators |

#### Infrastructure Layer
| Package | Version | Purpose |
|---------|---------|---------|
| StackExchange.Redis | 2.8.16 | Redis client for cart persistence |
| Confluent.Kafka | 2.6.1 | Kafka client for event publishing |

#### API Layer
| Package | Version | Purpose |
|---------|---------|---------|
| Swashbuckle.AspNetCore | 6.6.2 | Swagger/OpenAPI documentation |
| AspNetCore.HealthChecks.Redis | 8.0.1 | Redis health check integration |

### External Dependencies
| Service | Purpose |
|---------|---------|
| **Redis** | High-performance cart data storage |
| **Apache Kafka** | Event streaming for checkout events |

## Project Structure

```
Cart.Api/
??? Cart.Api/                          # Presentation Layer
?   ??? Controllers/
?   ?   ??? CartController.cs          # REST API endpoints
?   ??? Middleware/
?   ?   ??? ExceptionHandlingMiddleware.cs
?   ??? Program.cs                     # Application entry point
?   ??? Dockerfile                     # Container configuration
?
??? Cart.Application/                  # Application Layer
?   ??? Abstractions/
?   ?   ??? ICartRepository.cs         # Repository interface
?   ?   ??? IEventPublisher.cs         # Event publisher interface
?   ??? Carts/
?   ?   ??? Commands/                  # Write operations (CQRS)
?   ?   ?   ??? AddItem/
?   ?   ?   ??? RemoveItem/
?   ?   ?   ??? UpdateItemQuantity/
?   ?   ?   ??? ClearCart/
?   ?   ?   ??? ApplyCoupon/
?   ?   ?   ??? RemoveCoupon/
?   ?   ?   ??? MergeCart/
?   ?   ?   ??? RepriceCart/
?   ?   ?   ??? Checkout/
?   ?   ??? Queries/                   # Read operations (CQRS)
?   ?       ??? GetCart/
?   ??? Common/
?   ?   ??? Behaviors/
?   ?   ?   ??? ValidationBehavior.cs  # MediatR pipeline behavior
?   ?   ??? Exceptions/
?   ??? Contracts/
?   ?   ??? Requests/                  # API request DTOs
?   ??? DTOs/
?   ?   ??? CartDto.cs
?   ?   ??? CartMappingExtensions.cs
?   ??? Events/
?       ??? CheckoutRequestedEvent.cs  # Domain events
?
??? Cart.Domain/                       # Domain Layer
?   ??? CartAggregate/
?   ?   ??? ShoppingCart.cs            # Aggregate root
?   ?   ??? CartItem.cs                # Entity
?   ?   ??? Coupon.cs                  # Value object
?   ??? Exceptions/
?       ??? CartDomainException.cs
?
??? Cart.Infrastructure/               # Infrastructure Layer
    ??? Messaging/
    ?   ??? KafkaEventPublisher.cs     # Kafka integration
    ?   ??? NoOpEventPublisher.cs      # Fallback publisher
    ??? Options/
    ?   ??? RedisOptions.cs
    ?   ??? KafkaOptions.cs
    ?   ??? CartOptions.cs
    ??? Persistence/
    ?   ??? RedisCartRepository.cs     # Redis implementation
    ?   ??? CartSerializationHelper.cs
    ??? DependencyInjection.cs
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/cart/{userId}` | Get cart by user ID |
| `POST` | `/api/cart/{userId}/items` | Add item to cart |
| `PUT` | `/api/cart/{userId}/items/{productId}` | Update item quantity |
| `DELETE` | `/api/cart/{userId}/items/{productId}` | Remove item from cart |
| `DELETE` | `/api/cart/{userId}` | Clear entire cart |
| `POST` | `/api/cart/{userId}/checkout` | Checkout cart |
| `POST` | `/api/cart/{userId}/merge` | Merge anonymous cart |
| `POST` | `/api/cart/{userId}/coupon` | Apply coupon |
| `DELETE` | `/api/cart/{userId}/coupon` | Remove coupon |
| `POST` | `/api/cart/{userId}/reprice` | Reprice cart items |

### Health Endpoints
| Endpoint | Description |
|----------|-------------|
| `/health/live` | Liveness probe |
| `/health/ready` | Readiness probe (includes Redis check) |
| `/swagger` | API documentation |

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Redis Server
- Apache Kafka (optional, for event publishing)
- Docker (optional)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/Ekrem-A/App-microServices.git
   cd Cart.Api
   ```

2. **Start Redis** (using Docker)
   ```bash
   docker run -d -p 6379:6379 redis:alpine
   ```

3. **Run the application**
   ```bash
   dotnet run --project Cart.Api
   ```

4. **Access Swagger UI**
   ```
   http://localhost:8080/swagger
   ```

## Configuration

### Application Settings (appsettings.json)

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "CheckoutTopic": "cart-checkout-events"
  },
  "Cart": {
    "DefaultExpirationMinutes": 1440
  }
}
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | Production |
| `Redis__ConnectionString` | Redis connection string | localhost:6379 |

## Docker Support

### Build Image
```bash
docker build -t cart-api -f Cart.Api/Dockerfile .
```

### Run Container
```bash
docker run -d -p 8080:8080 \
  -e Redis__ConnectionString=your-redis-host:6379 \
  cart-api
```

### Docker Features
- Multi-stage build for optimized image size
- Alpine-based images for minimal footprint
- Non-root user for security
- Health check endpoints
- Kubernetes ready

## Features

- ? Full shopping cart CRUD operations
- ? Coupon/discount support (percentage & fixed amount)
- ? Cart merging (anonymous to authenticated user)
- ? Price synchronization with catalog
- ? Event-driven checkout via Kafka
- ? Redis-based high-performance storage
- ? Input validation with FluentValidation
- ? Health checks for monitoring
- ? Swagger/OpenAPI documentation
- ? Docker containerization
- ? Kubernetes deployment ready

## License

This project is open source and available under the [MIT License](LICENSE).

---

**Author:** Ekrem A.  
**Repository:** [https://github.com/Ekrem-A/App-microServices](https://github.com/Ekrem-A/App-microServices)
