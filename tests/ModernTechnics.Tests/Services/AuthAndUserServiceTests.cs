using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Infrastructure.Data;
using ModernTechnics.Tests.Support;

namespace ModernTechnics.Tests.Services;

public class AuthAndUserServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sign_in_succeeds_with_the_right_credentials()
    {
        await using var db = await TestDatabase.CreateAsync();

        var result = await db.Auth.SignInAsync(DemoData.AdministratorEmail, DemoData.Password, Ct);

        ResultAssert.Succeeded(result);
        Assert.Equal(Role.Administrator, result.Value.Role);
    }

    [Fact]
    public async Task Sign_in_ignores_case_and_whitespace_in_the_email()
    {
        await using var db = await TestDatabase.CreateAsync();

        var result = await db.Auth.SignInAsync("  Admin@ModernTechnics.GE ", DemoData.Password, Ct);

        ResultAssert.Succeeded(result);
    }

    [Theory]
    [InlineData(DemoData.AdministratorEmail, "wrong-password")]
    [InlineData("nobody@moderntechnics.ge", DemoData.Password)]
    [InlineData("' or 1=1 --", "' or 1=1 --")]
    public async Task Sign_in_fails_with_the_same_error_for_any_bad_credentials(string email, string password)
    {
        await using var db = await TestDatabase.CreateAsync();

        var result = await db.Auth.SignInAsync(email, password, Ct);

        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorCodes.InvalidCredentials, error.Code);
    }

    [Fact]
    public async Task Passwords_are_never_stored_in_plain_text()
    {
        await using var db = await TestDatabase.CreateAsync();
        await db.Users.CreateAsync("new@moderntechnics.ge", "Plaintext99", Role.Manager, Ct);

        await using var context = db.CreateDbContext();
        var hashes = await context.Users.Select(u => u.PasswordHash).ToListAsync(Ct);

        Assert.DoesNotContain(hashes, h => h.Contains("Plaintext99") || h.Contains(DemoData.Password));
    }

    [Fact]
    public async Task Created_user_can_sign_in()
    {
        await using var db = await TestDatabase.CreateAsync();

        ResultAssert.Succeeded(await db.Users.CreateAsync("New@ModernTechnics.ge", "Welcome2026", Role.SalesAssociate, Ct));
        var signIn = await db.Auth.SignInAsync("new@moderntechnics.ge", "Welcome2026", Ct);

        ResultAssert.Succeeded(signIn);
        Assert.Equal(Role.SalesAssociate, signIn.Value.Role);
    }

    [Fact]
    public async Task Create_rejects_duplicate_email_and_weak_password()
    {
        await using var db = await TestDatabase.CreateAsync();

        ResultAssert.Failed(
            await db.Users.CreateAsync(DemoData.ManagerEmail.ToUpperInvariant(), "Welcome2026", Role.Manager, Ct),
            ErrorCodes.Duplicate, nameof(UserAccount.Email));
        ResultAssert.Failed(
            await db.Users.CreateAsync("weak@moderntechnics.ge", "abc", Role.Manager, Ct),
            ErrorCodes.WeakPassword);
    }

    [Fact]
    public async Task Update_changes_role_and_password()
    {
        await using var db = await TestDatabase.CreateAsync();
        var sales = (await db.Users.ListAsync("sales", Ct)).Single();

        ResultAssert.Succeeded(await db.Users.UpdateAsync(sales.Id, Role.Manager, "Changed2026", Ct));

        Assert.True((await db.Auth.SignInAsync(DemoData.SalesEmail, DemoData.Password, Ct)).IsFailure);
        var signIn = await db.Auth.SignInAsync(DemoData.SalesEmail, "Changed2026", Ct);
        Assert.Equal(Role.Manager, signIn.Value.Role);
    }

    [Fact]
    public async Task Update_without_a_new_password_keeps_the_old_one()
    {
        await using var db = await TestDatabase.CreateAsync();
        var sales = (await db.Users.ListAsync("sales", Ct)).Single();

        ResultAssert.Succeeded(await db.Users.UpdateAsync(sales.Id, Role.Manager, newPassword: null, Ct));

        ResultAssert.Succeeded(await db.Auth.SignInAsync(DemoData.SalesEmail, DemoData.Password, Ct));
    }

    [Fact]
    public async Task The_last_administrator_cannot_be_demoted_or_deleted()
    {
        await using var db = await TestDatabase.CreateAsync();
        var users = await db.Users.ListAsync(ct: Ct);
        var admin = users.Single(u => u.Role == Role.Administrator);
        var manager = users.Single(u => u.Role == Role.Manager);

        ResultAssert.Failed(await db.Users.UpdateAsync(admin.Id, Role.Manager, null, Ct), ErrorCodes.LastAdministrator);
        ResultAssert.Failed(await db.Users.DeleteAsync(admin.Id, manager.Id, Ct), ErrorCodes.LastAdministrator);
    }

    [Fact]
    public async Task An_administrator_can_be_removed_once_another_exists()
    {
        await using var db = await TestDatabase.CreateAsync();
        var admin = (await db.Users.ListAsync("admin", Ct)).Single();
        var second = (await db.Users.CreateAsync("second@moderntechnics.ge", "Welcome2026", Role.Administrator, Ct)).Value;

        ResultAssert.Succeeded(await db.Users.DeleteAsync(admin.Id, second.Id, Ct));
    }

    [Fact]
    public async Task Users_cannot_delete_their_own_account()
    {
        await using var db = await TestDatabase.CreateAsync();
        var manager = (await db.Users.ListAsync("manager", Ct)).Single();

        ResultAssert.Failed(await db.Users.DeleteAsync(manager.Id, manager.Id, Ct), ErrorCodes.CannotDeleteSelf);
    }

    [Fact]
    public async Task Initializer_seeds_only_once()
    {
        await using var db = await TestDatabase.CreateAsync();

        await db.Initializer.InitializeAsync(Ct);

        Assert.Equal(4, (await db.Users.ListAsync(ct: Ct)).Count);
    }
}
