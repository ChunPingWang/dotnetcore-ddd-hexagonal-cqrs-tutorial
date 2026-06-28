using System.Text;
using BankAccountQuery.Application.Behaviors;
using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Application.Queries.Account;
using BankAccountQuery.Infrastructure.Adapters.In.Web;
using BankAccountQuery.Infrastructure.Adapters.Out.AuditLog;
using BankAccountQuery.Infrastructure.Adapters.Out.Persistence;
using BankAccountQuery.Infrastructure.Adapters.Out.RequestContext;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BankAccountQuery.Infrastructure.Configuration;

public static class DependencyInjection
{
    public const string JwtSigningKeyConfigPath = "Jwt:SigningKey";

    public static IServiceCollection AddBankingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddApplicationCore();
        services.AddWebAdapters();
        services.AddJwtAuthentication(configuration);
        return services;
    }

    // ── 持久化（Driven Adapters）────────────────────────────────────────
    private static IServiceCollection AddPersistence(
        this IServiceCollection services, IConfiguration configuration)
    {
        var dbName = configuration["Database:InMemoryName"] ?? "BankAccountQueryDb";
        services.AddDbContext<BankDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<ILoadAccountPort, AccountEfCoreAdapter>();
        services.AddScoped<ILoadTransactionPort, TransactionEfCoreAdapter>();
        services.AddScoped<ILoadPrivilegePort, PrivilegeEfCoreAdapter>();
        services.AddSingleton<IAuditLogPort, InMemoryAuditLogAdapter>();
        return services;
    }

    // ── MediatR + Validators + Pipeline Behaviors ───────────────────────
    private static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        var applicationAssembly = typeof(GetTwdTransactionHistoryHandler).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);

            // Pipeline Behavior 依序執行（順序即優先級）
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuditLogBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly);
        return services;
    }

    // ── Web 相關 Driving Adapter 支援元件 ───────────────────────────────
    private static IServiceCollection AddWebAdapters(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IRequestContextPort, HttpRequestContextAdapter>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    // ── JWT Bearer 認證 ─────────────────────────────────────────────────
    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var signingKey = configuration[JwtSigningKeyConfigPath]
            ?? "dev-only-super-secret-signing-key-please-change-32+chars";
        var issuer = configuration["Jwt:Issuer"] ?? "BankAccountQuery";
        var audience = configuration["Jwt:Audience"] ?? "BankAccountQuery.Clients";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(signingKey))
                };
            });

        services.AddAuthorization();
        return services;
    }
}
