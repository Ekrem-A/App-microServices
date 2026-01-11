using Catalog.Application.Common.Interfaces;
using Catalog.Application.Products.Commands.DeleteProduct;
using Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Products.Handlers;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher? _eventPublisher;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<DeleteProductCommandHandler> logger,
        IEventPublisher? eventPublisher = null)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
            if (product == null)
                return false;

            await _unitOfWork.Products.DeleteAsync(product, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Product deleted: {ProductId}", product.Id);

            // Clear cache
            await _cacheService.RemoveAsync($"products:{product.Id}", cancellationToken);
            await _cacheService.RemoveByPrefixAsync("products:", cancellationToken);

            // Publish event (if available)
            if (_eventPublisher != null)
            {
                await _eventPublisher.PublishAsync(
                    KafkaTopics.ProductDeleted,
                    new ProductDeletedEvent
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        DeletedAt = DateTime.UtcNow
                    },
                    cancellationToken);
            }

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}