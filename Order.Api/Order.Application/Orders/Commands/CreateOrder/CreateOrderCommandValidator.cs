using FluentValidation;

namespace Order.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required");

        When(x => x.ShippingAddress != null, () =>
        {
            RuleFor(x => x.ShippingAddress.Street)
                .NotEmpty().WithMessage("Street is required")
                .MaximumLength(200);

            RuleFor(x => x.ShippingAddress.City)
                .NotEmpty().WithMessage("City is required")
                .MaximumLength(100);

            RuleFor(x => x.ShippingAddress.District)
                .NotEmpty().WithMessage("District is required")
                .MaximumLength(100);

            RuleFor(x => x.ShippingAddress.PostalCode)
                .NotEmpty().WithMessage("Postal code is required")
                .MaximumLength(20);

            RuleFor(x => x.ShippingAddress.Country)
                .NotEmpty().WithMessage("Country is required")
                .MaximumLength(100);
        });

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must have at least one item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Product ID is required");

            item.RuleFor(x => x.ProductName)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(200);

            item.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0");

            item.RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");
        });

        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => x.Notes != null);

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(100).When(x => x.IdempotencyKey != null);
    }
}

