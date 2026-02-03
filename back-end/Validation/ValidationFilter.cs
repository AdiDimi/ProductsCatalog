using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AdsApi.Validation;

//public sealed class ValidationFilter : IAsyncActionFilter
//{
//    private readonly IServiceProvider _sp;

//    public ValidationFilter (IServiceProvider sp) => _sp = sp;
//    public async Task OnActionExecutionAsync (ActionExecutingContext context, ActionExecutionDelegate next)
//    {
//        //var sp = context.HttpContext.RequestServices;

//        foreach (var arg in context.ActionArguments.Values)
//        {
//            if (arg is null) continue;
//            var t = arg.GetType();
//            // Skip primitives, strings, and files
//            if (t.IsPrimitive || t == typeof(string) || typeof(IFormFile).IsAssignableFrom(t) || typeof(IFormFileCollection).IsAssignableFrom(t))
//                continue;

//            //typeof(T) != typeof(IValidator<>).MakeGenericType(t)) // || 
//            var validatorType = typeof(IValidator<>).MakeGenericType(t);
//            //if (typeof(T) != t)
//            {
//                var validator = _sp.GetService(validatorType); 
//                if (validator is null) continue;

//                //var ctx = new ValidationContext<object>(arg);
//                //var result = await validator.ValidateAsync(ctx, context.HttpContext.RequestAborted).ConfigureAwait(false);
//                var validateAsync = validatorType.GetMethod("ValidateAsync", new[] { arg.GetType(), typeof(CancellationToken) });
//                var task = (Task)validateAsync!.Invoke(validator, new object[] { arg, context.HttpContext.RequestAborted })!;
//                await task.ConfigureAwait(false);

//                dynamic result = task.GetType().GetProperty("Result")!.GetValue(task)!;
//                if (!result.IsValid)
//                {
//                    var errors = ((IEnumerable<dynamic>)result.Errors)
//                     .GroupBy(e => (string)e.PropertyName)
//                     .ToDictionary(g => g.Key, g => g.Select(e => (string)e.ErrorMessage).ToArray());

//                    context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors));
//                    return;
//                    //return Results.ValidationProblem(errors);
//                }
//            }
//        }

//        await next();
//    }
//}
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Polly;
using System.Xml;

public sealed class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync (EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var sp = ctx.HttpContext.RequestServices;
      
        foreach (var arg in ctx.Arguments)
        {
            if (arg is null) continue;
            var t = arg.GetType();
            // Skip primitives, strings, and files
            if (t.IsPrimitive || t == typeof(string) || typeof(IFormFile).IsAssignableFrom(t) || typeof(IFormFileCollection).IsAssignableFrom(t))
                continue;
            var validatorType = typeof(IValidator<>).MakeGenericType(t);
            if (validatorType is null) continue;
         
            var validator = sp.GetService(validatorType);
            if (validator is null) continue;

            var validateAsync = validatorType.GetMethod("ValidateAsync", new[] { arg.GetType(), typeof(CancellationToken) });
            var task = (Task)validateAsync!.Invoke(validator, new object[] { arg, ctx.HttpContext.RequestAborted })!;
            await task.ConfigureAwait(false);

            dynamic result = task.GetType().GetProperty("Result")!.GetValue(task)!;
            //ValidationResult result = await (validator as IValidator<T>)!.ValidateAsync(arg, ctx.HttpContext.RequestAborted);
            if (result.IsValid)
                return await next(ctx);

            var errors = ((IEnumerable<dynamic>)result.Errors)
                     .GroupBy(e => (string)e.PropertyName)
                     .ToDictionary(g => g.Key, g => g.Select(e => (string)e.ErrorMessage).ToArray());
          
            return Results.ValidationProblem(errors);
        }
        return await next(ctx);
      
    }
}
