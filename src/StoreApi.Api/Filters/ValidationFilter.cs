using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StoreApi.Api.Filters;

/// <summary>
/// Runs the registered FluentValidation validator for every action argument and short-circuits
/// with a 400 ProblemDetails response listing field errors.
/// </summary>
public sealed class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (result.IsValid)
            {
                continue;
            }

            var modelState = new ModelStateDictionary();
            foreach (var failure in result.Errors)
            {
                modelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(modelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://httpstatuses.io/400",
                Extensions = { ["traceId"] = context.HttpContext.TraceIdentifier }
            });

            return;
        }

        await next();
    }
}
