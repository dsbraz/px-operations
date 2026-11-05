using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Tests.Helpers;

internal static class TeamTestHelpers
{
    internal static string TeamMembersJson() => """
        [
          {"name":"Julia Pereira","canBeDeliveryManager":false},
          {"name":"Renata Fabri","canBeDeliveryManager":true},
          {"name":"Denise Santos","canBeDeliveryManager":true},
          {"name":"Carla Paixão","canBeDeliveryManager":true},
          {"name":"Pedro Vieira","canBeDeliveryManager":true}
        ]
        """;

    internal static IReadOnlyList<TeamMemberView> TeamMembers() =>
    [
        new() { Name = "Julia Pereira", CanBeDeliveryManager = false },
        new() { Name = "Denise Santos", CanBeDeliveryManager = true },
        new() { Name = "Pedro Vieira", CanBeDeliveryManager = true }
    ];

    internal static string?[] OptionValues(AngleSharp.Dom.IElement select)
        => select.QuerySelectorAll("option")
            .Select(option => option.GetAttribute("value"))
            .ToArray();
}
