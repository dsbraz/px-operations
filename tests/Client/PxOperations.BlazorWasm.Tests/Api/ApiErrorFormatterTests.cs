using System.Reflection;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Tests.Api;

public sealed class ApiErrorFormatterTests
{
    [Fact]
    public void Format_should_return_problem_details_detail()
    {
        var exception = CreateApiException("""{"detail":"Informe o título do marco."}""");

        var method = typeof(ApiErrorFormatter).GetMethod("Format", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
        var formatted = (string)method.Invoke(null, [exception, "fallback"])!;

        Assert.Equal("Informe o título do marco.", formatted);
    }

    [Fact]
    public void Format_should_return_first_validation_error()
    {
        var exception = CreateApiException("""{"errors":{"Title":["Informe o título do marco."]}}""");

        var method = typeof(ApiErrorFormatter).GetMethod("Format", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
        var formatted = (string)method.Invoke(null, [exception, "fallback"])!;

        Assert.Equal("Informe o título do marco.", formatted);
    }

    [Fact]
    public void Format_should_fall_back_when_the_failure_carries_no_server_message()
    {
        var formatted = ApiErrorFormatter.Format(
            new HttpRequestException("TypeError: Failed to fetch"),
            "Não foi possível gerar o link.");

        Assert.Equal("Não foi possível gerar o link.", formatted);
    }

    [Fact]
    public void Format_should_fall_back_when_the_response_body_has_no_usable_text()
    {
        var formatted = ApiErrorFormatter.Format(
            CreateApiException("""{"status":500}"""),
            "Não foi possível gerar o link.");

        Assert.Equal("Não foi possível gerar o link.", formatted);
    }

    private static ApiException CreateApiException(string response)
        => new("Bad Request", 400, response, new Dictionary<string, IEnumerable<string>>(), null!);
}
