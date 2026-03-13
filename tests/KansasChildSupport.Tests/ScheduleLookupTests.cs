using KansasChildSupport.Web.Services;
using Xunit;

namespace KansasChildSupport.Tests;

public class ScheduleLookupTests
{
    private readonly ScheduleLookupService _service = new();

    // Test 1: Exact table value - combined $3,000, 1 child, age 6-11 → $546
    [Fact]
    public void Test1_ExactTableValue_1Child_3000_Age611()
    {
        var result = _service.Lookup(1, 3000m, "6-11");
        // Per the encoded table: $3,000, 1 child, 6-11 → 546
        Assert.Equal(546m, result);
    }

    // Test 2: Interpolation between rows
    [Fact]
    public void Test2_Interpolation_1Child_3050_Age1218()
    {
        var at3000 = _service.Lookup(1, 3000m, "12-18");
        var at3100 = _service.Lookup(1, 3100m, "12-18");
        var at3050 = _service.Lookup(1, 3050m, "12-18");

        // Should be between at3000 and at3100
        Assert.True(at3050 >= at3000, $"Interpolated {at3050} should be >= {at3000}");
        Assert.True(at3050 <= at3100, $"Interpolated {at3050} should be <= {at3100}");

        // Should be approximately the midpoint
        var midpoint = (at3000 + at3100) / 2m;
        var diff = Math.Abs(at3050 - midpoint);
        Assert.True(diff <= 1m, $"Should be within $1 of midpoint. Got {at3050}, midpoint {midpoint}");
    }

    // Test 3: Two-child schedule at $5,000 combined, age 0-5
    [Fact]
    public void Test3_TwoChildSchedule_5000_Age05()
    {
        var result = _service.Lookup(2, 5000m, "0-5");
        // Per encoded table: 2 children, $5,000, 0-5 → 1025
        Assert.Equal(1025m, result);
    }

    // Test 4: Extended formula for income above table maximum
    [Fact]
    public void Test4_ExtendedFormula_1Child_20000_Age1218()
    {
        var result = _service.Lookup(1, 20000m, "12-18");

        // Extended formula: income^0.6386 × 4.8982
        var expected = (decimal)(Math.Pow(20000.0, 0.6386) * 4.8982);
        expected = Math.Round(expected, MidpointRounding.AwayFromZero);

        Assert.Equal(expected, result);
        Assert.True(result > 0, "Extended formula result should be positive");
    }

    // Test 5: Age group multipliers for extended formula
    [Fact]
    public void Test5_ExtendedFormulaAgeGroupMultipliers()
    {
        var base1218 = _service.Lookup(1, 20000m, "12-18");
        var result611 = _service.Lookup(1, 20000m, "6-11");
        var result05 = _service.Lookup(1, 20000m, "0-5");

        // 6-11 = 12-18 × 0.94
        var expected611 = Math.Round(base1218 * 0.94m, MidpointRounding.AwayFromZero);
        Assert.Equal(expected611, result611);

        // 0-5 = 12-18 × 0.84
        var expected05 = Math.Round(base1218 * 0.84m, MidpointRounding.AwayFromZero);
        Assert.Equal(expected05, result05);
    }

    // Test 6: All age groups are in correct order (12-18 > 6-11 > 0-5) for same income
    [Fact]
    public void Test6_AgeGroupOrdering()
    {
        for (int n = 1; n <= 6; n++)
        {
            var v1218 = _service.Lookup(n, 5000m, "12-18");
            var v611 = _service.Lookup(n, 5000m, "6-11");
            var v05 = _service.Lookup(n, 5000m, "0-5");

            Assert.True(v1218 >= v611, $"Children={n}: 12-18 ({v1218}) should be >= 6-11 ({v611})");
            Assert.True(v611 >= v05, $"Children={n}: 6-11 ({v611}) should be >= 0-5 ({v05})");
        }
    }

    // Test 7: More children = higher support (same income)
    [Fact]
    public void Test7_MoreChildrenHigherSupport()
    {
        for (int n = 1; n < 6; n++)
        {
            var current = _service.Lookup(n, 5000m, "12-18");
            var next = _service.Lookup(n + 1, 5000m, "12-18");
            Assert.True(next > current, $"Support for {n+1} children ({next}) should be > {n} children ({current})");
        }
    }

    // Test 8: Higher income = higher support
    [Fact]
    public void Test8_HigherIncomeHigherSupport()
    {
        var low = _service.Lookup(1, 2000m, "6-11");
        var high = _service.Lookup(1, 5000m, "6-11");
        Assert.True(high > low, $"Higher income ({high}) should produce higher support than lower ({low})");
    }

    // Test 9: Clamped to 6 children max
    [Fact]
    public void Test9_ClampToMaxSixChildren()
    {
        var six = _service.Lookup(6, 5000m, "12-18");
        var seven = _service.Lookup(7, 5000m, "12-18");
        Assert.Equal(six, seven); // 7 children clamps to 6
    }

    // Test 10: Below-table income (below $600)
    [Fact]
    public void Test10_BelowTableIncome()
    {
        var result = _service.Lookup(1, 300m, "12-18");
        Assert.True(result > 0, "Even very low income should produce a result");
        Assert.True(result < _service.Lookup(1, 600m, "12-18"), "Low income should produce less support");
    }
}
