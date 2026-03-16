using Xunit;

namespace TaxiDispatch.Tests;

public class ExtensionMethodsTests
{
    [Fact]
    public void StartOfWeek_ReturnsMonday_ForWednesdayDate()
    {
        var input = new DateTime(2026, 3, 4); // Wednesday
        var result = input.StartOfWeek(DayOfWeek.Monday);

        Assert.Equal(new DateTime(2026, 3, 2), result);
    }

    [Fact]
    public void To2359_SetsTimeToEndOfDay()
    {
        var input = new DateTime(2026, 3, 4, 10, 11, 12);
        var result = input.To2359();

        Assert.Equal(23, result.Hour);
        Assert.Equal(59, result.Minute);
        Assert.Equal(59, result.Second);
    }

    [Fact]
    public void RemoveExtraSpaces_TrimsAndNormalizesWhitespace()
    {
        var input = "  Ace    Taxis   Dorset  ";
        var result = input.RemoveExtraSpaces();

        Assert.Equal("Ace Taxis Dorset", result);
    }

    [Fact]
    public void Substring_ReturnsTextBetweenAnchors()
    {
        var input = "from:[booking-123]:to";
        var result = input.Substring("from:[", "]:to");

        Assert.Equal("booking-123", result);
    }

    [Fact]
    public void Substring_ThrowsWhenFromAnchorMissing()
    {
        var input = "alpha:beta:gamma";

        Assert.Throws<ArgumentException>(() => input.Substring("missing:", ":gamma"));
    }
}

