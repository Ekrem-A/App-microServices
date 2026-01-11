using Catalog.Application.Features.Categories.Commands.Delete;
using Catalog.Application.Common.Interfaces;
using Catalog.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Categories.Handlers;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DeleteCategoryCommandHandler> _logger;

    public DeleteCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<DeleteCategoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
            if (category == null)
                return false;

            var hasProducts = await _unitOfWork.Products.ExistsAsync(
                p => p.CategoryId == request.Id,
                cancellationToken);

            if (hasProducts)
                throw new InvalidOperationException("Cannot delete category that has products");

            await _unitOfWork.Categories.DeleteAsync(category, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Category deleted: {CategoryId}", category.Id);

            await _cacheService.RemoveByPrefixAsync("categories:", cancellationToken);

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}