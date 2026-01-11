using AutoMapper;
using Catalog.Application.Features.Brands.Commands.Update;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using Catalog.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Brands.Handlers;

public class UpdateBrandCommandHandler : IRequestHandler<UpdateBrandCommand, BrandDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateBrandCommandHandler> _logger;

    public UpdateBrandCommandHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<UpdateBrandCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BrandDto> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var brand = await _unitOfWork.Brands.GetByIdAsync(request.Id, cancellationToken);
            if (brand == null)
                throw new KeyNotFoundException($"Brand with ID {request.Id} not found");

            var exists = await _unitOfWork.Brands.ExistsAsync(
                b => b.Slug == request.Slug && b.Id != request.Id,
                cancellationToken);

            if (exists)
                throw new InvalidOperationException($"Brand with slug '{request.Slug}' already exists");

            brand.Name = request.Name;
            brand.Slug = request.Slug;
            brand.Description = request.Description;
            brand.LogoUrl = request.LogoUrl;
            brand.WebsiteUrl = request.WebsiteUrl;
            brand.IsActive = request.IsActive;
            brand.DisplayOrder = request.DisplayOrder;

            await _unitOfWork.Brands.UpdateAsync(brand, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Brand updated: {BrandId}", brand.Id);

            await _cacheService.RemoveByPrefixAsync("brands:", cancellationToken);

            return _mapper.Map<BrandDto>(brand);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
