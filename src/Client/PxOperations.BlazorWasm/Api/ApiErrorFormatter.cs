using System.Text.Json;

namespace PxOperations.BlazorWasm.Api;

public static class ApiErrorFormatter
{
    public static string Format(Exception exception, string fallbackMessage)
    {
        if (exception is ApiException apiException)
        {
            var resultMessage = TryExtractMessageFromResult(apiException);
            if (!string.IsNullOrWhiteSpace(resultMessage))
                return resultMessage;

            var responseMessage = TryExtractMessage(apiException.Response);
            if (!string.IsNullOrWhiteSpace(responseMessage))
                return responseMessage;
        }

        return string.IsNullOrWhiteSpace(exception.Message) ? fallbackMessage : exception.Message;
    }

    private static string? TryExtractMessage(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        try
        {
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;

            if (root.TryGetProperty("detail", out var detail) &&
                detail.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(detail.GetString()))
            {
                return detail.GetString();
            }

            if (root.TryGetProperty("title", out var title) &&
                title.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(title.GetString()))
            {
                return title.GetString();
            }

            if (root.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String &&
                                !string.IsNullOrWhiteSpace(item.GetString()))
                            {
                                return item.GetString();
                            }
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static string? TryExtractMessageFromResult(ApiException apiException)
    {
        var resultProperty = apiException.GetType().GetProperty("Result");
        var result = resultProperty?.GetValue(apiException);
        if (result is null)
            return null;

        var detailProperty = result.GetType().GetProperty("Detail");
        var detail = detailProperty?.GetValue(result) as string;
        if (!string.IsNullOrWhiteSpace(detail))
            return detail;

        var titleProperty = result.GetType().GetProperty("Title");
        var title = titleProperty?.GetValue(result) as string;
        if (!string.IsNullOrWhiteSpace(title))
            return title;

        return null;
    }
}
