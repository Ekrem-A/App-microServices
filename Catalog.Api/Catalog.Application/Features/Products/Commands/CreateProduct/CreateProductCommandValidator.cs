using FluentValidation;

namespace Catalog.Application.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(500).WithMessage("Product name must not exceed 500 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required")
            .MaximumLength(550).WithMessage("Slug must not exceed 550 characters");

        RuleFor(x => x.BrandId)
            .NotEmpty().WithMessage("Brand is required");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");

        RuleFor(x => x.OriginalPrice)
            .GreaterThan(x => x.Price)
            .When(x => x.OriginalPrice.HasValue)
            .WithMessage("Original price must be greater than current price");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative");

        RuleFor(x => x.DiscountPercentage)
            .InclusiveBetween(0, 100).WithMessage("Discount percentage must be between 0 and 100");
    }
}