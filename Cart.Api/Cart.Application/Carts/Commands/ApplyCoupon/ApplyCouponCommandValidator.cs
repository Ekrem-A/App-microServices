using FluentValidation;

namespace Cart.Application.Carts.Commands.ApplyCoupon;

public class ApplyCouponCommandValidator : AbstractValidator<ApplyCouponCommand>
{
    public ApplyCouponCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.CouponCode)
            .NotEmpty().WithMessage("CouponCode is required.")
            .MaximumLength(50).WithMessage("CouponCode cannot exceed 50 characters.");

        RuleFor(x => x.CouponType)
            .NotEmpty().WithMessage("CouponType is required.")
            .Must(x => x.Equals("Percentage", StringComparison.OrdinalIgnoreCase) ||
                       x.Equals("FixedAmount", StringComparison.OrdinalIgnoreCase))
            .WithMessage("CouponType must be 'Percentage' or 'FixedAmount'.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Value must be greater than zero.");

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.CouponType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Percentage discount cannot exceed 100%.");
    }
}

