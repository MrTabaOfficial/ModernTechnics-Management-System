using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;
using ModernTechnics.Core.Validation;
using ModernTechnics.Infrastructure.Data;

namespace ModernTechnics.Infrastructure.Services;

public sealed class JobApplicationService(IDbContextFactory<AppDbContext> contextFactory, TimeProvider clock)
    : IJobApplicationService
{
    public async Task<IReadOnlyList<JobApplication>> ListAsync(
        string? search = null, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var query = db.JobApplications.AsNoTracking().Include(a => a.Position).AsQueryable();

        if (Search.ToPattern(search) is { } pattern)
        {
            query = query.Where(a =>
                EF.Functions.Like(a.FirstName, pattern, Search.Escape)
                || EF.Functions.Like(a.LastName, pattern, Search.Escape)
                || EF.Functions.Like(a.Phone, pattern, Search.Escape)
                || EF.Functions.Like(a.Position!.Name, pattern, Search.Escape));
        }

        return await query.OrderByDescending(a => a.SubmittedAtUtc).ToListAsync(ct);
    }

    public async Task<Result> SubmitAsync(JobApplication application, CancellationToken ct = default)
    {
        application.FirstName = application.FirstName.Trim();
        application.LastName = application.LastName.Trim();
        application.Phone = application.Phone.Trim();
        application.MotivationLetter = application.MotivationLetter.Trim();

        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var validation = EntityValidators.Validate(application, today);
        if (validation.IsFailure)
        {
            return validation;
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        if (!await db.Positions.AnyAsync(p => p.Id == application.PositionId, ct))
        {
            return Result.Failure(new Error(ErrorCodes.NotFound, nameof(JobApplication.PositionId)));
        }

        application.Id = 0;
        application.Position = null;
        application.Status = ApplicationStatus.New;
        application.SubmittedAtUtc = clock.GetUtcNow().UtcDateTime;
        db.JobApplications.Add(application);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetStatusAsync(int id, ApplicationStatus status, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var application = await db.JobApplications.FindAsync([id], ct);
        if (application is null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound));
        }

        application.Status = status;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var deleted = await db.JobApplications.Where(a => a.Id == id).ExecuteDeleteAsync(ct);
        return deleted > 0 ? Result.Success() : Result.Failure(new Error(ErrorCodes.NotFound));
    }
}
