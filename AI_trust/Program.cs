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
 * LOAD ENV
 * ========================= */
builder.Configuration.AddEnvironmentVariables();

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

/* =========================
 * DATABASE (PostgreSQL Railway)
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
        // Local only
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
            .WithOrigins(
                "http://localhost:5173",
                "https://aitrustapi-production.up.railway.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

/* =========================
 * RAILWAY PORT (BẮT BUỘC)
 * ========================= */
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

/* =========================
 * AUTO MIGRATE DB (SAFE)
 * ========================= */
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiTrustContext>();
    db.Database.Migrate();
}

/* =========================
 * MIDDLEWARE
 * ========================= */
app.UseCors("AllowReactApp");

// Swagger bật cho Railway (debug)
app.UseSwagger();
app.UseSwaggerUI();

// Railway đã HTTPS sẵn → KHÔNG redirect nữa
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
