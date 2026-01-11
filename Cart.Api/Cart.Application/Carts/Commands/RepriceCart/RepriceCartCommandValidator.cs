using FluentValidation;

namespace Cart.Application.Carts.Commands.RepriceCart;

public class RepriceCartCommandValidator : AbstractValidator<RepriceCartCommand>
{
    public RepriceCartCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.PriceUpdates)
            .NotNull().WithMessage("PriceUpdates is required.")
            .NotEmpty().WithMessage("At least one price update is required.");

        RuleForEach(x => x.PriceUpdates).ChildRules(update =>
        {
            update.RuleFor(x => x.ProductId)
                .NotEqual(Guid.Empty).WithMessage("ProductId is required.");

            update.RuleFor(x => x.NewUnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("NewUnitPrice cannot be negative.");
        });
    }
}

