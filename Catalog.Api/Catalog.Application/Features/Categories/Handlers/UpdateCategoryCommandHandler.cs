using AutoMapper;
using Catalog.Application.Features.Categories.Commands.Update;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using Catalog.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Categories.Handlers;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateCategoryCommandHandler> _logger;

    public UpdateCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<UpdateCategoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
            if (category == null)
                throw new KeyNotFoundException($"Category with ID {request.Id} not found");

            var exists = await _unitOfWork.Categories.ExistsAsync(
                c => c.Slug == request.Slug && c.Id != request.Id,
                cancellationToken);

            if (exists)
                throw new InvalidOperationException($"Category with slug '{request.Slug}' already exists");

            category.Name = request.Name;
            category.Slug = request.Slug;
            category.ParentId = request.ParentId;
            category.Description = request.Description;
            category.DisplayOrder = request.DisplayOrder;

            await _unitOfWork.Categories.UpdateAsync(category, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Category updated: {CategoryId}", category.Id);

            await _cacheService.RemoveByPrefixAsync("categories:", cancellationToken);

            return _mapper.Map<CategoryDto>(category);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}