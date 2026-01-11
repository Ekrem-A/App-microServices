using FluentValidation;

namespace Cart.Application.Carts.Commands.AddItem;

public class AddItemCommandValidator : AbstractValidator<AddItemCommand>
{
    public AddItemCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.ProductId)
            .NotEqual(Guid.Empty).WithMessage("ProductId is required.");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("ProductName is required.")
            .MaximumLength(500).WithMessage("ProductName cannot exceed 500 characters.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("UnitPrice cannot be negative.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}

