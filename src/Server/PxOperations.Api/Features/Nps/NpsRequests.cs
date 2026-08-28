using Microsoft.AspNetCore.Mvc;

namespace PxOperations.Api.Features.Nps;

public sealed class NpsQueryRequest
{
    [FromQuery(Name = "search")]
    public string? Search { get; init; }

    [FromQuery(Name = "client")]
    public string[] Client { get; init; } = [];

    [FromQuery(Name = "dc")]
    public string[] Dc { get; init; } = [];

    [FromQuery(Name = "projectType")]
    public string[] ProjectType { get; init; } = [];

    [FromQuery(Name = "deliveryManager")]
    public string[] DeliveryManager { get; init; } = [];

    [FromQuery(Name = "status")]
    public string[] Status { get; init; } = [];

    [FromQuery(Name = "format")]
    public string[] Format { get; init; } = [];

    [FromQuery(Name = "classification")]
    public string[] Classification { get; init; } = [];

    [FromQuery(Name = "from")]
    public string? From { get; init; }

    [FromQuery(Name = "to")]
    public string? To { get; init; }

    [FromQuery(Name = "includeWaived")]
    public bool IncludeWaived { get; init; }

    [FromQuery(Name = "projectId")]
    public int? ProjectId { get; init; }
}

public sealed record CreateNpsContactRequest(string Name, string Email, string? Role);
public sealed record UpdateNpsContactRequest(string Name, string Email, string? Role);
public sealed record CreateNpsDispatchRequest(int ProjectId, string Format, string Language, IReadOnlyList<int>? ContactIds);
public sealed record WaiveNpsCollectionRequest(string Reason);
public sealed record SubmitNpsSurveyResponseRequest(
    int Score,
    int? Quality,
    int? Schedule,
    int? Communication,
    int? BusinessValue,
    string? Comment,
    string? RespondentName,
    string? RespondentEmail);
