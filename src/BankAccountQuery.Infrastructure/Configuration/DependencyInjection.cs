using System.Text;
using BankAccountQuery.Application.Behaviors;
using BankAccountQuery.Application.Commands.Privilege;
using BankAccountQuery.Application.Common;
using BankAccountQuery.Application.Ports.Out;
using BankAccountQuery.Application.Queries.Account;
using BankAccountQuery.Domain.Model.Privilege;
using BankAccountQuery.Infrastructure.Adapters.In.Web;
using BankAccountQuery.Infrastructure.Adapters.Out.AuditLog;
using BankAccountQuery.Infrastructure.Adapters.Out.Events;
using BankAccountQuery.Infrastructure.Adapters.Out.Persistence;
using BankAccountQuery.Infrastructure.Adapters.Out.RequestContext;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BankAccountQuery.Infrastructure.Configuration;

public static class DependencyInjection
{
    public const string JwtSigningKeyConfigPath = "Jwt:SigningKey";

    public static IServiceCollection AddBankingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddDomainEvents();
        services.AddApplicationCore();
        services.AddWebAdapters();
        services.AddJwtAuthentication(configuration);
        services.AddObservability();
        services.AddSwaggerDocumentation();
        return services;
    }

    // ── 持久化（Driven Adapters）────────────────────────────────────────
    private static IServiceCollection AddPersistence(
        this IServiceCollection services, IConfiguration configuration)
    {
        // 以設定切換資料庫供應者：預設 InMemory（測試/開發），可改為 Postgres。
        var provider = configuration["Database:Provider"] ?? "InMemory";
        if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString =
                configuration.GetConnectionString("BankDb")
                ?? configuration["Database:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Database:Provider=Postgres 需設定連線字串（ConnectionStrings:BankDb）");
            services.AddDbContext<BankDbContext>(options =>
                options.UseNpgsql(connectionString));
        }
        else
        {
            var dbName = configuration["Database:InMemoryName"] ?? "BankAccountQueryDb";
            services.AddDbContext<BankDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        }

        services.AddScoped<ILoadAccountPort, AccountEfCoreAdapter>();
        services.AddScoped<ILoadTransactionPort, TransactionEfCoreAdapter>();
        services.AddSingleton<IAuditLogPort, InMemoryAuditLogAdapter>();

        // 優惠的持久化方式可切換：狀態儲存（預設）或事件溯源（opt-in 範例）
        var privilegePersistence = configuration["Privilege:Persistence"] ?? "StateBased";
        if (privilegePersistence.Equals("EventSourced", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<EventSourcedPrivilegeAdapter>();
            services.AddScoped<ILoadPrivilegePort>(sp => sp.GetRequiredService<EventSourcedPrivilegeAdapter>());
            services.AddScoped<ISavePrivilegePort>(sp => sp.GetRequiredService<EventSourcedPrivilegeAdapter>());
        }
        else
        {
            services.AddScoped<PrivilegeEfCoreAdapter>();
            services.AddScoped<ILoadPrivilegePort>(sp => sp.GetRequiredService<PrivilegeEfCoreAdapter>());
            services.AddScoped<ISavePrivilegePort>(sp => sp.GetRequiredService<PrivilegeEfCoreAdapter>());
        }
        return services;
    }

    // ── 領域事件派發 + 處理者 + Outbox ───────────────────────────────────
    private static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<
            IDomainEventHandler<TransferPrivilegeUsedEvent>,
            TransferPrivilegeUsedLoggingHandler>();

        // Outbox：可靠地將已持久化的領域事件派發出去
        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<OutboxBackgroundService>();
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

    // ── 可觀測性：Health Checks + OpenTelemetry（Metrics/Tracing）+ Prometheus ──
    private static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<BankDbContext>("database");

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: "BankAccountQuery",
                serviceVersion: "1.0.0"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter())
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation());

        return services;
    }

    // ── Swagger / OpenAPI 文件（含 JWT Bearer 設定）──────────────────────
    private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Banking Account Query API",
                Version = "v1",
                Description = "DDD · Hexagonal · CQRS 銀行帳戶查詢與優惠服務（教學）"
            });

            var jwtScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "輸入 JWT（不含 'Bearer ' 前綴）",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            options.AddSecurityDefinition("Bearer", jwtScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [jwtScheme] = Array.Empty<string>()
            });
        });

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
