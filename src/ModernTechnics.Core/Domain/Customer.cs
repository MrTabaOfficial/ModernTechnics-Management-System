namespace ModernTechnics.Core.Domain;

public class Customer
{
    public string PersonalId { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}";
}
