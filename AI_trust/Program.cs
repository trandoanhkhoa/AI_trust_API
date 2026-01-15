using AI_trust.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

/* =========================
 * BASIC SERVICES
 * ========================= */
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

/* =========================
 * ENV
 * ========================= */
builder.Configuration.AddEnvironmentVariables();

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

/* =========================
 * DATABASE
 * ========================= */
builder.Services.AddDbContext<AiTrustContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);

        var connectionString =
            $"Host={uri.Host};" +
            $"Port={uri.Port};" +
            $"Database={uri.AbsolutePath.TrimStart('/')};" +
            $"Username={userInfo[0]};" +
            $"Password={userInfo[1]};" +
            $"Ssl Mode=Require;" +
            $"Trust Server Certificate=true";

        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("PostgresConnection")
        );
    }
});

/* =========================
 * CORS
 * ========================= */
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

/* =========================
 * PORT (RAILWAY BẮT BUỘC)
 * ========================= */
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

/* =========================
 * MIDDLEWARE
 * ========================= */
app.UseCors("AllowReactApp");

app.UseSwagger();
app.UseSwaggerUI();

// 🚫 KHÔNG HTTPS redirect trên Railway
app.UseAuthorization();
app.MapControllers();

app.Run();
