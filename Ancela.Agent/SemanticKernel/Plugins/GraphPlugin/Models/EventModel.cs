namespace Ancela.Agent.SemanticKernel.Plugins.GraphPlugin;

public class EventModel
{
    public required string Description { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }
}
