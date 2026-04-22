# Kansas Child Support Worksheet

A guided, plain-language web application that walks low-income, low-literacy Kansas residents through completing the official **Kansas Child Support Worksheet** (Appendix I of the Kansas Child Support Guidelines, effective July 1, 2025).

The app collects all required information across multiple friendly steps, performs all calculations, and produces a completed, court-ready PDF — ready to sign and submit.

---

## Purpose

**Target user:** A parent with no legal training who needs to file or respond to a child support action in Kansas.

**End output:** A completed PDF matching the official Kansas Child Support Worksheet (Appendix I), filled with calculated values, suitable for court submission.

---

## Setup & Running

### Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Run locally

```bash
git clone <repo-url>
cd Kansas-Child-Support-Worksheet
dotnet run --project src/KansasChildSupport.Web
```

The app will start at `https://localhost:5001` (or `http://localhost:5000`).

### Run tests

```bash
dotnet test
```

---

## Technology Stack

- **Language:** C# / .NET 8
- **Framework:** ASP.NET Core 8 (MVC)
- **PDF Generation:** QuestPDF (Community license)
- **Styling:** Plain CSS, mobile-first
- **JavaScript:** Vanilla JS only
- **Session storage:** In-memory (`ISession`)
- **Tests:** xUnit

---

## Kansas Child Support Guidelines Reference

This application implements calculations per the:

- **Kansas Child Support Guidelines** effective July 1, 2025 (Kansas Supreme Court Administrative Order)
- **K.S.A. 20-165** — Supreme Court authority to establish guidelines
- **K.S.A. 23-3002** — Court's duty to follow guidelines
- **K.S.A. 38-2277** — Presumptions when income is unknown

Official Kansas Courts resource:
[kscourts.org — Child Support Resources](https://kscourts.org/Kansas-Courts/Self-Help)

---

## Production Deployment Notes

For multi-server deployments, replace the in-memory session with a distributed cache:

```csharp
// Instead of AddDistributedMemoryCache(), use:
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
// or SQL Server:
builder.Services.AddDistributedSqlServerCache(options => { ... });
```

The app works without any environment variables in development mode.

---

## Legal Disclaimer

This tool is for informational purposes only and does not constitute legal advice.
Results represent the rebuttable presumptive amount under Kansas Child Support Guidelines
(effective July 1, 2025) and may be adjusted by a court. Always consult a licensed Kansas
attorney if you have questions about your specific situation.

**Free legal help:**
- Kansas Legal Services: [kansaslegalservices.org](https://kansaslegalservices.org) — 1-800-723-6953
- Kansas Bar Association Lawyer Referral: (785) 234-5696
- [Kansas Courts Self-Help Center](https://kscourts.org/Kansas-Courts/Self-Help)
