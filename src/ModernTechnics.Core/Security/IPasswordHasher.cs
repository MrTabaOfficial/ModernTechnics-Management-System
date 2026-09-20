namespace ModernTechnics.Core.Security;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
