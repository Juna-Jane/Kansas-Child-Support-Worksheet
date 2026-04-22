namespace KansasChildSupport.Web.Services;

public interface IChildTaxCreditService
{
    decimal GetMonthlyCredit(decimal annualGrossIncome, int numberOfChildren);
}

public class ChildTaxCreditService : IChildTaxCreditService
{
    // Child Care Credit Chart per AGENTS.md / Kansas Guidelines Section IV.J
    // Annual AGI → (percentage, max_1child, max_2plus_children) per month
    private static readonly (decimal maxIncome, decimal max1Child, decimal max2PlusChildren)[] CreditTable =
    {
        (14999,  87.50m, 175.00m),
        (16999,  85.00m, 170.00m),
        (18999,  82.50m, 165.00m),
        (20999,  80.00m, 160.00m),
        (22999,  77.50m, 155.00m),
        (24999,  75.00m, 150.00m),
        (26999,  72.50m, 145.00m),
        (28999,  70.00m, 140.00m),
        (30999,  67.50m, 135.00m),
        (32999,  65.00m, 130.00m),
        (34999,  62.50m, 125.00m),
        (36999,  60.00m, 120.00m),
        (38999,  57.50m, 115.00m),
        (40999,  55.00m, 110.00m),
        (42999,  52.50m, 105.00m),
        (decimal.MaxValue, 50.00m, 100.00m),
    };

    public decimal GetMonthlyCredit(decimal annualGrossIncome, int numberOfChildren)
    {
        foreach (var (maxIncome, max1Child, max2PlusChildren) in CreditTable)
        {
            if (annualGrossIncome <= maxIncome)
            {
                return numberOfChildren >= 2 ? max2PlusChildren : max1Child;
            }
        }
        return numberOfChildren >= 2 ? 100.00m : 50.00m;
    }
}
