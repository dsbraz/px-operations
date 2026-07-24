namespace PxOperations.Ui.Components.Navigation;

public sealed record BrqNavigationItem(
    string Label,
    string Href,
    string Icon,
    bool MatchExact = false);
