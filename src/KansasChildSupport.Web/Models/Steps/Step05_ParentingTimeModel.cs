namespace KansasChildSupport.Web.Models.Steps;

public class Step05_ParentingTimeModel
{
    // Does non-primary parent have 35%+ of time?
    public string NonPrimaryTimePercent { get; set; } = "No"; // "Yes", "No", "NotSure"

    // Which adjustment type?
    public string AdjustmentType { get; set; } = ""; // "Percentage", "ActualCost", "ExtendedTime"

    // Percentage of time method (35-49%)
    public decimal PercentageOfTime { get; set; }

    // Actual cost method
    public decimal ActualExtraCosts { get; set; }

    // Extended time method
    public decimal ExtendedTimeReductionPercent { get; set; }

    // 50/50 equal time
    public bool IsEqualParentingTime { get; set; }

    // Long distance costs
    public bool HasLongDistanceCosts { get; set; }
    public decimal MonthlyLongDistanceCosts { get; set; }

    public decimal GetPercentageAdjustmentRate()
    {
        return PercentageOfTime switch
        {
            >= 35 and < 40 => 0.10m,
            >= 40 and < 45 => 0.20m,
            >= 45 and < 50 => 0.30m,
            _ => 0m
        };
    }
}
