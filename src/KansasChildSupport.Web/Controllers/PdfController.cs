using KansasChildSupport.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace KansasChildSupport.Web.Controllers;

public class PdfController : Controller
{
    private readonly IPdfGenerationService _pdfService;
    private readonly ISessionService _sessionService;

    public PdfController(IPdfGenerationService pdfService, ISessionService sessionService)
    {
        _pdfService = pdfService;
        _sessionService = sessionService;
    }

    [HttpPost("/pdf/generate")]
    [HttpGet("/pdf/generate")]
    public IActionResult Generate()
    {
        var session = _sessionService.GetSession(HttpContext.Session);
        if (session.CalculatedResult == null)
            return RedirectToAction("Step8", "Worksheet");

        try
        {
            var pdfBytes = _pdfService.GeneratePdf(session);
            var caseInfo = session.CaseInfo;
            var fileName = $"KansasChildSupportWorksheet_{caseInfo?.Party1Name?.Replace(" ", "_") ?? "Unknown"}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            TempData["PdfError"] = $"Could not generate PDF: {ex.Message}";
            return RedirectToAction("Step8", "Worksheet");
        }
    }
}
