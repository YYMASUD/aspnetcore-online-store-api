using FluentValidation;
using StoreApi.Application.Cart;

namespace StoreApi.Application.Validators;

public sealed class AddToCartDtoValidator : AbstractValidator<AddToCartDto>
{
    public AddToCartDtoValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class UpdateCartDtoValidator : AbstractValidator<UpdateCartDto>
{
    public UpdateCartDtoValidator()
    {
        // Quantity == 0 is allowed as an explicit remove; otherwise it must be positive.
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}
