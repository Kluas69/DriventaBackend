using System.Text;
using Driventa.Application.Interfaces;
using Driventa.Infrastructure.Identity;
using Driventa.Infrastructure.Services;
using Driventa.Infrastructure.Persistence;
using Driventa.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

using Driventa.Application.Validation.Validators;
using FluentValidation;

namespace Driventa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<PublicApplicationRequestValidator>();

        // Database
        // Accepts a Neon/Railway URI (postgres://...) or a plain Npgsql key/value
        // string, and normalizes it so Npgsql can parse it in both cases.
        var rawConnectionString =
     Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
     ?? Environment.GetEnvironmentVariable("DATABASE_URL")
     ?? configuration.GetConnectionString("DefaultConnection");

        var connectionString = BuildNpgsqlConnectionString(rawConnectionString);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Identity
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders()
        .AddSignInManager();

        // JWT Authentication
        var jwtSettings = new JwtSettings();
        configuration.GetSection("Jwt").Bind(jwtSettings);
        services.AddSingleton(jwtSettings);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) &&
                        (path.StartsWithSegments("/hubs/chat") || path.StartsWithSegments("/hubs/notifications")))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            // Legacy role-based policies (kept for backward compatibility)
            options.AddPolicy("CanManageApplications", p =>
                p.RequireRole("SuperAdmin", "Admin", "DispatchManager", "Dispatcher"));
            options.AddPolicy("CanManageCarriers", p =>
                p.RequireRole("SuperAdmin", "Admin", "DispatchManager"));
            options.AddPolicy("CanManageLoads", p =>
                p.RequireRole("SuperAdmin", "Admin", "DispatchManager", "Dispatcher"));
            options.AddPolicy("CanManageTrucks", p =>
                p.RequireRole("SuperAdmin", "Admin", "DispatchManager"));
            options.AddPolicy("CanManageDrivers", p =>
                p.RequireRole("SuperAdmin", "Admin", "DispatchManager"));
            options.AddPolicy("CanViewBrokers", p =>
                p.RequireRole("SuperAdmin", "Admin", "DispatchManager"));
            options.AddPolicy("CanManageFinance", p =>
                p.RequireRole("SuperAdmin", "Admin"));
            options.AddPolicy("CanViewReports", p =>
                p.RequireRole("SuperAdmin", "Admin", "DispatchManager", "Dispatcher"));
            options.AddPolicy("CanManageSettings", p =>
                p.RequireRole("SuperAdmin"));
            options.AddPolicy("CanAssignDispatchers", p =>
                p.RequireRole("SuperAdmin", "Admin", "DispatchManager"));

            // Permission-based policies
            // Users
            options.AddPolicy("users.view", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "users.view")));
            options.AddPolicy("users.create", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "users.create")));
            options.AddPolicy("users.edit", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "users.edit")));
            options.AddPolicy("users.delete", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "users.delete")));

            // Applications
            options.AddPolicy("applications.view", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "applications.view")));
            options.AddPolicy("applications.edit", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "applications.edit")));
            options.AddPolicy("applications.assign", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "applications.assign")));
            options.AddPolicy("applications.convert", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "applications.convert")));

            // Carriers
            options.AddPolicy("carriers.view", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "carriers.view")));
            options.AddPolicy("carriers.create", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "carriers.create")));
            options.AddPolicy("carriers.edit", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "carriers.edit")));

            // Loads
            options.AddPolicy("loads.view", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "loads.view")));
            options.AddPolicy("loads.create", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "loads.create")));
            options.AddPolicy("loads.edit", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "loads.edit")));
            options.AddPolicy("loads.updateStatus", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "loads.updateStatus")));

            // Billing
            options.AddPolicy("billing.view", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "billing.view")));
            options.AddPolicy("billing.create", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "billing.create")));
            options.AddPolicy("billing.manage", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "billing.manage")));

            // Reports
            options.AddPolicy("reports.view", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "reports.view")));

            // Roles
            options.AddPolicy("roles.manage", p => p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "permission" && c.Value == "roles.manage")));
        });

        // Repositories
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<ICarrierRepository, CarrierRepository>();
        services.AddScoped<ILoadRepository, LoadRepository>();
        services.AddScoped<ITruckRepository, TruckRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IBrokerRepository, BrokerRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();

        // Services
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    /// <summary>
    /// Accepts either a standard Npgsql key/value connection string or a URI-style
    /// one (postgres://user:pass@host:port/db?sslmode=require) as handed out by
    /// Neon, Railway, Heroku, etc., and returns a key/value string Npgsql can parse.
    /// </summary>
    private static string BuildNpgsqlConnectionString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "No database connection string configured. Set ConnectionStrings:DefaultConnection " +
                "or the DATABASE_URL environment variable.");
        }

        var value = raw.Trim();

        // Already a key/value string (Host=...;Database=...) – use it as-is.
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null
        };

        // Translate the query parameters we support. Unknown ones (e.g.
        // channel_binding) are ignored – Npgsql negotiates channel binding
        // automatically once the connection is encrypted.
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(kv[0]);
            var val = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;

            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                builder.SslMode = val.ToLowerInvariant() switch
                {
                    "disable" => SslMode.Disable,
                    "allow" => SslMode.Allow,
                    "prefer" => SslMode.Prefer,
                    "require" => SslMode.Require,
                    "verify-ca" => SslMode.VerifyCA,
                    "verify-full" => SslMode.VerifyFull,
                    _ => builder.SslMode
                };
            }
        }

        return builder.ConnectionString;
    }
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}