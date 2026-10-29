using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Tests.Helpers;

internal static class TeamTestHelpers
{
    internal static string TeamMembersJson() => """
        [
          {"name":"Julia Pereira","area":"sales","areaLabel":"Sales","canBeDeliveryManager":false},
          {"name":"Renata Fabri","area":"taas","areaLabel":"TaaS","canBeDeliveryManager":true},
          {"name":"Denise Santos","area":"operations","areaLabel":"Operações","canBeDeliveryManager":true},
          {"name":"Carla Paixão","area":"projects","areaLabel":"Projetos","canBeDeliveryManager":true},
          {"name":"Pedro Vieira","area":"projects","areaLabel":"Projetos","canBeDeliveryManager":true}
        ]
        """;

    internal static IReadOnlyList<TeamMemberView> TeamMembers() =>
    [
        new() { Name = "Julia Pereira", Area = "sales", AreaLabel = "Sales", CanBeDeliveryManager = false },
        new() { Name = "Denise Santos", Area = "operations", AreaLabel = "Operações", CanBeDeliveryManager = true },
        new() { Name = "Pedro Vieira", Area = "projects", AreaLabel = "Projetos", CanBeDeliveryManager = true }
    ];

    internal static string?[] OptionValues(AngleSharp.Dom.IElement select)
        => select.QuerySelectorAll("option")
            .Select(option => option.GetAttribute("value"))
            .ToArray();
}
