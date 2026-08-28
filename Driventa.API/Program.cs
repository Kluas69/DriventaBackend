using Driventa.API.Filters;
using Driventa.API.Hubs;
using Driventa.API.Middleware;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Driventa.Infrastructure;
using Driventa.Infrastructure.Identity;
using Driventa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Railway: Bind to the PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Infrastructure (DbContext, Identity, JWT, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR
builder.Services.AddSignalR();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("PublicEndpoints", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

// Controllers with validation filter
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationActionFilter>();
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dashboard", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://driventa.us",
                "https://dashboard.driventa.us")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Driventa API",
        Version = "v1",
        Description = "Backend API for Driventa Dispatch Management System"
    });

    options.OperationFilter<FormFileOperationFilter>();

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Apply pending migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        logger.LogInformation("Connecting to database...");

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var pendingCount = pendingMigrations.Count();

        if (pendingCount > 0)
        {
            logger.LogInformation("Applying {Count} pending migration(s)...", pendingCount);
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date. No pending migrations.");
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await RoleSeeder.SeedRolesAsync(roleManager);
        await RoleSeeder.SeedSuperAdminAsync(userManager);
        logger.LogInformation("Roles and SuperAdmin seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "DATABASE STARTUP FAILED: {Message}", ex.Message);
        throw;
    }
}

// Middleware pipeline
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Driventa API v1");
    });
}

app.UseStaticFiles();
app.UseRateLimiter();
app.UseCors("Dashboard");
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapControllers();

app.Run();