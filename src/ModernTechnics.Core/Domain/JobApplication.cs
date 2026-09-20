namespace ModernTechnics.Core.Domain;

public class JobApplication
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }

    public string Phone { get; set; } = string.Empty;

    public int PositionId { get; set; }

    public Position? Position { get; set; }

    public string MotivationLetter { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; }

    public DateTime SubmittedAtUtc { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
