using KansasChildSupport.Web.Models.Steps;

namespace KansasChildSupport.Web.Models;

public class WorksheetSession
{
    public Step01_CaseInfoModel? CaseInfo { get; set; }
    public Step02_ChildrenModel? Children { get; set; }
    public Step03_Parent1IncomeModel? Parent1Income { get; set; }
    public Step04_Parent2IncomeModel? Parent2Income { get; set; }
    public Step05_ParentingTimeModel? ParentingTime { get; set; }
    public Step06_InsuranceChildcareModel? InsuranceChildcare { get; set; }
    public Step07_AdjustmentsModel? Adjustments { get; set; }
    public WorksheetResult? CalculatedResult { get; set; }
}
