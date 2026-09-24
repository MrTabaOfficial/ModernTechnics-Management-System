using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Security;
using ModernTechnics.Core.Services;
using ModernTechnics.Core.Validation;
using ModernTechnics.Infrastructure.Data;

namespace ModernTechnics.Infrastructure.Services;

public sealed class UserService(IDbContextFactory<AppDbContext> contextFactory, IPasswordHasher hasher)
    : IUserService
{
    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    public async Task<IReadOnlyList<UserAccount>> ListAsync(string? search = null, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var query = db.Users.AsNoTracking();

        if (Search.ToPattern(search) is { } pattern)
        {
            query = query.Where(u => EF.Functions.Like(u.Email, pattern, Search.Escape));
        }

        return await query.OrderBy(u => u.Email).ToListAsync(ct);
    }

    public async Task<Result<UserAccount>> CreateAsync(
        string email, string password, Role role, CancellationToken ct = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var validation = new Validator()
            .Email(normalizedEmail, nameof(UserAccount.Email))
            .Password(password, nameof(UserAccount.PasswordHash))
            .ToResult();
        if (validation.IsFailure)
        {
            return Result.Failure<UserAccount>(validation.Errors);
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
        {
            return Result.Failure<UserAccount>(new Error(ErrorCodes.Duplicate, nameof(UserAccount.Email)));
        }

        var user = new UserAccount { Email = normalizedEmail, PasswordHash = hasher.Hash(password), Role = role };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return Result.Success(user);
    }

    public async Task<Result> UpdateAsync(int id, Role role, string? newPassword, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound));
        }

        if (user.Role == Role.Administrator && role != Role.Administrator && await IsLastAdministratorAsync(db, ct))
        {
            return Result.Failure(new Error(ErrorCodes.LastAdministrator, nameof(UserAccount.Role)));
        }

        if (!string.IsNullOrEmpty(newPassword))
        {
            var validation = new Validator().Password(newPassword, nameof(UserAccount.PasswordHash)).ToResult();
            if (validation.IsFailure)
            {
                return validation;
            }

            user.PasswordHash = hasher.Hash(newPassword);
        }

        user.Role = role;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, int actingUserId, CancellationToken ct = default)
    {
        if (id == actingUserId)
        {
            return Result.Failure(new Error(ErrorCodes.CannotDeleteSelf));
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound));
        }

        if (user.Role == Role.Administrator && await IsLastAdministratorAsync(db, ct))
        {
            return Result.Failure(new Error(ErrorCodes.LastAdministrator));
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static async Task<bool> IsLastAdministratorAsync(AppDbContext db, CancellationToken ct) =>
        await db.Users.CountAsync(u => u.Role == Role.Administrator, ct) <= 1;
}
