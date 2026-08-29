using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PxOperations.Api.Errors;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Api.IntegrationTests.Errors;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task Internal_argument_failures_should_not_be_answered_as_bad_request()
    {
        var problemDetails = new RecordingProblemDetailsService();
        var handler = new ApiExceptionHandler(problemDetails);

        var handled = await handler.TryHandleAsync(
            new DefaultHttpContext(),
            new ArgumentException("An item with the same key has already been added."),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Null(problemDetails.Written);
    }

    [Fact]
    public async Task Invalid_request_values_should_be_answered_as_bad_request()
    {
        var problemDetails = new RecordingProblemDetailsService();
        var handler = new ApiExceptionHandler(problemDetails);
        var context = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidRequestValueException("Invalid project status: Nope"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("Invalid project status: Nope", problemDetails.Written?.Detail);
    }

    [Theory]
    [InlineData(typeof(BusinessRuleValidationException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(ValidationException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(ResourceNotFoundException), StatusCodes.Status404NotFound)]
    [InlineData(typeof(BusinessStateConflictException), StatusCodes.Status409Conflict)]
    public async Task Known_failures_should_keep_their_status(Type exceptionType, int expectedStatus)
    {
        var problemDetails = new RecordingProblemDetailsService();
        var handler = new ApiExceptionHandler(problemDetails);
        var context = new DefaultHttpContext();
        var exception = (Exception)Activator.CreateInstance(exceptionType, "boom")!;

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
    }

    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? Written { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Written = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Written = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }
    }
}
