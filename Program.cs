using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using mhrc.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Конфигурация JWT ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
})
.AddCookie(); // Сохраняем поддержку cookie-based аутентификации

// --- Остальной код остается без изменений ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/";
    o.AccessDeniedPath = "/";
    o.SlidingExpiration = true;
    o.ExpireTimeSpan = TimeSpan.FromDays(7);
});

builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Mosquito Auth API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Mosquito Auth API Documentation";
    });
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// --- API endpoints ---

// Регистрация (без изменений)
app.MapPost("/register", async (UserManager<IdentityUser> userManager, RegisterDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest(new { error = "User name,Email & Password are necessary." });

    var exists = await userManager.FindByNameAsync(dto.Username);
    if (exists is not null)
        return Results.BadRequest(new { error = "This Username is already taken" });

    exists = await userManager.FindByEmailAsync(dto.Email);
    if (exists is not null)
        return Results.BadRequest(new { error = "This Email is already taken" });

    var user = new IdentityUser { UserName = dto.Username, Email = dto.Email };
    var result = await userManager.CreateAsync(user, dto.Password);

    if (!result.Succeeded)
        return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });

    return Results.Ok(new { message = "Registration is successful" });
});

// Логин с возвратом JWT токена
app.MapPost("/login", async (SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager, IConfiguration config, LoginDto dto) =>
{
    var tokenCheck = dto.Password.Split('.') > 2 && dto.Password.Length > 20;
        var result = await signInManager.PasswordSignInAsync(
        dto.Username, dto.Password, dto.RememberMe, lockoutOnFailure: false);

    if (!result.Succeeded)
        return Results.BadRequest(new { error = "Wrong login data" });

    // Генерация JWT токена
    var user = await userManager.FindByNameAsync(dto.Username);
    var token = GenerateJwtToken(user, config);

    return Results.Ok(new
    {
        message = "Login success",
        token = token,
        expires = DateTime.UtcNow.AddHours(1)
    });
});

// Логин только для получения токена (без cookie)
app.MapPost("/login/token", async (SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager, IConfiguration config, LoginDto dto) =>
{
    var user = await userManager.FindByNameAsync(dto.Username);
    if (user == null)
        return Results.BadRequest(new { error = "Wrong login data" });

    var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, false);

    if (!result.Succeeded)
        return Results.BadRequest(new { error = "Wrong login data" });

    var token = GenerateJwtToken(user, config);

    return Results.Ok(new TokenResponse(token, DateTime.UtcNow.AddDays(1))
        );
});

// Текущий пользователь (работает с JWT и cookie)
app.MapGet("/me", [Authorize] async (UserManager<IdentityUser> userManager, HttpContext http) =>
{
    var user = await userManager.GetUserAsync(http.User);
    return user is null
        ? Results.Unauthorized()
        : Results.Ok(new { user.Email, user.UserName, user.Id });
});

// Логаут (только для cookie)
app.MapPost("/logout", [Authorize(AuthenticationSchemes = "Identity.Application")]
async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok(new { message = "You logged out" });
});

// Проверка JWT токена
app.MapGet("/validate", [Authorize] () =>
{
    return Results.Ok(new { message = "Token is valid" });
});

app.Run();

// Метод для генерации JWT токена
string GenerateJwtToken(IdentityUser user, IConfiguration config)
{
    var jwtSettings = config.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!)
        }),
        Expires = DateTime.UtcNow.AddDays(1),
        Issuer = jwtSettings["Issuer"],
        Audience = jwtSettings["Audience"],
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}