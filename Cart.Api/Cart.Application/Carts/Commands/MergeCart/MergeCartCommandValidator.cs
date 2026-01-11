using FluentValidation;

namespace Cart.Application.Carts.Commands.MergeCart;

public class MergeCartCommandValidator : AbstractValidator<MergeCartCommand>
{
    public MergeCartCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("TargetUserId is required.");

        RuleFor(x => x.SourceUserId)
            .NotEmpty().WithMessage("SourceUserId is required.");

        RuleFor(x => x)
            .Must(x => x.TargetUserId != x.SourceUserId)
            .WithMessage("TargetUserId and SourceUserId cannot be the same.");
    }
}

