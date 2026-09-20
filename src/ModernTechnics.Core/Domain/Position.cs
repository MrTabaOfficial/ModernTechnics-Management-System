namespace ModernTechnics.Core.Domain;

/// <summary>A job title employees can hold and applicants can apply for.</summary>
public class Position
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}
