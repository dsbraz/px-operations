using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Api.Errors;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            ValidationException or BusinessRuleValidationException or ArgumentException => StatusCodes.Status400BadRequest,
            ResourceNotFoundException => StatusCodes.Status404NotFound,
            BusinessStateConflictException => StatusCodes.Status409Conflict,
            _ => 0
        };
        if (status == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = ReasonPhrases.GetReasonPhrase(status),
                Detail = exception.Message
            }
        });
    }
}
