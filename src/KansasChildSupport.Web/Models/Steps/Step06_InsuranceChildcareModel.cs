namespace KansasChildSupport.Web.Models.Steps;

public class Step06_InsuranceChildcareModel
{
    // Section F - Health Insurance
    public bool Parent1PaysInsurance { get; set; }
    public decimal Parent1MonthlyInsurancePremium { get; set; }

    public bool Parent2PaysInsurance { get; set; }
    public decimal Parent2MonthlyInsurancePremium { get; set; }

    // Section G - Child Care
    public bool Parent1PaysChildCare { get; set; }
    public decimal Parent1MonthlyChildCare { get; set; }
    public decimal Parent1AnnualGrossIncomeForChildCare { get; set; }

    public bool Parent2PaysChildCare { get; set; }
    public decimal Parent2MonthlyChildCare { get; set; }
    public decimal Parent2AnnualGrossIncomeForChildCare { get; set; }
}
