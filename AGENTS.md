# AGENTS.md — Kansas Child Support Worksheet Web Application

## Project Purpose

Build a guided, plain-language web application that walks low-income, low-literacy Kansas residents (non-attorneys) through completing the official Kansas Child Support Worksheet (Appendix I of the Kansas Child Support Guidelines, effective July 1, 2025). The app collects all required information across multiple friendly, plain-language steps, performs all calculations, and produces a completed, court-ready PDF of the official worksheet — ready for the user to sign and submit.

**Target user:** A parent with no legal training who needs to file or respond to a child support action in Kansas. Plain language, clear guidance, zero legal jargon unless explained.

**End output:** A completed PDF matching the official Kansas Child Support Worksheet (Appendix I), filled with calculated values, suitable for court submission.

---

## Technology Stack

- **Language:** C# only
- **Framework:** ASP.NET Core 8 (MVC pattern)
- **PDF generation:** QuestPDF (NuGet: `QuestPDF`) — use this to render the final worksheet PDF
- **Styling:** Plain CSS (no external CSS frameworks) — mobile-first, large text, high contrast
- **JavaScript:** Vanilla JS only, minimal, progressive enhancement
- **No JavaScript frameworks** (no React, no Vue, no Angular)
- **Database:** None — all state held in server-side session (use `ISession` / `AddSession()`)
- **Hosting target:** GitHub + any standard .NET host (e.g. Azure App Service, Railway, Render)
- **Test framework:** xUnit

---

## Repository Structure

```
/
├── AGENTS.md                          ← this file
├── KansasChildSupport.sln
├── src/
│   └── KansasChildSupport.Web/
│       ├── KansasChildSupport.Web.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Controllers/
│       │   ├── WorksheetController.cs     ← main multi-step wizard
│       │   └── PdfController.cs           ← PDF generation endpoint
│       ├── Models/
│       │   ├── WorksheetSession.cs        ← all session state
│       │   ├── Steps/
│       │   │   ├── Step01_CaseInfoModel.cs
│       │   │   ├── Step02_ChildrenModel.cs
│       │   │   ├── Step03_Parent1IncomeModel.cs
│       │   │   ├── Step04_Parent2IncomeModel.cs
│       │   │   ├── Step05_ParentingTimeModel.cs
│       │   │   ├── Step06_InsuranceChildcareModel.cs
│       │   │   ├── Step07_AdjustmentsModel.cs
│       │   │   └── Step08_ReviewModel.cs
│       ├── Services/
│       │   ├── ScheduleLookupService.cs   ← Appendix II tables + extended formula
│       │   ├── CalculationService.cs      ← all worksheet math, Sections A–N
│       │   ├── ChildTaxCreditService.cs   ← child care credit table
│       │   ├── SessionService.cs          ← read/write WorksheetSession to ISession
│       │   └── PdfGenerationService.cs    ← QuestPDF worksheet renderer
│       ├── Views/
│       │   ├── Shared/
│       │   │   ├── _Layout.cshtml
│       │   │   └── _StepProgress.cshtml   ← progress bar partial
│       │   └── Worksheet/
│       │       ├── Step01_CaseInfo.cshtml
│       │       ├── Step02_Children.cshtml
│       │       ├── Step03_Parent1Income.cshtml
│       │       ├── Step04_Parent2Income.cshtml
│       │       ├── Step05_ParentingTime.cshtml
│       │       ├── Step06_InsuranceChildcare.cshtml
│       │       ├── Step07_Adjustments.cshtml
│       │       ├── Step08_Review.cshtml
│       │       └── Complete.cshtml
│       └── wwwroot/
│           ├── css/
│           │   └── site.css
│           └── js/
│               └── wizard.js
└── tests/
    └── KansasChildSupport.Tests/
        ├── KansasChildSupport.Tests.csproj
        ├── CalculationServiceTests.cs
        └── ScheduleLookupTests.cs
```

---

## Multi-Step Wizard Flow

The wizard is a linear 8-step form. Each step is its own GET/POST pair. Session stores accumulated data. Users can go Back freely. No step is skippable.

### Progress Indicator
Every page shows a horizontal progress bar with step names. Current step is highlighted. Completed steps are marked with a checkmark. The bar is visible at the top of every step page.

### Step Definitions

---

### STEP 1: Case Information
**URL:** `/worksheet/step1`  
**Plain-language heading:** "Let's start with the basics"

**Fields collected:**
- Case Number (text, optional — explain: "This is on any court papers you've already received. Leave blank if you don't have one yet.")
- Your Name (Party 1 — the person filling out this form)
- Other Parent's Name (Party 2)
- County (Kansas county dropdown — all 105 Kansas counties)
- Who has primary custody? (Radio: "I do (Party 1)" / "The other parent does (Party 2)")

**Guidance text on page:** "Child support is the money one parent pays the other to help cover the costs of raising your child. This form will walk you through calculating that amount using Kansas law. It takes about 10–15 minutes. Have your recent pay stubs or tax returns ready."

---

### STEP 2: Children
**URL:** `/worksheet/step2`  
**Plain-language heading:** "Tell us about your children"

**Fields collected (repeatable, up to 6 children):**
- Child's first name
- Child's date of birth (date picker)
- Dynamic display: app shows calculated age and age group (0–5, 6–11, 12–18) after entry

**Guidance text:** "Only include the children this support order is for. Do not include stepchildren or children from other relationships."

**Add/Remove child buttons** — JS-powered, no page reload needed. Minimum 1 child.

**On this page explain:** "Age matters because Kansas law uses three age groups to calculate support: ages 0–5, 6–11, and 12–18. Older children typically cost more to raise."

---

### STEP 3: Your Income (Party 1)
**URL:** `/worksheet/step3`  
**Plain-language heading:** "Your income"

**Sections with plain-language labels and help text:**

**Section A — Job / Employment Income**
- "Do you have a job or receive wages?" (Yes/No toggle — shows/hides fields)
- Monthly gross income from employment (dollar amount)
  - Help: "Gross income means the amount BEFORE taxes are taken out. Look at your pay stub — this is the 'gross pay' line. Include wages, salary, tips, commissions, overtime, and bonuses."
  - Help: "If you're paid weekly, multiply your weekly gross pay by 4.33. If paid every two weeks, multiply by 2.17."
- Does your employer offer a cafeteria plan (pre-tax benefits like health insurance, FSA, dependent care)? (Yes/No)
  - If Yes: "Even though these come out before taxes, Kansas law says we still count the full gross amount — so no adjustment needed here. Just enter your total gross wages above."

**Section B — Self-Employment Income**
- "Are you self-employed, own a business, or do freelance/gig work?" (Yes/No toggle)
- Monthly gross income from self-employment
  - Help: "This is your total business revenue before expenses."
- Monthly reasonable business expenses
  - Help: "These are expenses that are truly necessary to run your business — like supplies, equipment, or business insurance. Do NOT include personal expenses, your home mortgage, or the QBI deduction."

**Section C — Other Income Sources (checkboxes to reveal fields)**
- Bonuses / commissions (irregular): monthly average
  - Help: "If you get bonuses sometimes but not every month, add up the last 12 months of bonuses and divide by 12."
- Military pay (Basic Pay + BAH + BAS + other allowances): monthly total
  - Help: "Include ALL military income: Basic Pay, Basic Allowance for Housing (BAH), Basic Allowance for Subsistence (BAS), and any special pays. These all count under Kansas law."
- Disability payments (SSDI, VA Disability, Workers Comp): monthly total
- Unemployment compensation: monthly total
- Retirement/pension distributions: monthly total
- Other income (describe): monthly total

**Section C — Adjustments (deductions)**
- Do you pay child support for OTHER children (not in this case) under a court order? (Yes/No)
  - If Yes: Monthly amount you actually pay
  - Help: "Only enter this if there is an actual court order AND you are actually paying it."
- Do you pay spousal maintenance (alimony) to someone under a court order? (Yes/No)
  - If Yes: Monthly amount; Date of order (before/after Dec 31, 2018)
  - Help: "If your order is dated after December 31, 2018, the tax law changed how this is calculated. We'll handle that automatically."
- Do you RECEIVE spousal maintenance from someone under a court order? (Yes/No)
  - If Yes: Monthly amount; Date of order (before/after Dec 31, 2018)

**Running total display:** Show a live "Your Child Support Income (estimated): $X,XXX/month" that updates as fields are filled. Label it clearly as a working estimate.

---

### STEP 4: Other Parent's Income (Party 2)
**URL:** `/worksheet/step4`  
**Plain-language heading:** "The other parent's income"

**Same structure as Step 3** but for Party 2.

**Guidance at top:** "You'll need to enter the other parent's income as best you know it. If you're not sure, enter your best estimate — the court can require them to provide documentation. Income from a new spouse or new partner does NOT count."

**If income is unknown:** Provide a checkbox "I don't know the other parent's income." If checked:
- Show message: "If you don't know, the court may impute (estimate) income based on what a full-time worker earns at minimum wage in Kansas ($7.25/hr × 40 hrs × 4.33 weeks = approximately $1,254/month). You can enter that as a starting estimate, or leave it at $0 and note in court that you need income discovery."
- Auto-fill with Kansas minimum wage calculation as a suggestion (user can override).

---

### STEP 5: Parenting Time
**URL:** `/worksheet/step5`  
**Plain-language heading:** "Time with the children"

**Guidance:** "Kansas law may reduce the non-primary parent's child support if they spend a significant amount of time with the children. Answer these questions about how much time the children spend with each parent."

**Fields:**
- "Does the non-primary parent (the parent NOT listed as having primary custody) have the children for 35% or more of the time?" (Yes / No / Not sure)
  - Help: "35% of the year = about 128 days, or roughly every other weekend plus one evening per week plus half of all school breaks."
  
- If Yes, which type of adjustment applies? (Radio):
  1. **Percentage of time** — "I know roughly what percent of time the children are with the non-primary parent"
     - Slider or input: % of time (35–49%)
     - Show lookup table: 35–39% = 10% reduction, 40–44% = 20%, 45–49% = 30%
     - Note: "Time at school or daycare does NOT count toward this percentage."
  2. **Actual extra costs** — "I want to enter the actual dollar amount of extra costs the non-primary parent incurs"
     - Dollar amount field
  3. **Extended time (14+ consecutive days)** — "The non-primary parent has the children for 14 or more days in a row (e.g., summer)"
     - Reduction percentage (up to 50%)

- **Equal / Shared Parenting Time** (50/50 split):
  - "Do the children spend exactly equal time with both parents?" (Yes/No)
  - If Yes: Show information about the Shared Expense Formula and Direct Expense Formula options. For this tool, use the Direct Expense Formula (simpler, no cooperative expense-sharing required). Note this in the output.

- Long-distance parenting costs:
  - "Does one parent pay significant travel costs for parenting time because you live far apart?" (Yes/No)
  - If Yes: Estimated monthly cost (instructions to divide total annual cost by 12 and then by 2)

---

### STEP 6: Insurance & Child Care
**URL:** `/worksheet/step6`  
**Plain-language heading:** "Health insurance and child care"

**Section F — Health Insurance**

For each parent (shown side by side on desktop, stacked on mobile):
- "Does [Parent Name] pay for the children's health, dental, or vision insurance?" (Yes/No)
- If Yes: Monthly premium cost FOR THE CHILDREN ONLY
  - Help: "This is NOT your total insurance premium. It's only the portion that covers the children. To figure this out: subtract what your insurance would cost for yourself only (employee-only plan) from what it costs with the children added (employee + children plan). That difference is the children's share."
  - Help: "If you get insurance through work, check your benefits paperwork or call HR."

**Section G — Work-Related Child Care**

For each parent:
- "Does [Parent Name] pay for child care (daycare, after-school care, summer camp) so that they can work or look for work?" (Yes/No)
- If Yes: Monthly child care cost (average annual cost ÷ 12, including school-break variations)
  - Help: "Include daycare, after-school programs, and summer care. Do NOT include child care you pay for personal reasons when you're not working."
- Annual gross income of the parent paying child care (needed for child care tax credit reduction)
  - Help: "Kansas law requires us to subtract the tax credit you could get for child care costs. We need your annual income to look up that credit."
  - App automatically looks up the credit from the Child Care Credit Chart and subtracts it, showing the user the net amount used.

**Show calculation:** Display a small explanation: "Kansas law requires us to subtract the child care tax credit you may be eligible for. Based on your income, your estimated monthly credit is $X, so we'll use $Y as your child care cost in the calculation."

---

### STEP 7: Optional Adjustments
**URL:** `/worksheet/step7`  
**Plain-language heading:** "Special circumstances (most people skip this)"

**Guidance at top:** "These adjustments are optional and only apply to unusual situations. If none of these apply to you, just click Next."

Each item has a Yes/No toggle. Only show fields if Yes.

- **Special needs:** "Does your child have ongoing medical, educational, or therapy needs that cost more than normal?" Monthly amount.
  - Help: "Examples: regular physical therapy, special education tutoring, ongoing prescription costs not covered by insurance."
  
- **Income tax — dependency exemption:** "Is there a disagreement about who claims the child(ren) on taxes, or has the court addressed who gets the tax exemption?"
  - If Yes: Short explanation of Section J.2 and a dollar amount field (monthly adjustment).
  - Help: Briefly explain that claiming a child as a dependent is worth money and can affect child support. Recommend they ask an attorney or tax professional for this calculation. Provide link to the Income Tax Considerations supplement document's explanation.

- **Support past age 18:** "Do you have a written agreement that child support will continue after the child turns 18 (e.g., for college)?" Monthly amount.

- **Overall financial condition:** "Are there unusual financial circumstances not covered above that you believe should affect child support?"
  - Text: "This is a catch-all category. Describe the situation in writing and enter any monthly dollar adjustment. The court will decide whether to approve it."
  - Dollar amount field (+/-)

- **Social Security dependent benefits:** "Does the child receive Social Security benefits through the paying parent's account (for example, because that parent is disabled)?" Monthly benefit amount.
  - Help: "This is a credit that reduces the paying parent's obligation. It applies when a child receives SSDI dependent benefits tied to the paying parent."

- **Enforcement fee:** "Does your county charge a fee to collect and distribute child support payments through the Kansas Payment Center?" Monthly flat fee OR percentage.
  - Help: "Not all counties charge this. Check with your local court trustee's office or look at any existing court order. If unsure, leave at $0."

- **Bonus income:** "Does either parent receive irregular bonus income?" 
  - If Yes: Explain the two options (averaging vs. percentage method) in plain language. Collect which method they prefer and relevant amounts. Note that this will be displayed on the output for the court to use.

---

### STEP 8: Review & Confirm
**URL:** `/worksheet/step8`  
**Plain-language heading:** "Review your information"

**Display a clean summary of all entered data** organized by section. Each section has an "Edit" link that takes the user back to that step.

**Show the calculated results** in plain language:
- "Based on what you entered, the estimated monthly child support is: **$X,XXX**"
- "The [non-primary parent name] would pay this to [primary parent name] each month."
- Line-by-line calculation summary table (collapsible, labeled with plain names AND official line numbers in small text for reference)

**Important notices displayed:**
- "This amount is what Kansas law calls the 'rebuttable presumptive amount.' A judge can change it if they find a good reason."
- "This worksheet is a calculation tool. It does not give legal advice. If your situation is complicated, talk to a lawyer or legal aid."

**Links to legal aid:**
- Kansas Legal Services: kansaslegalservices.org / 1-800-723-6953
- Kansas Bar Association Lawyer Referral: (785) 234-5696

**Action buttons:**
- "Generate My Worksheet PDF" → POST to `/pdf/generate` → returns PDF download
- "Go Back and Edit"
- "Start Over"

---

### COMPLETE Page
**URL:** `/worksheet/complete`  
**Heading:** "Your worksheet is ready!"

- Confirmation message
- Download PDF button (re-downloadable)
- "What do I do next?" section in plain language:
  1. Print or save your PDF
  2. Sign and date where indicated on Page 2
  3. You will also need to complete a Domestic Relations Affidavit (link to KS Courts website)
  4. File both documents with the clerk of the district court in your county
  5. Serve a copy on the other parent
  6. Keep a copy for yourself
- Links to Kansas Courts self-help resources: https://kscourts.org/Kansas-Courts/Self-Help
- Legal aid links (same as above)

---

## Session Management

Use `ISession` to persist the wizard state across steps.

```csharp
// WorksheetSession.cs — stored as JSON in session
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
```

Session key: `"worksheet_session"`. Serialize to/from JSON using `System.Text.Json`.

Session timeout: 2 hours (configurable in `appsettings.json`).

If session is lost mid-wizard (e.g., timeout), redirect to Step 1 with a friendly message: "Your session expired. Let's start over — it should only take about 15 minutes."

---

## Calculation Engine (CalculationService.cs)

Implement ALL calculations per the Kansas Child Support Guidelines (effective July 1, 2025). This is the most critical component. Every line must match the official worksheet.

### Maintenance Paid Adjustment (Post-2018 Orders)
Per Section III.C.2 of the guidelines, for maintenance orders after December 31, 2018:
- Option used when parties agree: multiply maintenance paid by 1.25 (adds 25% to represent tax effect) — deduct from payor, add to payee
- Otherwise use marginal tax rates (collect from user or use 25% option)

### Child Care Tax Credit Reduction (Section IV.J)
Before entering child care on Line G.1, reduce by the applicable monthly child care tax credit per the Child Care Credit Chart:

```
Annual Adjusted Gross Income → Applicable % → Max Monthly Credit (1 child / 2+ children)
$0–$14,999        → 35% → $87.50 / $175.00
$15,000–$16,999   → 34% → $85.00 / $170.00
$17,000–$18,999   → 33% → $82.50 / $165.00
$19,000–$20,999   → 32% → $80.00 / $160.00
$21,000–$22,999   → 31% → $77.50 / $155.00
$23,000–$24,999   → 30% → $75.00 / $150.00
$25,000–$26,999   → 29% → $72.50 / $145.00
$27,000–$28,999   → 28% → $70.00 / $140.00
$29,000–$30,999   → 27% → $67.50 / $135.00
$31,000–$32,999   → 26% → $65.00 / $130.00
$33,000–$34,999   → 25% → $62.50 / $125.00
$35,000–$36,999   → 24% → $60.00 / $120.00
$37,000–$38,999   → 23% → $57.50 / $115.00
$39,000–$40,999   → 22% → $55.00 / $110.00
$41,000–$42,999   → 21% → $52.50 / $105.00
$43,000+           → 20% → $50.00 / $100.00
```
Net child care = Actual monthly child care - applicable monthly credit
If net < 0, use $0.

### Cost of Living Differential (Section IV.E)
Formula: `CLD = ((KS_RPP - Other_RPP) / Other_RPP) × Monthly_Income`
Apply as adjustment to Line A.1 or B.3.
Not applicable if both parents live in Kansas or same MSA.
Kansas RPP is approximately 91.2 (use this as the default; note it changes annually).

### Child Support Schedules (ScheduleLookupService.cs)
Encode ALL six child support schedule tables (1–6 children) from Appendix II of the Guidelines.

**For 4, 5, 6 children:** Encode the full tables from the PDF (not just the extended formula). The PDF contains complete tables for all six child counts.

**Extended formula** (for combined income > table maximum, or as fallback):
- Age 12–18: `income^0.6386 × multiplier`
- Multipliers: 1 child=4.8982, 2=3.5596, 3=3.0359, 4=2.6015, 5=2.3175, 6=2.1083
- Age 6–11: multiply 12–18 result × 0.94
- Age 0–5: multiply 12–18 result × 0.84

**Age group:** Use nearest birthday rule (round to nearest birthday, not just current age).

**Linear interpolation** between table rows for income values not exactly in the table.

**Rounding:** Round all dollar amounts to nearest dollar per guidelines Section VI.J.2. Round percentages to nearest tenth.

### Worksheet Lines (complete mapping)

```
Section A (Wage Earner):
  A.1 = sum of all wage/salary/other employment income (gross, pre-tax, pre-cafeteria-plan)
        + military basic pay + BAH + BAS + special pays
        + SSDI received by parent + disability insurance + workers comp
        + bonus income (averaged or as entered)
        Note: Do NOT include: SSI, TANF, food stamps, section 8, EIC, 
              child support received for other children, new spouse income

Section B (Self-Employed):
  B.1 = self-employment gross income
  B.2 = reasonable business expenses (may not include QBI deduction)
  B.3 = B.1 - B.2
  
  C.1 = A.1 + B.3 (domestic gross income)

Section C (Adjustments):
  C.2 = court-ordered child support actually paid in other cases (arrears NOT deducted)
  C.3 = court-ordered maintenance paid (adjusted for pre/post 2018 tax law)
  C.4 = court-ordered maintenance received (adjusted for pre/post 2018 tax law)
  C.5 = C.1 - C.2 - C.3 + C.4  [= Child Support Income, aka Line D.1]

Section D (Computations):
  D.1 = C.5 for each parent
  D.2 = P1.D1 + P2.D1  [combined child support income]
  D.3(P1) = P1.D1 / D.2  [P1 proportionate share, rounded to 3 decimal places]
  D.3(P2) = P2.D1 / D.2  [P2 proportionate share]
  
  D.4 = SUM over all children of: ScheduleLookup(numChildren, D.2, child.AgeGroup)
        [Gross child support obligation — from schedule]
  
  D.5(P1) = D.3(P1) × D.4  [P1 proportionate share of gross CSO]
  D.5(P2) = D.3(P2) × D.4  [P2 proportionate share of gross CSO]

Section E (Parenting Time Adjustment — applied to non-primary parent only):
  E.1.a = formula adjustment: adjPct × non-primary D.5
          where adjPct = 10% if 35–39%, 20% if 40–44%, 30% if 45–49%
  E.1.b = actual cost adjustment (entered by user)
  E.1.c = extended time adjustment (up to 50% of non-primary D.5)
  E.2   = shared residency formula: (higherD5 - lowerD5) / 2
  E.3   = direct expense formula amount (for 50/50 custody)
  E.4   = total parenting time adjustment (use E.1.a, E.1.b, OR E.1.c — not combined)
          [only one of E.1.a/b/c applies; E.2 or E.2+E.3 is an alternative]

Section F (Health Insurance):
  F.1(P1) = monthly insurance premium paid by P1 FOR CHILDREN ONLY
  F.1(P2) = monthly insurance premium paid by P2 FOR CHILDREN ONLY
  F.1(total) = F.1(P1) + F.1(P2)
  F.2(P1) = D.3(P1) × F.1(total)  [P1 proportionate share of total insurance cost]
  F.2(P2) = D.3(P2) × F.1(total)  [P2 proportionate share of total insurance cost]

Section G (Child Care):
  G.1(P1) = net work-related child care paid by P1 (after child care tax credit reduction)
  G.1(P2) = net work-related child care paid by P2
  G.1(total) = G.1(P1) + G.1(P2)
  G.2(P1) = D.3(P1) × G.1(total)
  G.2(P2) = D.3(P2) × G.1(total)

Section H (Proportionate Child Support Obligation):
  Primary residency case:
    H.1(primary) = D.5(primary) + F.2(primary) + G.2(primary)
    H.1(non-primary) = D.5(non-primary) - E.4 + F.2(non-primary) + G.2(non-primary)

Section I (Basic Child Support Obligation):
  I.1(P1) = F.1(P1) + G.1(P1)   [credit for insurance + child care actually paid by P1]
  I.1(P2) = F.1(P2) + G.1(P2)
  I.2(P1) = H.1(P1) - I.1(P1)
  I.2(P2) = H.1(P2) - I.1(P2)
  [I.2 for the non-primary parent = the rebuttable presumptive amount before adjustments]

Section J (Adjustments — applied to non-primary parent's obligation):
  J.1 = long-distance parenting time costs (monthly)
  J.2 = income tax adjustment (monthly, +/-)
  J.3 = special needs (monthly)
  J.4 = support past majority (monthly)
  J.5 = overall financial condition (monthly, +/-)
  J.6 = J.1 + J.2 + J.3 + J.4 + J.5

Section K (Deviation from Presumptive Amount):
  K.1 = I.2 for non-primary parent
  K.2 = J.6
  K.3 = K.1 +/- K.2

Section K Social Security:
  K.4 = SS dependent benefit credit (reduces obligation)

Ability to Pay (Section VI.G — apply if K.3 > income available for support):
  Income available for support = D.1(non-primary) - federal poverty guideline (household of 1)
  2025 poverty guideline for household of 1 = $1,255/month (verify and update annually)
  If income available < K.3, court sets obligation based on best interest (flag this case)

Section L:
  L = K.3 - K.4  [net parental child support obligation]
  If L < 0, L = 0

Section M:
  M = enforcement fee allowance = (monthly fee × 0.5)  [half paid by non-primary]

Section N:
  N = L + M  [TOTAL CHILD SUPPORT ORDER — the final answer]
```

---

## PDF Generation (PdfGenerationService.cs)

Use **QuestPDF** to render a PDF that closely mirrors the official Kansas Child Support Worksheet layout from Appendix I of the guidelines.

### PDF Page 1 Content (mirrors Appendix I, Page 27)

**Header:**
- Court name: "IN THE [JUDICIAL DISTRICT] JUDICIAL DISTRICT, [COUNTY] COUNTY, KANSAS"
- Case style: "IN THE MATTER OF [Party 1] v. [Party 2]"
- Case number
- Title: "CHILD SUPPORT WORKSHEET"
- "Pursuant to K.S.A. Chapter 23"

**Section A — Income Computation (Wage Earner)**
- Two-column layout: Party 1 | Party 2
- Line A.1: Domestic Gross Income

**Section B — Income Computation (Self-Employed)**
- B.1: Self-employment Gross Income
- B.2: Reasonable Business Expenses (–)
- B.3: Domestic Gross Income

**Section C — Adjustments to Domestic Gross Income**
- C.1: Domestic Gross Income
- C.2: Court-Ordered Child Support Paid (–)
- C.3: Court-Ordered Maintenance Paid (%) [show percentage if post-2018]
- C.4: Court-Ordered Maintenance Received (%)
- C.5: Child Support Income → Insert on Line D.1

**Section D — Computation of Child Support**
- D.1: Child Support Income (two columns)
- D.2: Total Combined Income
- D.3: Proportionate Shares (% columns)
- D.4: Gross Child Support Obligation table:
  - Row: "Age of Children 0-5 / 6-11 / 12-18"
  - Row: "Number Per Age Category" (show count per group)
  - Row: "Total Amount = $XXX"
  - Checkbox indicators: Cost of Living Differential? / Multiple Family Adjustment? / Extended Formula?
- D.5: Proportionate Share (D.3 × D.4)

### PDF Page 2 Content (mirrors Appendix I, Page 28)

**Section E — Parenting Time or Shared Residency Adjustment**
- E.1.a through E.1.c, E.2, E.3, E.4

**Section F — Health Insurance**
- F.1, F.2

**Section G — Work Related Child Care Costs**
- G.1, G.2

**Section H — Proportionate Child Support Obligation**
- H.1

**Section I — Basic Child Support Obligation**
- I.1, I.2

### PDF Page 3 Content (mirrors Appendix I, Page 29)

**Section J — Child Support Adjustments**
- J.1 through J.6 with categories

**Section K — Deviation(s) from Rebuttable Presumption Amount**
- K.1, K.2, K.3, K.4, K.5 (Ability to Pay)
- K.6 Total

**Section L — Net Parental Child Support Obligation**
**Section M — Enforcement Fee Allowance**
**Section N — Total Child Support Obligation**

**Signature lines:**
- "Prepared by (Signature): _______________  Date Submitted: _______________"
- "Prepared by (Print Name): _______________"
- "Judge/Hearing Officer Signature: _______________  Date Approved: _______________"

**Footer note:** "Calculated using Kansas Child Support Guidelines effective July 1, 2025 pursuant to Kansas Supreme Court Administrative Order"

### PDF Styling
- Font: standard serif (e.g., Times New Roman or similar)
- Section headers: bold, slightly larger font
- Grid lines: thin borders on table cells
- Money values: right-aligned, dollar-formatted
- Percentages: right-aligned with "%" suffix
- Party names: dynamically inserted from user input
- All calculated values printed in the appropriate cells
- Blank lines for fields not applicable (e.g., E.2 if not using shared residency)
- Letter-size paper (8.5" × 11")
- Margins: 0.75" all sides

---

## UX & Accessibility Requirements

### Plain Language Rules
- Use "you" and "the other parent" — not "petitioner/respondent"
- Never use legal jargon without a plain-language explanation immediately following in parentheses
- All dollar input fields: no formatting required from user (accept "3500", "3,500", "3500.00")
- Error messages: friendly and specific ("Please enter the monthly amount you earn from your job" — NOT "Field required")

### Layout & Design
- **Mobile-first:** All steps must work well on a phone screen
- **Font size:** Minimum 16px base, larger for headings
- **Color contrast:** WCAG AA minimum
- **High contrast mode:** Support via CSS media query
- **No dark backgrounds** — white/light gray backgrounds with dark text
- Progress bar: clearly shows which step the user is on and how many remain
- Each field has a visible label above it (not placeholder text as label)
- Help text: shown in a clearly distinct style (e.g., light blue background, small icon)
- "Why do we need this?" expandable accordion on fields that may seem intrusive

### Input UX
- Dollar amount fields: show "$" prefix, accept numbers only
- Percentage fields: show "%" suffix
- Date of birth: date picker with manual entry fallback
- All radio button groups have a clear selected state
- Minimum tap target size: 44px
- "Back" button on every step (does NOT lose data)
- Auto-save to session on every field change (debounced, 500ms)

### Error Handling
- Inline validation on POST (not just on submit)
- Required fields clearly marked
- On error, scroll to first error
- Do not clear form data on validation error

---

## Business Logic Edge Cases

Handle all of the following explicitly:

1. **Zero income:** If either parent has $0 income, the schedule still produces an obligation. The app should not crash or produce $0 automatically — flag for "ability to pay" review.

2. **Unknown other parent income:** See Step 4. Suggest minimum wage imputation. Flag in PDF output: "Note: Other parent's income was estimated/unknown. Court may require income disclosure."

3. **50/50 parenting time:** Use Direct Expense Formula (Section VI.F). Calculate Step 1 (difference of D.5 values ÷ 2) and Step 2 (D.4 × income-based percentage). Display both components.

4. **Income above schedule maximum ($18,000/month combined):** Use extended formula with appropriate multiplier. Flag in output: "Combined income exceeds schedule — extended formula used (discretionary per guidelines)."

5. **Post-2018 maintenance:** Use 25% option (increase by 25%) unless user provides specific tax rate information. Show this clearly in output.

6. **Ability to pay:** If non-primary parent's calculated obligation exceeds their income minus the federal poverty guideline for household of one, flag prominently: "The calculated amount may exceed what this parent can pay. The court must consider their ability to pay under Section VI.G of the Guidelines."

7. **Multiple age groups:** If children span multiple age groups, look up each child separately from the same schedule (same total number of children, same combined income, different age column).

8. **Children turning a birthday before next review:** Note the age used (nearest birthday rule) in the PDF output.

9. **Self-employment tax:** The guidelines state reasonable business expenses MUST include the additional self-employment tax paid above the FICA rate (the "extra" SE tax). Mention this in the help text for business expenses.

10. **Cafeteria plans / pre-tax benefits:** Use gross wages before any salary reduction — the user should enter gross wages as if the cafeteria plan deduction didn't happen. Help text explains this.

11. **SS dependent benefits:** If the child's SS benefit equals or exceeds Line K.3, obligation = $0. If less than K.3, obligation = K.3 - SS benefit.

12. **Rounding:** All dollar values rounded to nearest dollar. All percentages rounded to nearest tenth (e.g., 63.2%). Use `Math.Round(value, MidpointRounding.AwayFromZero)` in C#.

---

## Supplemental Materials Integration

The app should reference (but not reproduce in full) these official supplemental documents. Include links or brief explanations in help text at the relevant steps:

| Document | Used In | Key Content |
|----------|---------|-------------|
| `CafeteriaPlansAndSalaryReductionAgreements.pdf` | Step 3/4 income | Gross wages = pre-cafeteria-plan amount |
| `Child-Care-Credit-Chart-2024.pdf` | Step 6 child care | Table of credit percentages by income |
| `Cost-of-living-differential.pdf` | Step 3/4 income | Formula: (KS_RPP - NS_RPP)/NS_RPP × income |
| `Filing-Single-Child-Tax-Credit-Phaseout-schedule-8-24-23.pdf` | Step 7 tax adj | Phase-out table for child tax credit |
| `Income-Tax-Considerations-8-24-23.pdf` | Step 7 tax adj | Head of household benefit calculation |
| `Military-Pay-and-Allowances.pdf` | Step 3/4 income | BAH, BAS, special pays all included |
| `Percentage-of-bonus-final.pdf` | Step 3/4 income | Two bonus calculation methods |
| K.S.A. 20-165 | About/Legal | Supreme Court authority for guidelines |
| K.S.A. 23-3002 | About/Legal | Court must follow guidelines; retirement plan provision |
| K.S.A. 38-2277 | About/Legal | Presumptions when facts unknown |

---

## Testing Requirements

### Unit Tests (xUnit)

Write tests in `tests/KansasChildSupport.Tests/CalculationServiceTests.cs`:

1. **Basic 1-child calculation test:** P1 income $3,000/mo, P2 income $2,000/mo, 1 child age 7, no adjustments → verify D.2 = $5,000, D.3 proportions 60/40, D.4 from schedule, D.5 values, final Line N.

2. **Two children, different age groups:** P1 $4,500, P2 $3,500, Child 1 age 3, Child 2 age 14 → verify D.4 uses two-child schedule correctly.

3. **Parenting time adjustment:** 40% parenting time for non-primary → verify 20% reduction applied correctly.

4. **Post-2018 maintenance adjustment:** $500/mo maintenance paid → verify 25% adjustment = $625 deducted.

5. **Child care credit reduction:** $800/mo child care, annual income $35,000, 1 child → verify $60/mo credit subtracted, net = $740.

6. **Zero income edge case:** One parent with $0 income → no crash, correct proportionate shares.

7. **Income above schedule:** Combined income $20,000 → extended formula used, result is numeric.

8. **SS dependent benefit:** K.3 = $500, SS benefit = $300 → Line L = $200.

9. **SS dependent benefit exceeds obligation:** K.3 = $300, SS benefit = $400 → Line L = $0.

10. **Enforcement fee:** $20/mo fee → M = $10 (half), N = L + $10.

### Schedule Lookup Tests (ScheduleLookupTests.cs)

1. Exact table value: combined $3,000, 1 child, age 6–11 → should return $612
2. Interpolation: combined $3,050, 1 child, age 12–18 → verify interpolated between $3,000 and $3,100 rows
3. Two-child schedule: combined $5,000, age 0–5 → $626 (verify against PDF table)
4. Extended formula: combined $20,000, 1 child, age 12–18 → verify formula result

---

## Program.cs Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Register services
builder.Services.AddScoped<ICalculationService, CalculationService>();
builder.Services.AddScoped<IScheduleLookupService, ScheduleLookupService>();
builder.Services.AddScoped<IChildTaxCreditService, ChildTaxCreditService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();

// QuestPDF license (Community = free for open source)
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

---

## NuGet Packages Required

```xml
<ItemGroup>
  <PackageReference Include="QuestPDF" Version="2024.3.0" />
  <PackageReference Include="Microsoft.AspNetCore.Session" Version="2.2.0" />
</ItemGroup>

<!-- Tests project -->
<ItemGroup>
  <PackageReference Include="xunit" Version="2.7.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
</ItemGroup>
```

---

## Legal Disclaimers (Required on Every Page)

Include in the shared `_Layout.cshtml` footer:

> This tool is for informational purposes only and does not constitute legal advice. Results represent the rebuttable presumptive amount under Kansas Child Support Guidelines (effective July 1, 2025) and may be adjusted by a court. Always consult a licensed Kansas attorney if you have questions about your specific situation.
>
> **Free legal help:** Kansas Legal Services — [kansaslegalservices.org](https://kansaslegalservices.org) — 1-800-723-6953

Also include on the Review step and the Complete page.

---

## GitHub / Deployment Notes

- Include a `.github/workflows/dotnet.yml` CI pipeline that runs `dotnet build` and `dotnet test` on push to `main`
- Include `.gitignore` for .NET projects
- Include a `README.md` with:
  - Project description and purpose
  - Setup instructions (`dotnet run`)
  - Guidelines version reference
  - Link to Kansas Judicial Branch child support resources
  - Legal disclaimer
- The app should work without any environment variables or external services (all data in-memory / session)
- For production: document that session should be backed by Redis or SQL for multi-server deployments (add note in README, not required for MVP)

---

## Source Documents Summary

All calculation logic derives from these official Kansas documents:

1. **Kansas Child Support Guidelines** (effective July 1, 2025) — primary source for all formulas, line definitions, and schedule tables
2. **Child Care Credit Chart 2024** — for Section G child care tax credit reduction
3. **Cost of Living Differential guide** — for Section E.4 / income adjustment when a parent lives out of state
4. **Income Tax Considerations** — for Section J.2 tax adjustment calculations
5. **Filing Single Child Tax Credit Phase-Out Schedule** — for child tax credit phase-out
6. **Cafeteria Plans and Salary Reduction Agreements** — income definition guidance
7. **Military Pay and Allowances** — military income inclusion rules
8. **Percentage of Bonus** — bonus income calculation options
9. **K.S.A. 20-165** — Supreme Court authority to establish guidelines
10. **K.S.A. 23-3002** — court's duty to follow guidelines; retirement plan provision
11. **K.S.A. 38-2277** — presumptions when income is unknown/unproven

---

## What "Done" Looks Like

The app is complete when:
- [ ] All 8 wizard steps render correctly on mobile and desktop
- [ ] All calculations match the official worksheet line by line for common scenarios
- [ ] The generated PDF looks professional and matches the Appendix I layout
- [ ] The PDF can be downloaded and printed
- [ ] All unit tests pass
- [ ] Legal disclaimers present on every page
- [ ] No crashes on edge cases (zero income, missing data, above-schedule income)
- [ ] Session persists correctly through all 8 steps
- [ ] Back navigation works without data loss
- [ ] CI pipeline passes on GitHub
