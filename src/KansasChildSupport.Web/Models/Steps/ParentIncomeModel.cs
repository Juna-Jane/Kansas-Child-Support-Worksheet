namespace KansasChildSupport.Web.Models.Steps;

public abstract class ParentIncomeModel
{
    // Section A - Employment
    public bool HasEmploymentIncome { get; set; }
    public decimal MonthlyGrossEmployment { get; set; }
    public bool HasCafeteriaPlan { get; set; }

    // Section B - Self-Employment
    public bool IsSelfEmployed { get; set; }
    public decimal MonthlyGrossSelfEmployment { get; set; }
    public decimal MonthlyBusinessExpenses { get; set; }

    // Section C - Other Income Sources
    public bool HasBonusIncome { get; set; }
    public decimal MonthlyBonusAverage { get; set; }

    public bool HasMilitaryPay { get; set; }
    public decimal MonthlyMilitaryPay { get; set; }

    public bool HasDisabilityPayments { get; set; }
    public decimal MonthlyDisabilityPayments { get; set; }

    public bool HasUnemploymentCompensation { get; set; }
    public decimal MonthlyUnemploymentCompensation { get; set; }

    public bool HasRetirementIncome { get; set; }
    public decimal MonthlyRetirementIncome { get; set; }

    public bool HasOtherIncome { get; set; }
    public string? OtherIncomeDescription { get; set; }
    public decimal MonthlyOtherIncome { get; set; }

    // Section C - Adjustments (deductions)
    public bool PaysChildSupportOtherCases { get; set; }
    public decimal MonthlyChildSupportPaidOtherCases { get; set; }

    public bool PaysMaintenancePaid { get; set; }
    public decimal MonthlyMaintenancePaid { get; set; }
    public bool MaintenancePaidPreDecember2018 { get; set; } // true = pre-2018, false = post-2018

    public bool ReceivesMaintenance { get; set; }
    public decimal MonthlyMaintenanceReceived { get; set; }
    public bool MaintenanceReceivedPreDecember2018 { get; set; }

    // Computed helper
    public decimal GetSelfEmploymentNetIncome() => MonthlyGrossSelfEmployment - MonthlyBusinessExpenses;

    public decimal GetGrossIncome()
    {
        var income = 0m;
        if (HasEmploymentIncome) income += MonthlyGrossEmployment;
        if (IsSelfEmployed) income += GetSelfEmploymentNetIncome();
        if (HasBonusIncome) income += MonthlyBonusAverage;
        if (HasMilitaryPay) income += MonthlyMilitaryPay;
        if (HasDisabilityPayments) income += MonthlyDisabilityPayments;
        if (HasUnemploymentCompensation) income += MonthlyUnemploymentCompensation;
        if (HasRetirementIncome) income += MonthlyRetirementIncome;
        if (HasOtherIncome) income += MonthlyOtherIncome;
        return income;
    }
}
