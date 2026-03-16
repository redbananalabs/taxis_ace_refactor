using System.Text.RegularExpressions;

namespace TaxiDispatch.Tests;

public class DtoStructureTests
{
    [Fact]
    public void DtoFiles_ShouldContainSingleTopLevelPublicType()
    {
        var dtoFiles = GetDtoFiles();
        var offenders = new List<string>();

        foreach (var file in dtoFiles)
        {
            var count = GetTopLevelPublicTypeNames(file).Count;
            if (count > 1)
            {
                var relativePath = Path.GetRelativePath(GetRepositoryRoot(), file);
                offenders.Add($"{relativePath} ({count})");
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"DTO files with more than one top-level public type:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void DtoTypes_ShouldBeReferencedBeyondTheirDeclaration()
    {
        var dtoFiles = GetDtoFiles();
        var dtoTypeNames = dtoFiles
            .SelectMany(GetTopLevelPublicTypeNames)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var sourceFiles = GetProjectSourceFiles();
        var texts = sourceFiles.ToDictionary(x => x, File.ReadAllText);
        var declarationOnly = new List<string>();

        foreach (var typeName in dtoTypeNames)
        {
            var pattern = $@"\b{Regex.Escape(typeName)}\b";
            var count = texts.Values.Sum(text => Regex.Matches(text, pattern).Count);

            if (count <= 1)
            {
                declarationOnly.Add(typeName);
            }
        }

        Assert.True(
            declarationOnly.Count == 0,
            $"Declaration-only DTO types found:{Environment.NewLine}{string.Join(Environment.NewLine, declarationOnly.OrderBy(x => x))}");
    }

    private static IReadOnlyList<string> GetDtoFiles()
    {
        var root = GetRepositoryRoot();
        var dtoRoot = Path.Combine(root, "TaxiDispatch.Lib", "DTOs");
        return Directory.GetFiles(dtoRoot, "*.cs", SearchOption.AllDirectories);
    }

    private static IReadOnlyList<string> GetProjectSourceFiles()
    {
        var root = GetRepositoryRoot();
        var sourceRoots = new[]
        {
            Path.Combine(root, "TaxiDispatch.API"),
            Path.Combine(root, "TaxiDispatch.Lib"),
            Path.Combine(root, "TaxiDispatch.Tests")
        };

        return sourceRoots
            .SelectMany(path => Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static IReadOnlyList<string> GetTopLevelPublicTypeNames(string filePath)
    {
        var source = File.ReadAllText(filePath);
        var fileScopedNamespace = Regex.IsMatch(source, @"(?m)^\s*namespace\s+[A-Za-z0-9_.]+\s*;");
        var matches = Regex.Matches(
            source,
            @"(?m)^(?<indent>[ \t]*)public\s+(?:partial\s+)?(?:class|record|struct|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b");

        var names = new List<string>();
        foreach (Match match in matches)
        {
            var indent = match.Groups["indent"].Value;
            var indentLength = indent.Replace("\t", "    ", StringComparison.Ordinal).Length;
            var isTopLevel = fileScopedNamespace ? indentLength == 0 : indentLength == 4;

            if (isTopLevel)
            {
                names.Add(match.Groups["name"].Value);
            }
        }

        return names;
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
            throw new InvalidOperationException("Could not locate repository root (TaxiDispatch.sln).");
        }

        return current.FullName;
    }
}
