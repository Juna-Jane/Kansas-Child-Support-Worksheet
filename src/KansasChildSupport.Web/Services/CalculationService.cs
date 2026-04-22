using KansasChildSupport.Web.Models;
using KansasChildSupport.Web.Models.Steps;

namespace KansasChildSupport.Web.Services;

public interface ICalculationService
{
    WorksheetResult Calculate(WorksheetSession session);
}

public class CalculationService : ICalculationService
{
    private readonly IScheduleLookupService _schedule;
    private readonly IChildTaxCreditService _childTaxCredit;

    // 2025 Federal poverty guideline for household of 1
    private const decimal FederalPovertyGuidelineMonthly = 1255m;

    public CalculationService(IScheduleLookupService schedule, IChildTaxCreditService childTaxCredit)
    {
        _schedule = schedule;
        _childTaxCredit = childTaxCredit;
    }

    public WorksheetResult Calculate(WorksheetSession session)
    {
        var result = new WorksheetResult();
        var p1Income = session.Parent1Income ?? new Step03_Parent1IncomeModel();
        var p2Income = session.Parent2Income ?? new Step04_Parent2IncomeModel();
        var children = session.Children?.Children ?? new List<ChildEntry>();
        var parenting = session.ParentingTime ?? new Step05_ParentingTimeModel();
        var insurance = session.InsuranceChildcare ?? new Step06_InsuranceChildcareModel();
        var adjustments = session.Adjustments ?? new Step07_AdjustmentsModel();
        var caseInfo = session.CaseInfo ?? new Step01_CaseInfoModel();

        int numChildren = Math.Max(1, children.Count);
        bool p1IsPrimary = caseInfo.PrimaryCustody != "Party2";

        // ====== SECTION A: Domestic Gross Income ======
        result.P1_A1 = Round(ComputeGrossIncome(p1Income));
        result.P2_A1 = Round(ComputeGrossIncome(p2Income));

        // ====== SECTION B: Self-Employment ======
        result.P1_B1 = p1Income.IsSelfEmployed ? Round(p1Income.MonthlyGrossSelfEmployment) : 0;
        result.P1_B2 = p1Income.IsSelfEmployed ? Round(p1Income.MonthlyBusinessExpenses) : 0;
        result.P1_B3 = Round(result.P1_B1 - result.P1_B2);
        result.P2_B1 = p2Income.IsSelfEmployed ? Round(p2Income.MonthlyGrossSelfEmployment) : 0;
        result.P2_B2 = p2Income.IsSelfEmployed ? Round(p2Income.MonthlyBusinessExpenses) : 0;
        result.P2_B3 = Round(result.P2_B1 - result.P2_B2);

        // ====== SECTION C: Adjustments ======
        // C.1 = A.1 + B.3 (employment gross + net self-employment)
        // But we already included self-employment in A.1 via ComputeGrossIncome.
        // Per guidelines, Section A is all wage/other income, Section B is self-employment.
        // C.1 = total domestic gross = employment-only + net self-employment
        decimal p1Employment = 0;
        if (p1Income.HasEmploymentIncome) p1Employment += p1Income.MonthlyGrossEmployment;
        if (p1Income.HasBonusIncome) p1Employment += p1Income.MonthlyBonusAverage;
        if (p1Income.HasMilitaryPay) p1Employment += p1Income.MonthlyMilitaryPay;
        if (p1Income.HasDisabilityPayments) p1Employment += p1Income.MonthlyDisabilityPayments;
        if (p1Income.HasUnemploymentCompensation) p1Employment += p1Income.MonthlyUnemploymentCompensation;
        if (p1Income.HasRetirementIncome) p1Employment += p1Income.MonthlyRetirementIncome;
        if (p1Income.HasOtherIncome) p1Employment += p1Income.MonthlyOtherIncome;

        decimal p2Employment = 0;
        if (p2Income.HasEmploymentIncome) p2Employment += p2Income.MonthlyGrossEmployment;
        if (p2Income.HasBonusIncome) p2Employment += p2Income.MonthlyBonusAverage;
        if (p2Income.HasMilitaryPay) p2Employment += p2Income.MonthlyMilitaryPay;
        if (p2Income.HasDisabilityPayments) p2Employment += p2Income.MonthlyDisabilityPayments;
        if (p2Income.HasUnemploymentCompensation) p2Employment += p2Income.MonthlyUnemploymentCompensation;
        if (p2Income.HasRetirementIncome) p2Employment += p2Income.MonthlyRetirementIncome;
        if (p2Income.HasOtherIncome) p2Employment += p2Income.MonthlyOtherIncome;

        result.P1_A1 = Round(p1Employment);
        result.P2_A1 = Round(p2Employment);

        result.P1_C1 = Round(result.P1_A1 + result.P1_B3);
        result.P2_C1 = Round(result.P2_A1 + result.P2_B3);

        // C.2 = Child support paid in other cases
        result.P1_C2 = p1Income.PaysChildSupportOtherCases ? Round(p1Income.MonthlyChildSupportPaidOtherCases) : 0;
        result.P2_C2 = p2Income.PaysChildSupportOtherCases ? Round(p2Income.MonthlyChildSupportPaidOtherCases) : 0;

        // C.3 = Maintenance paid (adjusted for pre/post 2018)
        result.P1_C3 = p1Income.PaysMaintenancePaid ? AdjustMaintenance(p1Income.MonthlyMaintenancePaid, p1Income.MaintenancePaidPreDecember2018, true) : 0;
        result.P2_C3 = p2Income.PaysMaintenancePaid ? AdjustMaintenance(p2Income.MonthlyMaintenancePaid, p2Income.MaintenancePaidPreDecember2018, true) : 0;

        // C.4 = Maintenance received (adjusted)
        result.P1_C4 = p1Income.ReceivesMaintenance ? AdjustMaintenance(p1Income.MonthlyMaintenanceReceived, p1Income.MaintenanceReceivedPreDecember2018, false) : 0;
        result.P2_C4 = p2Income.ReceivesMaintenance ? AdjustMaintenance(p2Income.MonthlyMaintenanceReceived, p2Income.MaintenanceReceivedPreDecember2018, false) : 0;

        // C.5 = Child Support Income
        result.P1_C5 = Round(result.P1_C1 - result.P1_C2 - result.P1_C3 + result.P1_C4);
        result.P2_C5 = Round(result.P2_C1 - result.P2_C2 - result.P2_C3 + result.P2_C4);

        // ====== SECTION D ======
        result.P1_D1 = result.P1_C5;
        result.P2_D1 = result.P2_C5;
        result.D2 = Round(result.P1_D1 + result.P2_D1);

        if (result.D2 == 0)
        {
            // Zero combined income edge case — use equal shares
            result.P1_D3 = 0.500m;
            result.P2_D3 = 0.500m;
        }
        else
        {
            result.P1_D3 = Math.Round(result.P1_D1 / result.D2, 3, MidpointRounding.AwayFromZero);
            result.P2_D3 = Math.Round(result.P2_D1 / result.D2, 3, MidpointRounding.AwayFromZero);
            // Ensure they sum to exactly 1.000
            result.P2_D3 = Math.Round(1m - result.P1_D3, 3, MidpointRounding.AwayFromZero);
        }

        // D.4 = Gross child support obligation from schedule
        // For multiple children in different age groups, use same numChildren for each child lookup
        if (result.D2 > 18000m)
            result.ExtendedFormulaUsed = true;

        if (children.Count == 0)
        {
            result.D4 = _schedule.Lookup(1, result.D2, "12-18");
        }
        else
        {
            decimal totalD4 = 0;
            foreach (var child in children)
            {
                var ageGroup = child.GetAgeGroup();
                totalD4 += _schedule.Lookup(numChildren, result.D2, ageGroup);
            }
            // D.4 is the sum over all children (each looked up at same numChildren, same income)
            result.D4 = Round(totalD4);
        }

        result.P1_D5 = Round(result.P1_D3 * result.D4);
        result.P2_D5 = Round(result.P2_D3 * result.D4);

        // ====== SECTION E: Parenting Time Adjustment ======
        // Determine non-primary parent
        decimal nonPrimaryD5 = p1IsPrimary ? result.P2_D5 : result.P1_D5;
        decimal primaryD5 = p1IsPrimary ? result.P1_D5 : result.P2_D5;

        if (parenting.IsEqualParentingTime)
        {
            // 50/50 Direct Expense Formula: E.2 = (higher D5 - lower D5) / 2
            var higher = Math.Max(result.P1_D5, result.P2_D5);
            var lower = Math.Min(result.P1_D5, result.P2_D5);
            result.E2 = Round((higher - lower) / 2m);
            result.E4 = result.E2;
            result.SharedResidencyNote = "50/50 Direct Expense Formula used (Section VI.F).";
        }
        else if (parenting.NonPrimaryTimePercent == "Yes")
        {
            switch (parenting.AdjustmentType)
            {
                case "Percentage":
                    var adjRate = parenting.GetPercentageAdjustmentRate();
                    result.E1a = Round(adjRate * nonPrimaryD5);
                    result.E4 = result.E1a;
                    break;
                case "ActualCost":
                    result.E1b = Round(parenting.ActualExtraCosts);
                    result.E4 = result.E1b;
                    break;
                case "ExtendedTime":
                    var extRate = Math.Min(parenting.ExtendedTimeReductionPercent / 100m, 0.50m);
                    result.E1c = Round(extRate * nonPrimaryD5);
                    result.E4 = result.E1c;
                    break;
            }
        }

        // ====== SECTION F: Health Insurance ======
        result.P1_F1 = insurance.Parent1PaysInsurance ? Round(insurance.Parent1MonthlyInsurancePremium) : 0;
        result.P2_F1 = insurance.Parent2PaysInsurance ? Round(insurance.Parent2MonthlyInsurancePremium) : 0;
        result.F1Total = Round(result.P1_F1 + result.P2_F1);
        result.P1_F2 = Round(result.P1_D3 * result.F1Total);
        result.P2_F2 = Round(result.P2_D3 * result.F1Total);

        // ====== SECTION G: Child Care ======
        decimal p1ChildCareGross = insurance.Parent1PaysChildCare ? Round(insurance.Parent1MonthlyChildCare) : 0;
        decimal p2ChildCareGross = insurance.Parent2PaysChildCare ? Round(insurance.Parent2MonthlyChildCare) : 0;

        result.P1_G1_Gross = p1ChildCareGross;
        result.P2_G1_Gross = p2ChildCareGross;

        if (p1ChildCareGross > 0)
        {
            var credit = _childTaxCredit.GetMonthlyCredit(insurance.Parent1AnnualGrossIncomeForChildCare, numChildren);
            result.P1_ChildCareCredit = credit;
            result.P1_G1 = Math.Max(0, Round(p1ChildCareGross - credit));
        }
        if (p2ChildCareGross > 0)
        {
            var credit = _childTaxCredit.GetMonthlyCredit(insurance.Parent2AnnualGrossIncomeForChildCare, numChildren);
            result.P2_ChildCareCredit = credit;
            result.P2_G1 = Math.Max(0, Round(p2ChildCareGross - credit));
        }

        result.G1Total = Round(result.P1_G1 + result.P2_G1);
        result.P1_G2 = Round(result.P1_D3 * result.G1Total);
        result.P2_G2 = Round(result.P2_D3 * result.G1Total);

        // ====== SECTION H: Proportionate Child Support Obligation ======
        if (p1IsPrimary)
        {
            result.P1_H1 = Round(result.P1_D5 + result.P1_F2 + result.P1_G2);
            result.P2_H1 = Round(result.P2_D5 - result.E4 + result.P2_F2 + result.P2_G2);
        }
        else
        {
            result.P1_H1 = Round(result.P1_D5 - result.E4 + result.P1_F2 + result.P1_G2);
            result.P2_H1 = Round(result.P2_D5 + result.P2_F2 + result.P2_G2);
        }

        // ====== SECTION I: Basic Child Support Obligation ======
        result.P1_I1 = Round(result.P1_F1 + result.P1_G1);
        result.P2_I1 = Round(result.P2_F1 + result.P2_G1);
        result.P1_I2 = Round(result.P1_H1 - result.P1_I1);
        result.P2_I2 = Round(result.P2_H1 - result.P2_I1);

        // ====== SECTION J: Child Support Adjustments ======
        result.J1 = parenting.HasLongDistanceCosts ? Round(parenting.MonthlyLongDistanceCosts) : 0;
        result.J2 = adjustments.HasTaxExemptionDisagreement ? Round(adjustments.MonthlyTaxAdjustment) : 0;
        result.J3 = adjustments.HasSpecialNeeds ? Round(adjustments.MonthlySpecialNeedsCost) : 0;
        result.J4 = adjustments.HasSupportPastMajority ? Round(adjustments.MonthlySupportPastMajority) : 0;
        result.J5 = adjustments.HasOverallFinancialConditionAdjustment ? Round(adjustments.MonthlyOverallFinancialAdjustment) : 0;
        result.J6 = Round(result.J1 + result.J2 + result.J3 + result.J4 + result.J5);

        // ====== SECTION K ======
        // K.1 = non-primary parent's I.2
        result.K1 = p1IsPrimary ? result.P2_I2 : result.P1_I2;
        result.K2 = result.J6;
        result.K3 = Round(result.K1 + result.K2);

        // K.4 = SS dependent benefit credit
        result.K4 = adjustments.ChildReceivesSSDependentBenefits ? Round(adjustments.MonthlySSBenefit) : 0;

        // Ability to Pay check
        decimal nonPrimaryD1 = p1IsPrimary ? result.P2_D1 : result.P1_D1;
        result.IncomeAvailableForSupport = Round(nonPrimaryD1 - FederalPovertyGuidelineMonthly);
        if (result.K3 > result.IncomeAvailableForSupport && result.IncomeAvailableForSupport > 0)
        {
            result.AbilityToPayFlag = true;
            result.Notes.Add("The calculated amount may exceed what this parent can pay. The court must consider their ability to pay under Section VI.G of the Guidelines.");
        }

        // ====== SECTION L ======
        result.L = Math.Max(0, Round(result.K3 - result.K4));

        // ====== SECTION M ======
        result.M = adjustments.HasEnforcementFee ? Round(adjustments.MonthlyEnforcementFee * 0.5m) : 0;

        // ====== SECTION N ======
        result.N = Round(result.L + result.M);

        // Flags
        if (p2Income is Step04_Parent2IncomeModel p2Model && p2Model.IncomeUnknown)
        {
            result.IncomeUnknownFlag = true;
            result.Notes.Add("Note: Other parent's income was estimated/unknown. Court may require income disclosure.");
        }

        if (result.ExtendedFormulaUsed)
        {
            result.Notes.Add("Combined income exceeds schedule maximum — extended formula used (discretionary per guidelines).");
        }

        return result;
    }

    private decimal ComputeGrossIncome(ParentIncomeModel income)
    {
        var total = 0m;
        if (income.HasEmploymentIncome) total += income.MonthlyGrossEmployment;
        if (income.IsSelfEmployed) total += income.MonthlyGrossSelfEmployment - income.MonthlyBusinessExpenses;
        if (income.HasBonusIncome) total += income.MonthlyBonusAverage;
        if (income.HasMilitaryPay) total += income.MonthlyMilitaryPay;
        if (income.HasDisabilityPayments) total += income.MonthlyDisabilityPayments;
        if (income.HasUnemploymentCompensation) total += income.MonthlyUnemploymentCompensation;
        if (income.HasRetirementIncome) total += income.MonthlyRetirementIncome;
        if (income.HasOtherIncome) total += income.MonthlyOtherIncome;
        return total;
    }

    /// <summary>
    /// Adjusts maintenance for tax law changes post-2018.
    /// Pre-2018: use actual amount (tax-neutral).
    /// Post-2018: multiply by 1.25 for payor (add 25% to represent lost deduction).
    ///            For payee receiving, treat as regular income (no adjustment needed).
    /// </summary>
    private decimal AdjustMaintenance(decimal amount, bool preDecember2018, bool isPaying)
    {
        if (preDecember2018)
            return Round(amount);
        // Post-2018: per Section III.C.2, multiply by 1.25 when paid
        if (isPaying)
            return Round(amount * 1.25m);
        // Received: no adjustment under post-2018 law (not taxable income)
        return Round(amount);
    }

    private static decimal Round(decimal value)
        => Math.Round(value, 0, MidpointRounding.AwayFromZero);
}
