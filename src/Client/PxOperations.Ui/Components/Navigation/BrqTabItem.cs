namespace PxOperations.Ui.Components.Navigation;

/// <summary>Uma visão das abas. Com <see cref="Href"/>, a visão tem rota própria e vira link.</summary>
public sealed record BrqTabItem(string Key, string Label, string? Href = null);
