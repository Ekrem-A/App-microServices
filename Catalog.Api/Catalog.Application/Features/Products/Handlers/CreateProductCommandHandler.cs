using AutoMapper;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using Catalog.Application.Products.Commands.CreateProduct;
using Catalog.Domain.Entities;
using Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Catalog.Application.Features.Products.Handlers;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher? _eventPublisher;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<CreateProductCommandHandler> logger,
        IEventPublisher? eventPublisher = null)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // ✅ Transaction başlat
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Validate Brand
            var brand = await _unitOfWork.Brands.GetByIdAsync(request.BrandId, cancellationToken);
            if (brand == null)
                throw new KeyNotFoundException($"Brand with ID {request.BrandId} not found");

            // Validate Category if provided
            Category? category = null;
            if (request.CategoryId.HasValue)
            {
                category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId.Value, cancellationToken);
                if (category == null)
                    throw new KeyNotFoundException($"Category with ID {request.CategoryId} not found");
            }

            // Check slug uniqueness
            var existingProduct = await _unitOfWork.Products.FirstOrDefaultAsync(
                p => p.Slug == request.Slug,
                cancellationToken);

            if (existingProduct != null)
                throw new InvalidOperationException($"Product with slug '{request.Slug}' already exists");

            // Create Product
            var product = new Product
            {
                Name = request.Name,
                Slug = request.Slug,
                BrandId = request.BrandId,
                CategoryId = request.CategoryId,
                Price = request.Price,
                OriginalPrice = request.OriginalPrice,
                Description = request.Description,
                InStock = request.StockQuantity > 0,
                StockQuantity = request.StockQuantity,
                Featured = request.Featured,
                IsCampaign = request.IsCampaign,
                DiscountPercentage = request.DiscountPercentage,
                CampaignEndDate = request.CampaignEndDate,
                ProductSource = "own"
            };

            // Serialize Images and Tags
            if (request.Images != null && request.Images.Count > 0)
            {
                product.Images = JsonSerializer.Serialize(request.Images);
            }

            if (request.Tags != null && request.Tags.Count > 0)
            {
                product.Tags = JsonSerializer.Serialize(request.Tags);
            }

            // Add product
            await _unitOfWork.Products.AddAsync(product, cancellationToken);

            // ✅ Transaction commit (SaveChanges içinde)
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

            // Clear cache
            await _cacheService.RemoveByPrefixAsync("products:", cancellationToken);

            // Publish event (if available)
            if (_eventPublisher != null)
            {
                var productCreatedEvent = new ProductCreatedEvent
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Slug = product.Slug,
                    Price = product.Price,
                    BrandId = product.BrandId,
                    BrandName = brand.Name,
                    CategoryId = product.CategoryId,
                    StockQuantity = product.StockQuantity,
                    CreatedAt = product.CreatedAt
                };

                await _eventPublisher.PublishAsync(KafkaTopics.ProductCreated, productCreatedEvent, cancellationToken);
            }

            // Reload product with related entities for mapping
            var productWithIncludes = await _unitOfWork.ProductQueries.GetProductByIdWithIncludesAsync(product.Id, cancellationToken);
            return _mapper.Map<ProductDto>(productWithIncludes);
        }
        catch (Exception ex)
        {
            // ✅ Hata durumunda rollback
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error creating product");
            throw;
        }
    }
}