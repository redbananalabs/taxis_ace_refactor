using System.Reflection;
using System.Text.RegularExpressions;
using TaxiDispatch.API.Controllers;
using TaxiDispatch.Data;

namespace TaxiDispatch.Tests;

public class AdminUIControllerThinnessTests
{
    [Fact]
    public void AdminUIController_ShouldNotInjectTaxiDispatchContext()
    {
        var constructor = typeof(AdminUIController).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToList();

        Assert.DoesNotContain(typeof(TaxiDispatchContext), parameterTypes);
    }

    [Fact]
    public void AdminUIController_ShouldNotContainDirectEfCalls()
    {
        var source = File.ReadAllText(GetControllerPath());
        var scrubbed = ScrubCommentsAndStrings(source);

        var forbiddenTokens = new[]
        {
            "ExecuteUpdateAsync(",
            "ExecuteDeleteAsync(",
            "ToListAsync(",
            "FirstOrDefaultAsync(",
            "AsNoTracking(",
            "Include(",
            "_db."
        };

        foreach (var token in forbiddenTokens)
        {
            Assert.DoesNotContain(token, scrubbed, StringComparison.Ordinal);
        }
    }

    private static string GetControllerPath()
    {
        var root = GetRepositoryRoot();
        return Path.Combine(root, "TaxiDispatch.API", "Controllers", "AdminUIController.cs");
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
