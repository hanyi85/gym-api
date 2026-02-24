using gym_api.Models;
using gym_api.Services;
using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// =======================
// 資料庫設定
// =======================
builder.Services.AddDbContext<dbFitness2Context>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Fitness2Connection")
    );
});

// =======================
//  CORS 設定（給 Vue 用）
// =======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVue",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173" // Vite
                                   )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// =======================
// Controller & JSON 設定
// =======================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 忽略循環參照（EF 關聯很重要）
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // JSON 深度
        options.JsonSerializerOptions.MaxDepth = 64;

        // 保留 C# 屬性命名（不轉 camelCase）
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// =======================
//  Swagger
// =======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "gym-api", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "請輸入 Bearer Token"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// =======================
//  SMTP 寄信
// =======================
builder.Services.AddScoped<EmailService>();
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));

// =======================
//  JwtService
// =======================
builder.Services.AddScoped<JwtService>();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            )
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowVue");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
