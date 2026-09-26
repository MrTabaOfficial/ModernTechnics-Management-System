using ModernTechnics.Core.Common;

namespace ModernTechnics.Tests.Support;

public static class ResultAssert
{
    public static void Succeeded(Result result) =>
        Assert.True(result.IsSuccess, "Expected success but got: " + Describe(result));

    public static void Failed(Result result, string code, string? field = null)
    {
        Assert.True(result.IsFailure, $"Expected failure '{code}' but the operation succeeded.");
        Assert.Contains(result.Errors, e => e.Code == code && (field is null || e.Field == field));
    }

    private static string Describe(Result result) =>
        string.Join(", ", result.Errors.Select(e => $"{e.Code}({e.Field})"));
}
