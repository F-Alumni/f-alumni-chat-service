using Xunit;

namespace ChatService.Tests.Infrastructure;

/// <summary>
/// Custom xUnit fact attribute that conditionally runs when a real SQL Server instance
/// is configured and reachable for 'FptAlumni_Guideline_Test_Db'.
/// If SQL Server is unavailable or unconfigured, the test is cleanly reported as SKIPPED.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        var (isEligible, skipReason) = SqlServerTestEnvironment.EvaluateEligibility();
        if (!isEligible)
        {
            Skip = skipReason;
        }
    }
}
