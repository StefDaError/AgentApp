namespace AgentApp.ViewModels;

public sealed record InfoRow(string Label, string Value);

public sealed record InfoSection(string Title, IReadOnlyList<InfoRow> Rows);