using KansasChildSupport.Web.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    var timeoutHours = builder.Configuration.GetValue<int>("Session:TimeoutHours", 2);
    options.IdleTimeout = TimeSpan.FromHours(timeoutHours);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
    pattern: "{controller=Worksheet}/{action=Index}/{id?}");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
