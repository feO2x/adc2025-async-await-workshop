using FluentValidation;

namespace WebApi.CommonValidation;

public sealed class PagingParametersValidator : AbstractValidator<PagingParameters>
{
    public PagingParametersValidator()
    {
        RuleFor(p => p.Skip).GreaterThanOrEqualTo(0);
        RuleFor(p => p.Take).InclusiveBetween(1, 100);
    }
}