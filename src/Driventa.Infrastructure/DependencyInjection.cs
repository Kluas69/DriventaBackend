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
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

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
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}