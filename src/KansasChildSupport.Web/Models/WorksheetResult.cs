namespace KansasChildSupport.Web.Models;

public class WorksheetResult
{
    // Section A
    public decimal P1_A1 { get; set; } // P1 Domestic Gross Income (wage earner)
    public decimal P2_A1 { get; set; }

    // Section B
    public decimal P1_B1 { get; set; } // Self-employment gross
    public decimal P2_B1 { get; set; }
    public decimal P1_B2 { get; set; } // Business expenses
    public decimal P2_B2 { get; set; }
    public decimal P1_B3 { get; set; } // Net self-employment
    public decimal P2_B3 { get; set; }

    // Section C
    public decimal P1_C1 { get; set; } // Total gross income = A1 + B3
    public decimal P2_C1 { get; set; }
    public decimal P1_C2 { get; set; } // Child support paid other cases
    public decimal P2_C2 { get; set; }
    public decimal P1_C3 { get; set; } // Maintenance paid (adjusted)
    public decimal P2_C3 { get; set; }
    public decimal P1_C4 { get; set; } // Maintenance received (adjusted)
    public decimal P2_C4 { get; set; }
    public decimal P1_C5 { get; set; } // Child Support Income = C1 - C2 - C3 + C4
    public decimal P2_C5 { get; set; }

    // Section D
    public decimal P1_D1 { get; set; } // = C5
    public decimal P2_D1 { get; set; }
    public decimal D2 { get; set; } // Combined = P1_D1 + P2_D1
    public decimal P1_D3 { get; set; } // P1 proportionate share
    public decimal P2_D3 { get; set; }
    public decimal D4 { get; set; } // Gross child support obligation from schedule
    public decimal P1_D5 { get; set; } // P1 proportionate share of D4
    public decimal P2_D5 { get; set; }

    // Section E - Parenting Time Adjustment (non-primary parent only)
    public decimal E1a { get; set; } // Percentage method
    public decimal E1b { get; set; } // Actual cost method
    public decimal E1c { get; set; } // Extended time method
    public decimal E2 { get; set; } // Shared residency formula
    public decimal E3 { get; set; } // Direct expense formula
    public decimal E4 { get; set; } // Total parenting time adjustment used

    // Section F - Health Insurance
    public decimal P1_F1 { get; set; }
    public decimal P2_F1 { get; set; }
    public decimal F1Total { get; set; }
    public decimal P1_F2 { get; set; }
    public decimal P2_F2 { get; set; }

    // Section G - Child Care
    public decimal P1_G1_Gross { get; set; } // Before credit
    public decimal P2_G1_Gross { get; set; }
    public decimal P1_ChildCareCredit { get; set; }
    public decimal P2_ChildCareCredit { get; set; }
    public decimal P1_G1 { get; set; } // Net after credit
    public decimal P2_G1 { get; set; }
    public decimal G1Total { get; set; }
    public decimal P1_G2 { get; set; }
    public decimal P2_G2 { get; set; }

    // Section H
    public decimal P1_H1 { get; set; }
    public decimal P2_H1 { get; set; }

    // Section I
    public decimal P1_I1 { get; set; } // F1 + G1 credits
    public decimal P2_I1 { get; set; }
    public decimal P1_I2 { get; set; } // H1 - I1
    public decimal P2_I2 { get; set; }

    // Section J - Adjustments
    public decimal J1 { get; set; } // Long-distance costs
    public decimal J2 { get; set; } // Tax adjustment
    public decimal J3 { get; set; } // Special needs
    public decimal J4 { get; set; } // Support past majority
    public decimal J5 { get; set; } // Overall financial condition
    public decimal J6 { get; set; } // Total J1-J5

    // Section K
    public decimal K1 { get; set; } // = I2 for non-primary
    public decimal K2 { get; set; } // = J6
    public decimal K3 { get; set; } // = K1 +/- K2
    public decimal K4 { get; set; } // SS benefit credit

    // Ability to Pay check
    public bool AbilityToPayFlag { get; set; }
    public decimal IncomeAvailableForSupport { get; set; }

    // Section L
    public decimal L { get; set; } // K3 - K4, min 0

    // Section M
    public decimal M { get; set; } // Enforcement fee

    // Section N - Final answer
    public decimal N { get; set; } // L + M

    // Flags/Notes
    public bool ExtendedFormulaUsed { get; set; }
    public bool IncomeUnknownFlag { get; set; }
    public string? SharedResidencyNote { get; set; }
    public List<string> Notes { get; set; } = new();
}
