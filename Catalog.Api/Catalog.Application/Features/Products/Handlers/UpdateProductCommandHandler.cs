using AutoMapper;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using Catalog.Application.Products.Commands.UpdateProduct;
using Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Catalog.Application.Features.Products.Handlers;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher? _eventPublisher;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<UpdateProductCommandHandler> logger,
        IEventPublisher? eventPublisher = null)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
            if (product == null)
                throw new KeyNotFoundException($"Product with ID {request.Id} not found");

            // Validate Brand
            var brand = await _unitOfWork.Brands.GetByIdAsync(request.BrandId, cancellationToken);
            if (brand == null)
                throw new KeyNotFoundException($"Brand with ID {request.BrandId} not found");

            var oldPrice = product.Price;
            var oldStock = product.StockQuantity;

            // Update properties
            product.Name = request.Name;
            product.Slug = request.Slug;
            product.BrandId = request.BrandId;
            product.CategoryId = request.CategoryId;
            product.Price = request.Price;
            product.OriginalPrice = request.OriginalPrice;
            product.Description = request.Description;
            product.StockQuantity = request.StockQuantity;
            product.InStock = request.StockQuantity > 0;
            product.Featured = request.Featured;
            product.IsCampaign = request.IsCampaign;
            product.DiscountPercentage = request.DiscountPercentage;
            product.CampaignEndDate = request.CampaignEndDate;

            // Serialize Images and Tags
            if (request.Images != null && request.Images.Count > 0)
            {
                product.Images = JsonSerializer.Serialize(request.Images);
            }
            else
            {
                product.Images = null;
            }

            if (request.Tags != null && request.Tags.Count > 0)
            {
                product.Tags = JsonSerializer.Serialize(request.Tags);
            }
            else
            {
                product.Tags = null;
            }

            await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Product updated: {ProductId}", product.Id);

            // Clear cache
            await _cacheService.RemoveAsync($"products:{product.Id}", cancellationToken);
            await _cacheService.RemoveByPrefixAsync("products:", cancellationToken);

            // Publish events (if available)
            if (_eventPublisher != null)
            {
                if (oldPrice != product.Price)
                {
                    await _eventPublisher.PublishAsync(
                        KafkaTopics.ProductPriceChanged,
                        new ProductPriceChangedEvent
                        {
                            ProductId = product.Id,
                            Name = product.Name,
                            OldPrice = oldPrice,
                            NewPrice = product.Price,
                            ChangedAt = DateTime.UtcNow
                        },
                        cancellationToken);
                }

                if (oldStock != product.StockQuantity)
                {
                    await _eventPublisher.PublishAsync(
                        KafkaTopics.ProductStockUpdated,
                        new ProductStockUpdatedEvent
                        {
                            ProductId = product.Id,
                            Name = product.Name,
                            OldStock = oldStock,
                            NewStock = product.StockQuantity,
                            InStock = product.InStock,
                            UpdatedAt = DateTime.UtcNow
                        },
                        cancellationToken);
                }
            }

            // Reload product with related entities for mapping
            var productWithIncludes = await _unitOfWork.ProductQueries.GetProductByIdWithIncludesAsync(product.Id, cancellationToken);
            return _mapper.Map<ProductDto>(productWithIncludes);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
