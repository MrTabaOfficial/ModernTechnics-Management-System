using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Security;
using ModernTechnics.Core.Services;
using ModernTechnics.Infrastructure.Data;

namespace ModernTechnics.Infrastructure.Services;

public sealed class AuthService(IDbContextFactory<AppDbContext> contextFactory, IPasswordHasher hasher)
    : IAuthService
{
    public async Task<Result<UserAccount>> SignInAsync(
        string email, string password, CancellationToken ct = default)
    {
        var normalizedEmail = UserService.NormalizeEmail(email);

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        // The same error for "no such user" and "wrong password" so accounts cannot be enumerated.
        return user is not null && hasher.Verify(password, user.PasswordHash)
            ? Result.Success(user)
            : Result.Failure<UserAccount>(new Error(ErrorCodes.InvalidCredentials));
    }
}
