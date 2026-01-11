using AutoMapper;
using Catalog.Application.Categories.Commands.CreateCategory;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Categories.Handlers;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var exists = await _unitOfWork.Categories.ExistsAsync(
                c => c.Slug == request.Slug,
                cancellationToken);

            if (exists)
                throw new InvalidOperationException($"Category with slug '{request.Slug}' already exists");

            var category = new Category
            {
                Name = request.Name,
                Slug = request.Slug,
                ParentId = request.ParentId,
                Description = request.Description,
                DisplayOrder = request.DisplayOrder               
            };

            await _unitOfWork.Categories.AddAsync(category, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Category created: {CategoryId} - {CategoryName}", category.Id, category.Name);

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