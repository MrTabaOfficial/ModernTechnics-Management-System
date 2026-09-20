namespace ModernTechnics.Core.Domain;

public class Employee
{
    /// <summary>11-digit national personal number; the natural key.</summary>
    public string PersonalId { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public MaritalStatus MaritalStatus { get; set; }

    public int PositionId { get; set; }

    public Position? Position { get; set; }

    /// <summary>Gross monthly salary.</summary>
    public decimal Salary { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
