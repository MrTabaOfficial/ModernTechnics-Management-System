namespace ModernTechnics.Core.Domain;

/// <summary>A sign-in identity. Only a salted hash of the password is stored.</summary>
public class UserAccount
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; }
}
