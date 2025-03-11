using Microsoft.AspNetCore.Identity;
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

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// Register services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IQuestionLogService, QuestionLogService>();

var app = builder.Build();

// Ensure database is created and seed default user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Create default admin role if it doesn't exist
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }
        
        // Create default admin user if it doesn't exist
        var adminUser = await userManager.FindByEmailAsync("admin@mistral-ocr.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@mistral-ocr.com",
                Email = "admin@mistral-ocr.com",
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database or seeding data.");
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
