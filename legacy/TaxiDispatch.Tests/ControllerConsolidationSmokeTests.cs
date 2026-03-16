using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using TaxiDispatch.API.Controllers;
using TaxiDispatch.Data;

namespace TaxiDispatch.Tests;

public class ControllerConsolidationSmokeTests
{
    [Fact]
    public void ApiAssembly_ContainsMergedControllers()
    {
        var apiAssembly = typeof(AuthController).Assembly;
        var exportedNames = apiAssembly
            .GetExportedTypes()
            .Select(t => t.FullName)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(typeof(AuthController).FullName, exportedNames);
        Assert.Contains(typeof(UsersController).FullName, exportedNames);
        Assert.Contains(typeof(MessagingController).FullName, exportedNames);
    }

    [Theory]
    [InlineData(typeof(AuthController))]
    [InlineData(typeof(UsersController))]
    [InlineData(typeof(MessagingController))]
    public void MergedControllers_HaveExpectedBaseRoute(Type controllerType)
    {
        var route = controllerType.GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(route);
        Assert.Equal("api/[controller]", route!.Template);
    }

    [Fact]
    public void MergedControllers_ExposeExpectedKeyActionRoutes()
    {
        AssertActionRoute(typeof(AuthController), nameof(AuthController.AuthenticateUser), "POST", "Authenticate");
        AssertActionRoute(typeof(AuthController), nameof(AuthController.RefreshToken), "GET", "RefreshToken");

        AssertActionRoute(typeof(UsersController), nameof(UsersController.Test), "GET", "Test");
        AssertActionRoute(typeof(UsersController), nameof(UsersController.Register), "POST", "Register");

        AssertActionRoute(typeof(MessagingController), nameof(MessagingController.SendTextMessage), "POST", "SendTextMessage");
        AssertActionRoute(typeof(MessagingController), nameof(MessagingController.SendPushNotification), "POST", "SendPushNotification");
    }

    [Fact]
    public void LibAssembly_DoesNotContainMergedControllerTypes()
    {
        var forbiddenNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "AuthController",
            "UsersController",
            "MessagingController"
        };

        var leaked = typeof(TaxiDispatchContext).Assembly
            .GetTypes()
            .Where(t => forbiddenNames.Contains(t.Name))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        Assert.Empty(leaked);
    }

    private static void AssertActionRoute(
        Type controllerType,
        string methodName,
        string expectedVerb,
        string expectedTemplate)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var httpMethodAttributes = method!
            .GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .ToList();

        Assert.Contains(httpMethodAttributes, attr =>
            attr.HttpMethods.Any(m => string.Equals(m, expectedVerb, StringComparison.OrdinalIgnoreCase)));

        var templates = httpMethodAttributes
            .Select(attr => attr.Template)
            .Concat(method.GetCustomAttributes<RouteAttribute>().Select(attr => attr.Template))
            .Where(template => !string.IsNullOrWhiteSpace(template))
            .ToList();

        Assert.Contains(expectedTemplate, templates!);
    }
}
