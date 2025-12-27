namespace Ancela.Agent.SemanticKernel.Plugins.GraphPlugin;

public class ContactModel
{
    public required string DisplayName { get; init; }
    public required string[] EmailAddresses { get; init; }
    public required string MobilePhone { get; init; }
    public required string[] BusinessPhones { get; init; }
    public required string CompanyName { get; init; }
    public required string JobTitle { get; init; }
}
