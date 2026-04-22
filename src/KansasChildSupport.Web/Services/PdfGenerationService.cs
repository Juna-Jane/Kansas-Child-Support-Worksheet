using KansasChildSupport.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KansasChildSupport.Web.Services;

public interface IPdfGenerationService
{
    byte[] GeneratePdf(WorksheetSession session);
}

public class PdfGenerationService : IPdfGenerationService
{
    public byte[] GeneratePdf(WorksheetSession session)
    {
        var result = session.CalculatedResult ?? new WorksheetResult();
        var caseInfo = session.CaseInfo ?? new Models.Steps.Step01_CaseInfoModel();
        var children = session.Children?.Children ?? new List<Models.Steps.ChildEntry>();

        string p1Name = string.IsNullOrEmpty(caseInfo.Party1Name) ? "Party 1" : caseInfo.Party1Name;
        string p2Name = string.IsNullOrEmpty(caseInfo.Party2Name) ? "Party 2" : caseInfo.Party2Name;
        bool p1IsPrimary = caseInfo.PrimaryCustody != "Party2";
        string primaryName = p1IsPrimary ? p1Name : p2Name;
        string nonPrimaryName = p1IsPrimary ? p2Name : p1Name;

        var doc = Document.Create(container =>
        {
            // ---- PAGE 1 ----
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(0.75f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(9));

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().AlignCenter().Text($"IN THE {GetJudicialDistrict(caseInfo.County)} JUDICIAL DISTRICT, {caseInfo.County?.ToUpper()} COUNTY, KANSAS")
                        .Bold().FontSize(10);
                    col.Item().AlignCenter().Text($"In the Matter of {p1Name} v. {p2Name}")
                        .FontSize(10);
                    col.Item().AlignCenter().Text($"Case No.: {(string.IsNullOrEmpty(caseInfo.CaseNumber) ? "_______________" : caseInfo.CaseNumber)}")
                        .FontSize(9);
                    col.Item().AlignCenter().Text("CHILD SUPPORT WORKSHEET").Bold().FontSize(12);
                    col.Item().AlignCenter().Text("Pursuant to K.S.A. Chapter 23").FontSize(9);
                    col.Item().AlignCenter().Text("Kansas Child Support Guidelines effective July 1, 2025").FontSize(8);
                    col.Item().Height(8);

                    // Column headers
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(3);
                            c.RelativeColumn(3);
                        });
                        t.Cell().BorderBottom(1).Background(Colors.Grey.Lighten3).Text("").Bold();
                        t.Cell().BorderBottom(1).Background(Colors.Grey.Lighten3).AlignCenter().Text(p1Name).Bold().FontSize(8);
                        t.Cell().BorderBottom(1).Background(Colors.Grey.Lighten3).AlignCenter().Text(p2Name).Bold().FontSize(8);
                    });

                    col.Item().Height(4);

                    // Section A
                    AddSectionHeader(col, "SECTION A — INCOME COMPUTATION (Wage Earner)");
                    AddTwoColRow(col, "A.1 Domestic Gross Income (employment, military, disability, other)", result.P1_A1, result.P2_A1);

                    col.Item().Height(4);

                    // Section B
                    AddSectionHeader(col, "SECTION B — INCOME COMPUTATION (Self-Employed)");
                    AddTwoColRow(col, "B.1 Self-Employment Gross Income", result.P1_B1, result.P2_B1);
                    AddTwoColRowNeg(col, "B.2 Reasonable Business Expenses (–)", result.P1_B2, result.P2_B2);
                    AddTwoColRow(col, "B.3 Domestic Gross Income from Self-Employment", result.P1_B3, result.P2_B3);

                    col.Item().Height(4);

                    // Section C
                    AddSectionHeader(col, "SECTION C — ADJUSTMENTS TO DOMESTIC GROSS INCOME");
                    AddTwoColRow(col, "C.1 Domestic Gross Income (A.1 + B.3)", result.P1_C1, result.P2_C1);
                    AddTwoColRowNeg(col, "C.2 Court-Ordered Child Support Paid (other cases) (–)", result.P1_C2, result.P2_C2);
                    AddTwoColRowNeg(col, "C.3 Court-Ordered Maintenance Paid (–)", result.P1_C3, result.P2_C3);
                    AddTwoColRow(col, "C.4 Court-Ordered Maintenance Received (+)", result.P1_C4, result.P2_C4);
                    AddTwoColRow(col, "C.5 Child Support Income → Line D.1", result.P1_C5, result.P2_C5, bold: true);

                    col.Item().Height(4);

                    // Section D
                    AddSectionHeader(col, "SECTION D — COMPUTATION OF CHILD SUPPORT OBLIGATION");
                    AddTwoColRow(col, "D.1 Child Support Income", result.P1_D1, result.P2_D1);
                    AddOneColRow(col, "D.2 Total Combined Child Support Income", result.D2);
                    AddTwoColPctRow(col, "D.3 Proportionate Shares", result.P1_D3, result.P2_D3);

                    // D.4 table
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(6);
                        });
                        t.Cell().Text("D.4 Gross Child Support Obligation (from schedule)").Bold();
                        t.Cell().AlignRight().Text(FormatMoney(result.D4)).Bold();
                    });

                    // Age group details
                    if (children.Count > 0)
                    {
                        var groups = children.GroupBy(c => c.GetAgeGroup()).ToDictionary(g => g.Key, g => g.Count());
                        col.Item().PaddingLeft(20).Text(
                            $"Children: {string.Join(", ", groups.Select(g => $"{g.Value} age {g.Key}"))}" +
                            $" | Total: {children.Count}" +
                            (result.ExtendedFormulaUsed ? " [Extended Formula]" : "")
                        ).FontSize(8).Italic();
                    }

                    AddTwoColRow(col, "D.5 Proportionate Share of Gross CSO (D.3 × D.4)", result.P1_D5, result.P2_D5);

                    col.Item().Height(6);

                    // Notes
                    if (result.Notes.Count > 0)
                    {
                        AddSectionHeader(col, "NOTES");
                        foreach (var note in result.Notes)
                        {
                            col.Item().Text($"• {note}").FontSize(7).Italic();
                        }
                    }
                });

                page.Footer().AlignCenter().Text("Kansas Child Support Guidelines effective July 1, 2025 — Page 1 of 3").FontSize(7);
            });

            // ---- PAGE 2 ----
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(0.75f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(9));

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text("CHILD SUPPORT WORKSHEET (continued)").Bold().FontSize(10);
                    col.Item().AlignCenter().Text($"{p1Name} v. {p2Name}  |  Case No.: {(string.IsNullOrEmpty(caseInfo.CaseNumber) ? "_______________" : caseInfo.CaseNumber)}").FontSize(8);
                    col.Item().Height(8);

                    // Column headers
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(3);
                            c.RelativeColumn(3);
                        });
                        t.Cell().BorderBottom(1).Background(Colors.Grey.Lighten3).Text("").Bold();
                        t.Cell().BorderBottom(1).Background(Colors.Grey.Lighten3).AlignCenter().Text(p1Name).Bold().FontSize(8);
                        t.Cell().BorderBottom(1).Background(Colors.Grey.Lighten3).AlignCenter().Text(p2Name).Bold().FontSize(8);
                    });

                    col.Item().Height(4);

                    // Section E
                    AddSectionHeader(col, "SECTION E — PARENTING TIME OR SHARED RESIDENCY ADJUSTMENT");
                    col.Item().PaddingLeft(4).Text($"(Applied to non-primary parent: {nonPrimaryName})").FontSize(8).Italic();
                    AddOneColRow(col, "E.1.a Formula Adjustment (% of time × D.5)", result.E1a, showIfZero: result.E1a > 0);
                    AddOneColRow(col, "E.1.b Actual Cost Adjustment", result.E1b, showIfZero: result.E1b > 0);
                    AddOneColRow(col, "E.1.c Extended Time Adjustment (up to 50%)", result.E1c, showIfZero: result.E1c > 0);
                    AddOneColRow(col, "E.2 Shared Residency Formula [(higher D.5 – lower D.5) ÷ 2]", result.E2, showIfZero: result.E2 > 0);
                    AddOneColRow(col, "E.4 Total Parenting Time Adjustment Used", result.E4, bold: true);
                    if (!string.IsNullOrEmpty(result.SharedResidencyNote))
                        col.Item().PaddingLeft(4).Text(result.SharedResidencyNote).FontSize(7).Italic();

                    col.Item().Height(4);

                    // Section F
                    AddSectionHeader(col, "SECTION F — HEALTH INSURANCE");
                    AddTwoColRow(col, "F.1 Monthly Insurance Premium for Children Only", result.P1_F1, result.P2_F1);
                    AddOneColRow(col, "F.1 Total Insurance Premium", result.F1Total);
                    AddTwoColRow(col, "F.2 Proportionate Share of Insurance (D.3 × F.1 total)", result.P1_F2, result.P2_F2);

                    col.Item().Height(4);

                    // Section G
                    AddSectionHeader(col, "SECTION G — WORK-RELATED CHILD CARE COSTS");
                    AddTwoColRow(col, "G.1 Gross Monthly Child Care Cost", result.P1_G1_Gross, result.P2_G1_Gross);
                    AddTwoColRowNeg(col, "     Less: Child Care Tax Credit (–)", result.P1_ChildCareCredit, result.P2_ChildCareCredit);
                    AddTwoColRow(col, "G.1 Net Work-Related Child Care Cost", result.P1_G1, result.P2_G1, bold: true);
                    AddOneColRow(col, "G.1 Total Net Child Care", result.G1Total);
                    AddTwoColRow(col, "G.2 Proportionate Share of Child Care (D.3 × G.1 total)", result.P1_G2, result.P2_G2);

                    col.Item().Height(4);

                    // Section H
                    AddSectionHeader(col, "SECTION H — PROPORTIONATE CHILD SUPPORT OBLIGATION");
                    AddTwoColRow(col, "H.1 Proportionate CSO (D.5 ± E.4 + F.2 + G.2)", result.P1_H1, result.P2_H1, bold: true);

                    col.Item().Height(4);

                    // Section I
                    AddSectionHeader(col, "SECTION I — BASIC CHILD SUPPORT OBLIGATION");
                    AddTwoColRow(col, "I.1 Credits (F.1 + G.1 actually paid)", result.P1_I1, result.P2_I1);
                    AddTwoColRow(col, "I.2 Basic Child Support Obligation (H.1 – I.1)", result.P1_I2, result.P2_I2, bold: true);
                    col.Item().PaddingLeft(4).Text($"→ Non-primary parent ({nonPrimaryName}): {FormatMoney(p1IsPrimary ? result.P2_I2 : result.P1_I2)} per month (Rebuttable Presumptive Amount before adjustments)").FontSize(8).Italic();
                });

                page.Footer().AlignCenter().Text("Kansas Child Support Guidelines effective July 1, 2025 — Page 2 of 3").FontSize(7);
            });

            // ---- PAGE 3 ----
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(0.75f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(9));

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text("CHILD SUPPORT WORKSHEET (continued)").Bold().FontSize(10);
                    col.Item().AlignCenter().Text($"{p1Name} v. {p2Name}  |  Case No.: {(string.IsNullOrEmpty(caseInfo.CaseNumber) ? "_______________" : caseInfo.CaseNumber)}").FontSize(8);
                    col.Item().Height(8);

                    // Section J
                    AddSectionHeader(col, "SECTION J — CHILD SUPPORT ADJUSTMENTS");
                    AddOneColRow(col, "J.1 Long-Distance Parenting Time Costs", result.J1);
                    AddOneColRow(col, "J.2 Income Tax Adjustment", result.J2);
                    AddOneColRow(col, "J.3 Special Needs", result.J3);
                    AddOneColRow(col, "J.4 Support Past Majority", result.J4);
                    AddOneColRow(col, "J.5 Overall Financial Condition (+/–)", result.J5);
                    AddOneColRow(col, "J.6 Total Adjustments", result.J6, bold: true);

                    col.Item().Height(4);

                    // Section K
                    AddSectionHeader(col, "SECTION K — DEVIATION FROM REBUTTABLE PRESUMPTION AMOUNT");
                    AddOneColRow(col, $"K.1 Basic Child Support Obligation (I.2 for {nonPrimaryName})", result.K1);
                    AddOneColRow(col, "K.2 Total Adjustments (J.6)", result.K2);
                    AddOneColRow(col, "K.3 Adjusted Obligation (K.1 + K.2)", result.K3, bold: true);

                    if (result.AbilityToPayFlag)
                    {
                        col.Item().Background(Colors.Yellow.Lighten2).Padding(4).Text(
                            $"⚠ ABILITY TO PAY: Calculated amount ({FormatMoney(result.K3)}) may exceed income available for support ({FormatMoney(result.IncomeAvailableForSupport)}). Court must consider Section VI.G."
                        ).FontSize(8).Bold();
                    }

                    AddOneColRow(col, "K.4 Social Security Dependent Benefit Credit (–)", result.K4);

                    col.Item().Height(4);

                    // Section L
                    AddSectionHeader(col, "SECTION L — NET PARENTAL CHILD SUPPORT OBLIGATION");
                    AddOneColRow(col, "L. Net Obligation (K.3 – K.4)", result.L, bold: true);

                    col.Item().Height(4);

                    // Section M
                    AddSectionHeader(col, "SECTION M — ENFORCEMENT FEE ALLOWANCE");
                    AddOneColRow(col, "M. Enforcement Fee (50% of fee)", result.M);

                    col.Item().Height(4);

                    // Section N
                    AddSectionHeader(col, "SECTION N — TOTAL CHILD SUPPORT ORDER");
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.RelativeColumn(5);
                        });
                        t.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text($"N. TOTAL MONTHLY CHILD SUPPORT ({nonPrimaryName} pays {primaryName})").Bold().FontSize(11);
                        t.Cell().Background(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(FormatMoney(result.N)).Bold().FontSize(13);
                    });

                    col.Item().Height(16);

                    // Signature lines
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.RelativeColumn(5);
                        });
                        t.Cell().BorderBottom(1).Text("Prepared by (Signature)");
                        t.Cell().BorderBottom(1).Text("Date Submitted");
                    });
                    col.Item().Height(8);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => c.RelativeColumn(10));
                        t.Cell().BorderBottom(1).Text("Prepared by (Print Name)");
                    });
                    col.Item().Height(8);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.RelativeColumn(5);
                        });
                        t.Cell().BorderBottom(1).Text("Judge/Hearing Officer Signature");
                        t.Cell().BorderBottom(1).Text("Date Approved");
                    });

                    col.Item().Height(16);

                    // Footer note
                    col.Item().Text("Calculated using Kansas Child Support Guidelines effective July 1, 2025 pursuant to Kansas Supreme Court Administrative Order").FontSize(7).Italic();
                    col.Item().Text("This worksheet is for informational purposes only and does not constitute legal advice. The rebuttable presumptive amount may be adjusted by a court.").FontSize(7).Italic();
                });

                page.Footer().AlignCenter().Text("Kansas Child Support Guidelines effective July 1, 2025 — Page 3 of 3").FontSize(7);
            });
        });

        return doc.GeneratePdf();
    }

    private static string FormatMoney(decimal value)
        => $"${value:N0}";

    private static string FormatPercent(decimal value)
        => $"{value * 100:F1}%";

    private static void AddSectionHeader(ColumnDescriptor col, string title)
    {
        col.Item().Background(Colors.Grey.Lighten3).Padding(3).Text(title).Bold().FontSize(8);
    }

    private static void AddTwoColRow(ColumnDescriptor col, string label, decimal p1Val, decimal p2Val, bool bold = false)
    {
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(4);
                c.RelativeColumn(3);
                c.RelativeColumn(3);
            });
            var labelText = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingLeft(4).Text(label);
            if (bold) labelText.Bold();
            var p1Text = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignRight().PaddingRight(4).Text(FormatMoney(p1Val));
            if (bold) p1Text.Bold();
            var p2Text = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignRight().PaddingRight(4).Text(FormatMoney(p2Val));
            if (bold) p2Text.Bold();
        });
    }

    private static void AddTwoColRowNeg(ColumnDescriptor col, string label, decimal p1Val, decimal p2Val)
    {
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(4);
                c.RelativeColumn(3);
                c.RelativeColumn(3);
            });
            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingLeft(4).Text(label);
            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignRight().PaddingRight(4).Text(p1Val == 0 ? "—" : $"({FormatMoney(p1Val)})");
            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignRight().PaddingRight(4).Text(p2Val == 0 ? "—" : $"({FormatMoney(p2Val)})");
        });
    }

    private static void AddTwoColPctRow(ColumnDescriptor col, string label, decimal p1Val, decimal p2Val)
    {
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(4);
                c.RelativeColumn(3);
                c.RelativeColumn(3);
            });
            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingLeft(4).Text(label);
            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignRight().PaddingRight(4).Text(FormatPercent(p1Val));
            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignRight().PaddingRight(4).Text(FormatPercent(p2Val));
        });
    }

    private static void AddOneColRow(ColumnDescriptor col, string label, decimal val, bool bold = false, bool showIfZero = true)
    {
        if (!showIfZero && val == 0) return;
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(7);
                c.RelativeColumn(3);
            });
            var labelText = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingLeft(4).Text(label);
            if (bold) labelText.Bold();
            var valText = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignRight().PaddingRight(4).Text(FormatMoney(val));
            if (bold) valText.Bold();
        });
    }

    private static string GetJudicialDistrict(string? county)
    {
        if (string.IsNullOrEmpty(county)) return "___";
        // Simplified mapping — Kansas has 31 judicial districts
        return county.ToUpper() switch
        {
            "JOHNSON" => "10TH",
            "WYANDOTTE" => "29TH",
            "SEDGWICK" => "18TH",
            "DOUGLAS" => "7TH",
            "SHAWNEE" => "3RD",
            "LEAVENWORTH" => "10TH",
            "RILEY" => "21ST",
            "SALINE" => "28TH",
            "RENO" => "27TH",
            "BUTLER" => "13TH",
            _ => "___"
        };
    }
}
