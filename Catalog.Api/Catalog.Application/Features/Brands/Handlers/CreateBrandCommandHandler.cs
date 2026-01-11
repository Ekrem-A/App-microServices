using AutoMapper;
using Catalog.Application.Brands.Commands.CreateBrand;
using Catalog.Application.Common.Interfaces;
using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Brands.Handlers;

public class CreateBrandCommandHandler : IRequestHandler<CreateBrandCommand, BrandDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateBrandCommandHandler> _logger;

    public CreateBrandCommandHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<CreateBrandCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BrandDto> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var exists = await _unitOfWork.Brands.ExistsAsync(
                b => b.Slug == request.Slug,
                cancellationToken);

            if (exists)
                throw new InvalidOperationException($"Brand with slug '{request.Slug}' already exists");

            var brand = new Brand
            {
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                LogoUrl = request.LogoUrl,
                WebsiteUrl = request.WebsiteUrl,
                IsActive = true,
                DisplayOrder = request.DisplayOrder
            };

            await _unitOfWork.Brands.AddAsync(brand, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Brand created: {BrandId} - {BrandName}", brand.Id, brand.Name);

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
