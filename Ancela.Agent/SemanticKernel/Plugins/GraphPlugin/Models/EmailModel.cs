namespace Ancela.Agent.SemanticKernel.Plugins.GraphPlugin;

public class EmailModel
{
    public required string Subject { get; init; }
    public required string From { get; init; }
    public required DateTimeOffset ReceivedDateTime { get; init; }
    public required string BodyPreview { get; init; }
    public required bool IsRead { get; init; }
}
