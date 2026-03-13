namespace KansasChildSupport.Web.Models.Steps;

public class Step07_AdjustmentsModel
{
    // Special needs
    public bool HasSpecialNeeds { get; set; }
    public decimal MonthlySpecialNeedsCost { get; set; }

    // Income tax dependency exemption
    public bool HasTaxExemptionDisagreement { get; set; }
    public decimal MonthlyTaxAdjustment { get; set; }

    // Support past age 18
    public bool HasSupportPastMajority { get; set; }
    public decimal MonthlySupportPastMajority { get; set; }

    // Overall financial condition
    public bool HasOverallFinancialConditionAdjustment { get; set; }
    public string? OverallFinancialConditionDescription { get; set; }
    public decimal MonthlyOverallFinancialAdjustment { get; set; } // can be positive or negative

    // Social Security dependent benefits
    public bool ChildReceivesSSDependentBenefits { get; set; }
    public decimal MonthlySSBenefit { get; set; }

    // Enforcement fee
    public bool HasEnforcementFee { get; set; }
    public decimal MonthlyEnforcementFee { get; set; }

    // Bonus income
    public bool EitherParentHasBonusIncome { get; set; }
    public string BonusMethod { get; set; } = ""; // "Averaging", "Percentage"
    public decimal BonusMonthlyAmount { get; set; }
    public decimal BonusPercentage { get; set; }
}
