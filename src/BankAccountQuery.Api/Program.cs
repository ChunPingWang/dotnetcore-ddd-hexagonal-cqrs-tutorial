using BankAccountQuery.Infrastructure.Adapters.In.Web;
using BankAccountQuery.Infrastructure.Adapters.Out.Persistence;
using BankAccountQuery.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 註冊所有銀行查詢服務（DbContext、MediatR、Behaviors、Adapters、JWT）
builder.Services.AddBankingServices(builder.Configuration);

// Controllers 定義於 Infrastructure 組件，需註冊其 ApplicationPart
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(AccountController).Assembly);

var app = builder.Build();

// 全域例外處理（IExceptionHandler）
app.UseExceptionHandler();

// 開發 / 測試環境播種範例資料
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    DatabaseSeeder.Seed(db);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
   .AllowAnonymous();

app.Run();

// 供整合測試（WebApplicationFactory<Program>）取用
public partial class Program { }
