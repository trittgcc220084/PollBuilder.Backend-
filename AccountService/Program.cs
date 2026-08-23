using AccountService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Chuỗi Secret Key dùng chung đồng bộ để mã hóa JWT Token
string jwtSecretKey = "MotDoanMaBaoMatRatDaiVaKhoDoanChoPollBuilder123!@#";

// 2. Cấu hình Database PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Cấu hình xác thực JWT Token
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. Cấu hình chính sách CORS để Frontend gọi API không bị chặn
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

// 5. Tự động tạo cấu trúc bảng trong Database (Tối ưu hóa an toàn cho Cloud Neon Pooler)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
        // EnsureCreated sẽ kiểm tra nhẹ nhàng và tạo bảng nếu chưa có, không gây xung đột với cổng Pooler
        db.Database.EnsureCreated();
        Console.WriteLine("✅ Database check system successful.");
    }
    catch (Exception ex)
    {
        // Ghi log để theo dõi nhưng không làm sập tiến trình khởi động của Web Service
        Console.WriteLine($"⚠️ Database structure initialization warning: {ex.Message}");
    }
}

// Kích hoạt Middleware
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// 6. Các cổng kiểm tra trạng thái hoạt động (Health checks) của Render
app.MapGet("/", () => Results.Ok("AccountService is running smoothly"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapControllers();

// Cấu hình cổng Port linh hoạt theo môi trường Render Container
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");
