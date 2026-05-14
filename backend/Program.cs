using System.Text;
using CarpoolApp.Server.Data;
using CarpoolApp.Server.Hubs;
using CarpoolApp.Server.Models;
using CarpoolApp.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be supplied from a secret store or environment variable and be at least 32 characters.");
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT Authorization Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CarpoolApp API", Version = "v1" });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Put **_ONLY_** your JWT Bearer token below.",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddSignalR();
builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton<TokenBlacklistService>();

// Database
builder.Services.AddDbContext<CarpoolDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CarpoolDatabase"))
);

// Authentication
builder.Services.AddAuthentication(options =>
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                context.Token = accessToken;

            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var blacklist = context.HttpContext.RequestServices
                .GetRequiredService<TokenBlacklistService>();
            var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (jti != null && blacklist.IsRevoked(jti))
                context.Fail("Token has been revoked.");
            return Task.CompletedTask;
        }
    };
});

// Authorization
builder.Services.AddAuthorization();

// CORS
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Run migrations and seed admin user on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CarpoolDbContext>();
    db.Database.Migrate();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var adminEmail = config["AdminSettings:Email"];
    var adminPassword = config["AdminSettings:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail)
        && !string.IsNullOrWhiteSpace(adminPassword)
        && !db.Users.Any(u => u.UniversityEmail == adminEmail))
    {
        var hasher = new PasswordHasher<User>();
        var adminUser = new User
        {
            FullName = config["AdminSettings:FullName"],
            UniversityEmail = adminEmail,
            PhoneNumber = config["AdminSettings:PhoneNumber"],
            CreatedAt = DateTime.UtcNow
        };
        adminUser.PasswordHash = hasher.HashPassword(adminUser, adminPassword);
        db.Users.Add(adminUser);
        db.SaveChanges();
    }
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Cross-Origin-Resource-Policy", "same-origin");
    await next();
});

// Middlewares
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
