using FluentValidation;
using StoreApi.Application.Orders;

namespace StoreApi.Application.Validators;

public sealed class CheckoutDtoValidator : AbstractValidator<CheckoutDto>
{
    public CheckoutDtoValidator()
    {
        RuleFor(x => x.ShippingAddress).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
{
    public UpdateOrderStatusDtoValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class OrderQueryParamsValidator : AbstractValidator<OrderQueryParams>
{
    public OrderQueryParamsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, OrderQueryParams.MaxPageSize);
    }
}
