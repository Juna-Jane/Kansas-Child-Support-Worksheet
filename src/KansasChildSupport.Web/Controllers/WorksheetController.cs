using KansasChildSupport.Web.Models;
using KansasChildSupport.Web.Models.Steps;
using KansasChildSupport.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace KansasChildSupport.Web.Controllers;

public class WorksheetController : Controller
{
    private readonly ISessionService _sessionService;
    private readonly ICalculationService _calculationService;

    public WorksheetController(ISessionService sessionService, ICalculationService calculationService)
    {
        _sessionService = sessionService;
        _calculationService = calculationService;
    }

    private WorksheetSession GetSession() => _sessionService.GetSession(HttpContext.Session);
    private void SaveSession(WorksheetSession s) => _sessionService.SaveSession(HttpContext.Session, s);

    private bool SessionExpired(WorksheetSession s, int requiredStep)
    {
        if (requiredStep <= 1) return false;
        if (requiredStep >= 2 && s.CaseInfo == null) return true;
        if (requiredStep >= 3 && s.Children == null) return true;
        if (requiredStep >= 4 && s.Parent1Income == null) return true;
        if (requiredStep >= 5 && s.Parent2Income == null) return true;
        if (requiredStep >= 6 && s.ParentingTime == null) return true;
        if (requiredStep >= 7 && s.InsuranceChildcare == null) return true;
        if (requiredStep >= 8 && s.Adjustments == null) return true;
        return false;
    }

    // ============== INDEX ==============
    public IActionResult Index() => RedirectToAction("Step1");

    // ============== STEP 1 ==============
    [HttpGet("worksheet/step1")]
    public IActionResult Step1()
    {
        var session = GetSession();
        var model = session.CaseInfo ?? new Step01_CaseInfoModel();
        if (TempData["ExpiredMessage"] != null)
            ViewBag.ExpiredMessage = TempData["ExpiredMessage"];
        return View("Step01_CaseInfo", model);
    }

    [HttpPost("worksheet/step1")]
    public IActionResult Step1Post(Step01_CaseInfoModel model)
    {
        if (!ModelState.IsValid)
            return View("Step01_CaseInfo", model);

        var session = GetSession();
        session.CaseInfo = model;
        SaveSession(session);
        return RedirectToAction("Step2");
    }

    // ============== STEP 2 ==============
    [HttpGet("worksheet/step2")]
    public IActionResult Step2()
    {
        var session = GetSession();
        if (SessionExpired(session, 2))
        {
            TempData["ExpiredMessage"] = "Your session expired. Let's start over — it should only take about 15 minutes.";
            return RedirectToAction("Step1");
        }
        var model = session.Children ?? new Step02_ChildrenModel();
        if (model.Children.Count == 0) model.Children.Add(new ChildEntry());
        return View("Step02_Children", model);
    }

    [HttpPost("worksheet/step2")]
    public IActionResult Step2Post(Step02_ChildrenModel model)
    {
        if (model.Children == null || model.Children.Count == 0)
        {
            ModelState.AddModelError("", "Please add at least one child.");
            return View("Step02_Children", model);
        }
        // Remove children with no name
        model.Children = model.Children.Where(c => !string.IsNullOrWhiteSpace(c.FirstName)).ToList();
        if (model.Children.Count == 0)
        {
            ModelState.AddModelError("", "Please add at least one child with a name.");
            return View("Step02_Children", model);
        }
        if (!ModelState.IsValid)
            return View("Step02_Children", model);

        var session = GetSession();
        session.Children = model;
        SaveSession(session);
        return RedirectToAction("Step3");
    }

    // ============== STEP 3 ==============
    [HttpGet("worksheet/step3")]
    public IActionResult Step3()
    {
        var session = GetSession();
        if (SessionExpired(session, 3))
        {
            TempData["ExpiredMessage"] = "Your session expired. Let's start over — it should only take about 15 minutes.";
            return RedirectToAction("Step1");
        }
        var model = session.Parent1Income ?? new Step03_Parent1IncomeModel();
        ViewBag.ParentName = session.CaseInfo?.Party1Name ?? "Party 1";
        return View("Step03_Parent1Income", model);
    }

    [HttpPost("worksheet/step3")]
    public IActionResult Step3Post(Step03_Parent1IncomeModel model)
    {
        var session = GetSession();
        ViewBag.ParentName = session.CaseInfo?.Party1Name ?? "Party 1";
        if (!ModelState.IsValid)
            return View("Step03_Parent1Income", model);

        session.Parent1Income = model;
        SaveSession(session);
        return RedirectToAction("Step4");
    }

    // ============== STEP 4 ==============
    [HttpGet("worksheet/step4")]
    public IActionResult Step4()
    {
        var session = GetSession();
        if (SessionExpired(session, 4))
        {
            TempData["ExpiredMessage"] = "Your session expired. Let's start over — it should only take about 15 minutes.";
            return RedirectToAction("Step1");
        }
        var model = session.Parent2Income ?? new Step04_Parent2IncomeModel();
        ViewBag.ParentName = session.CaseInfo?.Party2Name ?? "Party 2";
        ViewBag.MinWageSuggestion = 1254m; // $7.25 × 40 × 4.33
        return View("Step04_Parent2Income", model);
    }

    [HttpPost("worksheet/step4")]
    public IActionResult Step4Post(Step04_Parent2IncomeModel model)
    {
        var session = GetSession();
        ViewBag.ParentName = session.CaseInfo?.Party2Name ?? "Party 2";
        ViewBag.MinWageSuggestion = 1254m;
        if (!ModelState.IsValid)
            return View("Step04_Parent2Income", model);

        session.Parent2Income = model;
        SaveSession(session);
        return RedirectToAction("Step5");
    }

    // ============== STEP 5 ==============
    [HttpGet("worksheet/step5")]
    public IActionResult Step5()
    {
        var session = GetSession();
        if (SessionExpired(session, 5))
        {
            TempData["ExpiredMessage"] = "Your session expired. Let's start over — it should only take about 15 minutes.";
            return RedirectToAction("Step1");
        }
        var model = session.ParentingTime ?? new Step05_ParentingTimeModel();
        ViewBag.PrimaryName = session.CaseInfo?.PrimaryCustody == "Party2"
            ? session.CaseInfo?.Party2Name ?? "Party 2"
            : session.CaseInfo?.Party1Name ?? "Party 1";
        ViewBag.NonPrimaryName = session.CaseInfo?.PrimaryCustody == "Party2"
            ? session.CaseInfo?.Party1Name ?? "Party 1"
            : session.CaseInfo?.Party2Name ?? "Party 2";
        return View("Step05_ParentingTime", model);
    }

    [HttpPost("worksheet/step5")]
    public IActionResult Step5Post(Step05_ParentingTimeModel model)
    {
        var session = GetSession();
        if (!ModelState.IsValid)
            return View("Step05_ParentingTime", model);

        session.ParentingTime = model;
        SaveSession(session);
        return RedirectToAction("Step6");
    }

    // ============== STEP 6 ==============
    [HttpGet("worksheet/step6")]
    public IActionResult Step6()
    {
        var session = GetSession();
        if (SessionExpired(session, 6))
        {
            TempData["ExpiredMessage"] = "Your session expired. Let's start over — it should only take about 15 minutes.";
            return RedirectToAction("Step1");
        }
        var model = session.InsuranceChildcare ?? new Step06_InsuranceChildcareModel();
        ViewBag.P1Name = session.CaseInfo?.Party1Name ?? "Party 1";
        ViewBag.P2Name = session.CaseInfo?.Party2Name ?? "Party 2";
        return View("Step06_InsuranceChildcare", model);
    }

    [HttpPost("worksheet/step6")]
    public IActionResult Step6Post(Step06_InsuranceChildcareModel model)
    {
        var session = GetSession();
        ViewBag.P1Name = session.CaseInfo?.Party1Name ?? "Party 1";
        ViewBag.P2Name = session.CaseInfo?.Party2Name ?? "Party 2";
        if (!ModelState.IsValid)
            return View("Step06_InsuranceChildcare", model);

        session.InsuranceChildcare = model;
        SaveSession(session);
        return RedirectToAction("Step7");
    }

    // ============== STEP 7 ==============
    [HttpGet("worksheet/step7")]
    public IActionResult Step7()
    {
        var session = GetSession();
        if (SessionExpired(session, 7))
        {
            TempData["ExpiredMessage"] = "Your session expired. Let's start over — it should only take about 15 minutes.";
            return RedirectToAction("Step1");
        }
        var model = session.Adjustments ?? new Step07_AdjustmentsModel();
        return View("Step07_Adjustments", model);
    }

    [HttpPost("worksheet/step7")]
    public IActionResult Step7Post(Step07_AdjustmentsModel model)
    {
        var session = GetSession();
        if (!ModelState.IsValid)
            return View("Step07_Adjustments", model);

        session.Adjustments = model;

        // Run calculations
        try
        {
            session.CalculatedResult = _calculationService.Calculate(session);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Calculation error: {ex.Message}");
            return View("Step07_Adjustments", model);
        }

        SaveSession(session);
        return RedirectToAction("Step8");
    }

    // ============== STEP 8 ==============
    [HttpGet("worksheet/step8")]
    public IActionResult Step8()
    {
        var session = GetSession();
        if (SessionExpired(session, 8))
        {
            TempData["ExpiredMessage"] = "Your session expired. Let's start over — it should only take about 15 minutes.";
            return RedirectToAction("Step1");
        }
        ViewBag.Session = session;
        return View("Step08_Review", session);
    }

    [HttpPost("worksheet/step8")]
    public IActionResult Step8Post()
    {
        return RedirectToAction("Complete");
    }

    // ============== COMPLETE ==============
    [HttpGet("worksheet/complete")]
    public IActionResult Complete()
    {
        var session = GetSession();
        if (session.CalculatedResult == null)
            return RedirectToAction("Step1");
        return View("Complete", session);
    }

    // ============== START OVER ==============
    [HttpPost("worksheet/startover")]
    public IActionResult StartOver()
    {
        _sessionService.ClearSession(HttpContext.Session);
        return RedirectToAction("Step1");
    }
}
