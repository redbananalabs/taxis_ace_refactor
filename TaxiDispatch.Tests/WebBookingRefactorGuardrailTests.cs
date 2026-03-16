using System.Reflection;
using System.Text.RegularExpressions;
using TaxiDispatch.API.Controllers;
using TaxiDispatch.Data;

namespace TaxiDispatch.Tests;

public class WeBookingControllerThinnessTests
{
    [Fact]
    public void WeBookingController_ShouldNotInjectTaxiDispatchContext()
    {
        var constructor = typeof(WeBookingController).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToList();

        Assert.DoesNotContain(typeof(TaxiDispatchContext), parameterTypes);
    }

    [Fact]
    public void WeBookingController_ShouldNotContainDirectEfOperators()
    {
        var source = File.ReadAllText(GetControllerPath());
        var scrubbed = ScrubCommentsAndStrings(source);

        var forbiddenTokens = new[]
        {
            "Where(",
            "Select(",
            "Include(",
            "ExecuteUpdateAsync(",
            "ExecuteDeleteAsync("
        };

        foreach (var token in forbiddenTokens)
        {
            Assert.DoesNotContain(token, scrubbed, StringComparison.Ordinal);
        }
    }

    private static string GetControllerPath()
    {
        var root = GetRepositoryRoot();
        return Path.Combine(root, "TaxiDispatch.API", "Controllers", "WeBookingController.cs");
    }

    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "TaxiDispatch.sln")))
        {
            current = current.Parent;
        }

        if (current == null)
        {
            throw new InvalidOperationException("Could not locate TaxiDispatch.sln.");
        }

        return current.FullName;
    }

    private static string ScrubCommentsAndStrings(string source)
    {
        var withoutBlockComments = Regex.Replace(
            source,
            @"/\*.*?\*/",
            string.Empty,
            RegexOptions.Singleline);

        var withoutLineComments = Regex.Replace(
            withoutBlockComments,
            @"//.*?$",
            string.Empty,
            RegexOptions.Multiline);

        var withoutVerbatimStrings = Regex.Replace(
            withoutLineComments,
            "@\"(?:[^\"]|\"\")*\"",
            "\"\"",
            RegexOptions.Singleline);

        return Regex.Replace(
            withoutVerbatimStrings,
            "\"(?:\\\\.|[^\"])*\"",
            "\"\"",
            RegexOptions.Singleline);
    }
}

public class WebBookingServiceSourceContractTests
{
    [Fact]
    public void GetWebBookings_ShouldAssignProcessedFilterBackToQuery()
    {
        var source = File.ReadAllText(GetServicePath());
        Assert.Contains("query = query.Where(o => o.Processed == dto.Processed);", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptFlow_ShouldMarkWebBookingAsProcessedAndAccepted()
    {
        var source = File.ReadAllText(GetServicePath());
        Assert.Contains("obj.Processed = true;", source, StringComparison.Ordinal);
        Assert.Contains("obj.Status = WebBookingStatus.Accepted;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectFlow_ShouldMarkWebBookingAsProcessedAndRejected()
    {
        var source = File.ReadAllText(GetServicePath());
        Assert.Contains("obj.Processed = true;", source, StringComparison.Ordinal);
        Assert.Contains("obj.Status = WebBookingStatus.Rejected;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptFlow_ShouldUseCorrectHvsCondition()
    {
        var source = File.ReadAllText(GetServicePath());
        Assert.Contains("obj.AccNo != 9014 && obj.AccNo != 10026", source, StringComparison.Ordinal);
    }

    private static string GetServicePath()
    {
        var root = GetRepositoryRoot();
        return Path.Combine(root, "TaxiDispatch.Lib", "Services", "WebBookingService.cs");
    }

    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "TaxiDispatch.sln")))
        {
            current = current.Parent;
        }

        if (current == null)
        {
            throw new InvalidOperationException("Could not locate TaxiDispatch.sln.");
        }

        return current.FullName;
    }
}
