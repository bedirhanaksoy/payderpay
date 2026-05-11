using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PayderPay.Application.Common.Interfaces.Validation;

namespace PayderPay.Api.Middleware;

public class ValidationFilter : IAsyncActionFilter
{
    private readonly IEnumerable<IRequestValidator> _validators;

    public ValidationFilter(IEnumerable<IRequestValidator> validators)
    {
        _validators = validators;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = BuildValidationProblemResult(context);
            return;
        }

        foreach (var argument in context.ActionArguments.Values.Where(x => x is not null))
        {
            var argumentType = argument!.GetType();
            var validators = _validators.Where(x => x.RequestType == argumentType).ToList();

            foreach (var validator in validators)
            {
                var errors = validator.Validate(argument);
                foreach (var (field, messages) in errors)
                {
                    foreach (var message in messages)
                    {
                        context.ModelState.AddModelError(field, message);
                    }
                }
            }
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = BuildValidationProblemResult(context);
            return;
        }

        await next();
    }

    private static IActionResult BuildValidationProblemResult(ActionExecutingContext context)
    {
        var details = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };

        return new BadRequestObjectResult(details);
    }
}
