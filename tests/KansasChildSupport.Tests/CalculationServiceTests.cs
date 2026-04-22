using KansasChildSupport.Web.Models;
using KansasChildSupport.Web.Models.Steps;
using KansasChildSupport.Web.Services;
using Xunit;

namespace KansasChildSupport.Tests;

public class CalculationServiceTests
{
    private static CalculationService CreateService()
    {
        var schedule = new ScheduleLookupService();
        var childCare = new ChildTaxCreditService();
        return new CalculationService(schedule, childCare);
    }

    private static WorksheetSession CreateBasicSession(
        decimal p1Income, decimal p2Income,
        string p1IsPrimary = "Party1",
        List<(string name, DateTime dob)>? children = null)
    {
        children ??= new() { ("Child", DateTime.Today.AddYears(-7)) }; // age 7 = group 6-11

        var session = new WorksheetSession
        {
            CaseInfo = new Step01_CaseInfoModel
            {
                Party1Name = "Alice",
                Party2Name = "Bob",
                County = "Johnson",
                PrimaryCustody = p1IsPrimary
            },
            Children = new Step02_ChildrenModel
            {
                Children = children.Select(c => new ChildEntry
                {
                    FirstName = c.name,
                    DateOfBirth = c.dob
                }).ToList()
            },
            Parent1Income = new Step03_Parent1IncomeModel
            {
                HasEmploymentIncome = true,
                MonthlyGrossEmployment = p1Income
            },
            Parent2Income = new Step04_Parent2IncomeModel
            {
                HasEmploymentIncome = true,
                MonthlyGrossEmployment = p2Income
            },
            ParentingTime = new Step05_ParentingTimeModel { NonPrimaryTimePercent = "No" },
            InsuranceChildcare = new Step06_InsuranceChildcareModel(),
            Adjustments = new Step07_AdjustmentsModel()
        };
        return session;
    }

    // Test 1: Basic 1-child calculation
    [Fact]
    public void Test1_BasicOneChildCalculation()
    {
        var session = CreateBasicSession(3000m, 2000m);
        var svc = CreateService();
        var result = svc.Calculate(session);

        // D.2 should be $5,000
        Assert.Equal(5000m, result.D2);

        // D.3 proportions: 60% / 40%
        Assert.Equal(0.600m, result.P1_D3);
        Assert.Equal(0.400m, result.P2_D3);

        // D.4 should be from schedule (1 child, age 6-11, combined $5000)
        Assert.True(result.D4 > 0, "D4 should be positive");

        // D.5 values
        Assert.Equal(Math.Round(result.P1_D3 * result.D4, MidpointRounding.AwayFromZero), result.P1_D5);
        Assert.Equal(Math.Round(result.P2_D3 * result.D4, MidpointRounding.AwayFromZero), result.P2_D5);

        // Line N should be positive
        Assert.True(result.N >= 0, "Final amount N should be non-negative");
    }

    // Test 2: Two children, different age groups
    [Fact]
    public void Test2_TwoChildrenDifferentAgeGroups()
    {
        var children = new List<(string name, DateTime dob)>
        {
            ("Child1", DateTime.Today.AddYears(-3)), // age 3 = group 0-5
            ("Child2", DateTime.Today.AddYears(-14)) // age 14 = group 12-18
        };
        var session = CreateBasicSession(4500m, 3500m, children: children);
        var svc = CreateService();
        var result = svc.Calculate(session);

        Assert.Equal(8000m, result.D2);
        Assert.True(result.D4 > 0, "D4 (2 children) should be positive");

        // Should have been looked up with numChildren=2 for each age group
        // Let's verify using the schedule
        var schedule = new ScheduleLookupService();
        var expected = schedule.Lookup(2, 8000m, "0-5") + schedule.Lookup(2, 8000m, "12-18");
        Assert.Equal(expected, result.D4);
    }

    // Test 3: Parenting time adjustment - 40% = 20% reduction
    [Fact]
    public void Test3_ParentingTimeAdjustment40Percent()
    {
        var session = CreateBasicSession(3000m, 2000m);
        // P1 is primary, P2 is non-primary with 40% time
        session.ParentingTime = new Step05_ParentingTimeModel
        {
            NonPrimaryTimePercent = "Yes",
            AdjustmentType = "Percentage",
            PercentageOfTime = 40m
        };
        var svc = CreateService();
        var result = svc.Calculate(session);

        // E.1.a = 20% of non-primary's D.5 (P2 since P1 is primary)
        var expectedE1a = Math.Round(0.20m * result.P2_D5, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedE1a, result.E1a);
        Assert.Equal(result.E1a, result.E4);
    }

    // Test 4: Post-2018 maintenance adjustment
    [Fact]
    public void Test4_Post2018MaintenanceAdjustment()
    {
        var session = CreateBasicSession(3000m, 2000m);
        session.Parent1Income!.PaysMaintenancePaid = true;
        session.Parent1Income.MonthlyMaintenancePaid = 500m;
        session.Parent1Income.MaintenancePaidPreDecember2018 = false; // post-2018

        var svc = CreateService();
        var result = svc.Calculate(session);

        // Post-2018 maintenance paid is multiplied by 1.25
        Assert.Equal(625m, result.P1_C3); // 500 * 1.25 = 625
    }

    // Test 5: Child care credit reduction
    [Fact]
    public void Test5_ChildCareCreditReduction()
    {
        var session = CreateBasicSession(3000m, 2000m);
        session.InsuranceChildcare = new Step06_InsuranceChildcareModel
        {
            Parent1PaysChildCare = true,
            Parent1MonthlyChildCare = 800m,
            Parent1AnnualGrossIncomeForChildCare = 35000m // 1 child, credit = $60/month
        };

        var svc = CreateService();
        var result = svc.Calculate(session);

        // Credit table: $35,000 → 24% → max $60/month for 1 child
        Assert.Equal(60m, result.P1_ChildCareCredit);
        // Net child care = 800 - 60 = 740
        Assert.Equal(740m, result.P1_G1);
    }

    // Test 6: Zero income edge case
    [Fact]
    public void Test6_ZeroIncomeEdgeCase()
    {
        var session = CreateBasicSession(0m, 2000m);
        var svc = CreateService();

        var result = svc.Calculate(session); // should not throw

        // P1 has $0, P2 has $2000 → combined $2000
        Assert.Equal(2000m, result.D2);
        // With $0 income, proportionate shares should be 0/100 BUT we set equal shares for 0 combined
        // Actually P1=0, P2=2000, so P1.D3=0.000, P2.D3=1.000
        Assert.Equal(0.000m, result.P1_D3);
        Assert.Equal(1.000m, result.P2_D3);
        Assert.True(result.N >= 0);
    }

    // Test 7: Income above schedule maximum
    [Fact]
    public void Test7_IncomeAboveScheduleMaximum()
    {
        var session = CreateBasicSession(12000m, 8000m);
        var svc = CreateService();
        var result = svc.Calculate(session);

        Assert.Equal(20000m, result.D2);
        Assert.True(result.ExtendedFormulaUsed, "Extended formula should be used for $20,000 combined");
        Assert.True(result.D4 > 0, "D4 should still be positive with extended formula");
        Assert.True(result.N > 0, "Final amount should be positive");
    }

    // Test 8: SS dependent benefit < K.3 → L = K.3 - SS benefit
    [Fact]
    public void Test8_SSDependentBenefitPartialCredit()
    {
        var session = CreateBasicSession(3000m, 2000m);
        session.Adjustments = new Step07_AdjustmentsModel
        {
            ChildReceivesSSDependentBenefits = true,
            MonthlySSBenefit = 300m
        };

        var svc = CreateService();
        var result = svc.Calculate(session);

        // K.4 = 300
        Assert.Equal(300m, result.K4);
        // L = K.3 - 300
        Assert.Equal(Math.Max(0, result.K3 - 300m), result.L);
    }

    // Test 9: SS dependent benefit exceeds K.3 → L = $0
    [Fact]
    public void Test9_SSDependentBenefitExceedsObligation()
    {
        // Use low incomes so obligation is small
        var session = CreateBasicSession(500m, 300m);
        session.Adjustments = new Step07_AdjustmentsModel
        {
            ChildReceivesSSDependentBenefits = true,
            MonthlySSBenefit = 400m
        };

        var svc = CreateService();
        var result = svc.Calculate(session);

        // If SS benefit (400) >= K.3, L should be $0
        if (result.K4 >= result.K3)
        {
            Assert.Equal(0m, result.L);
        }
        else
        {
            // Just verify L is non-negative
            Assert.True(result.L >= 0);
        }
    }

    // Test 10: Enforcement fee
    [Fact]
    public void Test10_EnforcementFee()
    {
        var session = CreateBasicSession(3000m, 2000m);
        session.Adjustments = new Step07_AdjustmentsModel
        {
            HasEnforcementFee = true,
            MonthlyEnforcementFee = 20m
        };

        var svc = CreateService();
        var result = svc.Calculate(session);

        // M = 20 * 0.5 = 10
        Assert.Equal(10m, result.M);
        // N = L + M
        Assert.Equal(result.L + result.M, result.N);
    }

    // Additional: proportionate shares sum to 1
    [Fact]
    public void ProportionateSharesSumToOne()
    {
        var session = CreateBasicSession(3000m, 2000m);
        var svc = CreateService();
        var result = svc.Calculate(session);
        Assert.Equal(1.000m, result.P1_D3 + result.P2_D3);
    }

    // Additional: C.5 calculation
    [Fact]
    public void C5_EqualsDomesticGrossMinusDeductionsPlusReceived()
    {
        var session = CreateBasicSession(3000m, 2000m);
        session.Parent1Income!.PaysChildSupportOtherCases = true;
        session.Parent1Income.MonthlyChildSupportPaidOtherCases = 200m;

        var svc = CreateService();
        var result = svc.Calculate(session);

        // C.5 = C.1 - C.2 - C.3 + C.4 = 3000 - 200 - 0 + 0 = 2800
        Assert.Equal(2800m, result.P1_C5);
    }
}
