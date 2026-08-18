using API.Data;
using API.Services;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add CORS configuration to allow Vue.js frontend requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost",
                "http://localhost:80",
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:3000",
                "http://localhost:8080")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
// Add Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Business Services
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IEmployeeSyncService, EmployeeSyncService>();
// Add hosted worker to periodically sync employees from HR Service
builder.Services.AddHostedService<API.Services.EmployeeSyncWorker>();

// Note: Program also performs a one-time sync at startup (see background Task below). The worker ensures periodic updates.

// Add HR Service Client (Group 1's HR Service)
// Configure the base address from appsettings.json
var hrServiceUrl = builder.Configuration["ExternalServices:HrServiceUrl"] ?? "https://172.16.6.17:7084";
// Ensure URL doesn't have trailing slash (HttpClient will append it when needed)
hrServiceUrl = hrServiceUrl.TrimEnd('/');

builder.Services.AddHttpClient<IHrServiceClient, HrServiceClient>(client =>
{
    client.BaseAddress = new Uri(hrServiceUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // Note: In development, we bypass SSL certificate validation
    // CRITICAL: This must be removed in production environments!
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    }
    return handler;
});

// Add AutoMapper
builder.Services.AddAutoMapper(cfg => { }, typeof(Program));

// Add Swagger/OpenAPIS
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// --- TỰ ĐỘNG TẠO/CẬP NHẬT DATABASE (AttendanceDB) KHI KHỞI ĐỘNG ---
// Trước đây không có bước này nên database "AttendanceDB" không bao giờ được
// tạo trên SQL Server, khiến mọi query đều lỗi "Cannot open database
// AttendanceDB... Login failed". Thêm MigrateAsync() giống HR Core Service
// để tự tạo DB + áp dụng migration lúc container khởi động.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.MigrateAsync();
        startupLogger.LogInformation("✅ AttendanceDB migration hoàn tất.");
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "❌ Lỗi khi migrate AttendanceDB. Service có thể không hoạt động đúng.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

// Use CORS policy (must be before UseAuthorization)
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();
app.MapStaticAssets();

// 🔄 Sync employees and repair shift names in background (non-blocking)
_ = Task.Run(async () =>
{
    // Wait 2 seconds for application to fully start
    await Task.Delay(2000);

    using (var scope = app.Services.CreateScope())
    {
        var syncService = scope.ServiceProvider.GetRequiredService<IEmployeeSyncService>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // 🛠 Repair shift names
        try
        {
            logger.LogInformation("🚀 [BACKGROUND JOB] Verifying and repairing shift names in database...");
            var shift1 = await context.Shifts.FindAsync(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            if (shift1 != null)
            {
                shift1.Name = "Ca Hành chính";
                shift1.Description = "Ca làm việc chính từ 8:00 đến 17:30";
            }
            var shift2 = await context.Shifts.FindAsync(Guid.Parse("00000000-0000-0000-0000-000000000002"));
            if (shift2 != null)
            {
                shift2.Name = "Ca Sáng";
                shift2.Description = "Ca sáng từ 7:00 đến 12:00";
            }
            var shift3 = await context.Shifts.FindAsync(Guid.Parse("00000000-0000-0000-0000-000000000003"));
            if (shift3 != null)
            {
                shift3.Name = "Ca Chiều";
                shift3.Description = "Ca chiều từ 13:00 đến 18:00";
            }
            await context.SaveChangesAsync();
            logger.LogInformation("✅ [BACKGROUND JOB] Shift names successfully verified and updated");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"⚠️ [BACKGROUND JOB] Shift repair failed: {ex.Message}");
        }

        // 🔄 Sync employees
        try
        {
            logger.LogInformation("🚀 [BACKGROUND JOB] Starting employee sync from HR Service...");
            var syncResult = await syncService.SyncEmployeesAsync();
            logger.LogInformation($"✅ [BACKGROUND JOB] Employee sync completed: {syncResult} employees synced");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"⚠️ [BACKGROUND JOB] Employee sync failed: {ex.Message}. Application will continue with existing employees.");
        }
    }
});

app.Run();
