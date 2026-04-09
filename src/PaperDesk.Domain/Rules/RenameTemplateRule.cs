namespace PaperDesk.Domain.Rules;

public sealed class RenameTemplateRule
{
    public string Template { get; init; } = "{date}_{type}_{party}_{amount}";
}
