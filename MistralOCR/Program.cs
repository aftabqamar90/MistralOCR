using Microsoft.EntityFrameworkCore;
using MistralOCR.Data;
using MistralOCR.Models;
using MistralOCR.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure AppSettings
builder.Services.Configure<AppSettings>(builder.Configuration);

// Configure Kestrel server timeouts
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    var kestrelLimits = builder.Configuration.GetSection("Kestrel:Limits");
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.Parse(kestrelLimits["KeepAliveTimeout"] ?? "00:03:00");
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.Parse(kestrelLimits["RequestHeadersTimeout"] ?? "00:03:00");
    serverOptions.Limits.MaxRequestBodySize = long.Parse(kestrelLimits["MaxRequestBodySize"] ?? "209715200");
});

// Add services to the container.
builder.Services.AddControllersWithViews(options => 
{
    options.MaxModelValidationErrors = 50;
    options.MaxModelBindingCollectionSize = 2000;
});

// Register HttpClient with timeout from configuration
builder.Services.AddHttpClient<IMistralService, MistralService>()
    .ConfigureHttpClient(client => {
        var timeoutSeconds = builder.Configuration.GetValue<int>("MistralAI:Timeouts:RequestTimeoutSeconds", 180);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    });

// Add SQLite database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=mistral_ocr.db"));

// Register services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IQuestionLogService, QuestionLogService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
