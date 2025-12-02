namespace Ancilla.FunctionApp;

public class NoteModel
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string UserPhoneNumber { get; set; } = string.Empty;
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? Deleted { get; set; }
}