using mhrc.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core + SQLite ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// --- Полный Identity (пользователи + роли + куки) ---
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // политика паролей (упрощена для примера)
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// --- Настройка куки ---
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Mosquito Auth API v1");
        options.RoutePrefix = "swagger"; // Доступ по /swagger
        options.DocumentTitle = "Mosquito Auth API Documentation";
    });
}

// --- Middleware ---
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// --- API endpoints ---

// Регистрация
app.MapPost("/register", async (UserManager<IdentityUser> userManager, RegisterDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest(new { error = "User name,Email & Password are neccessary." });

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

// Логин
app.MapPost("/login", async (SignInManager<IdentityUser> signInManager, LoginDto dto) =>
{
    
    var result = await signInManager.PasswordSignInAsync(
        dto.Username, dto.Password, dto.RememberMe, lockoutOnFailure: false);

    if (!result.Succeeded)
        return Results.BadRequest(new { error = "Wrong login data" });

    return Results.Ok(new { message = "Login success" });
});

// Текущий пользователь (требует авторизации)
app.MapGet("/me", [Authorize] async (UserManager<IdentityUser> userManager, HttpContext http) =>
{
    var user = await userManager.GetUserAsync(http.User);
    return user is null
        ? Results.Unauthorized()
        : Results.Ok(new { user.Email, user.UserName, user.Id });
});

// Логаут
app.MapPost("/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok(new { message = "You logged out" });
});

app.Run();
