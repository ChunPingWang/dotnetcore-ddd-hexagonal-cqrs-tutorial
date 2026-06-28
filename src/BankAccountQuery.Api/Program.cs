using System.Text.Json;
using BankAccountQuery.Infrastructure.Adapters.In.Web;
using BankAccountQuery.Infrastructure.Adapters.Out.Persistence;
using BankAccountQuery.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 註冊所有銀行查詢服務（DbContext、MediatR、Behaviors、Adapters、JWT、
// 可觀測性、Swagger）
builder.Services.AddBankingServices(builder.Configuration);

// Controllers 定義於 Infrastructure 組件，需註冊其 ApplicationPart
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(AccountController).Assembly);

var app = builder.Build();

// 全域例外處理（IExceptionHandler）
app.UseExceptionHandler();

// Swagger / OpenAPI（教學專案一律開啟）
app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Banking Account Query API v1"));

// 資料庫初始化：關聯式（Postgres）跑 Migration；其餘（InMemory）直接播種
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
    var eventSourcedPrivileges = string.Equals(
        app.Configuration["Privilege:Persistence"], "EventSourced", StringComparison.OrdinalIgnoreCase);
    DatabaseSeeder.Seed(db, eventSourcedPrivileges);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Prometheus 指標抓取端點（/metrics）
app.MapPrometheusScrapingEndpoint();

// Health Check（含資料庫探針），輸出 JSON
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(payload);
    }
}).AllowAnonymous();

app.Run();

// 供整合測試（WebApplicationFactory<Program>）取用
public partial class Program { }
