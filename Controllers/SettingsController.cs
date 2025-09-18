using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using mhrc.Data;
using mhrc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace mhrc;

public interface IUserSettingsService
{
    Task<UserSettings> GetUserSettingsAsync(string userId);
    Task UpdateUserSettingsAsync(string userId, UserSettings newSettings);
    Task<UserSettings> GetCurrentUserSettingsAsync();
}

public class UserSettingsService : IUserSettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserSettingsService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<UserSettings> GetUserSettingsAsync(string userId)
    {
        return await _context.UserSettings
            .FirstOrDefaultAsync(us => us.UserId == userId)
            ?? await CreateDefaultSettingsAsync(userId);
    }

    public async Task<UserSettings> GetCurrentUserSettingsAsync()
    {
        var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await GetUserSettingsAsync(userId);
    }

    public async Task UpdateUserSettingsAsync(string userId, UserSettings newSettings)
    {
        var existingSettings = await GetUserSettingsAsync(userId);

        existingSettings.TimeZone = newSettings.TimeZone;
        existingSettings.Language = newSettings.Language;
        existingSettings.Is24HourFormat = newSettings.Is24HourFormat;
        existingSettings.TemperatureUnit = newSettings.TemperatureUnit;

        _context.UserSettings.Update(existingSettings);
        await _context.SaveChangesAsync();
    }

    private async Task<UserSettings> CreateDefaultSettingsAsync(string userId)
    {
        var defaultSettings = new UserSettings
        {
            UserId = userId,
            TimeZone = "UTC",
            Language = "ru",
            Is24HourFormat = true,
            TemperatureUnit = "celsius"
        };

        _context.UserSettings.Add(defaultSettings);
        await _context.SaveChangesAsync();

        return defaultSettings;
    }
}


[ApiController]
[Route("settings")]

[Authorize]
public class SettingsController : Controller
{
    private readonly IUserSettingsService _settingsService;

    public SettingsController(IUserSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var settings = await _settingsService.GetUserSettingsAsync(userId);
        
        var model = new UserSettingsViewModel
        {
            TimeZone = settings.TimeZone,
            Language = settings.Language,
            Is24HourFormat = settings.Is24HourFormat,
            TemperatureUnit = settings.TemperatureUnit,
            AvailableTimeZones = GetTimeZones(),
            AvailableLanguages = GetLanguages(),
            AvailableTemperatureUnits = GetTemperatureUnits()
        };
        
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Update(UserSettingsViewModel model)
    {
        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var settings = new UserSettings
            {
                TimeZone = model.TimeZone,
                Language = model.Language,
                Is24HourFormat = model.Is24HourFormat,
                TemperatureUnit = model.TemperatureUnit
            };
            
            await _settingsService.UpdateUserSettingsAsync(userId, settings);
            
            TempData["SuccessMessage"] = "Настройки успешно сохранены";
            return RedirectToAction("Index");
        }
        
        // Заполняем списки для выбора при ошибке валидации
        model.AvailableTimeZones = GetTimeZones();
        model.AvailableLanguages = GetLanguages();
        model.AvailableTemperatureUnits = GetTemperatureUnits();
        
        return View("Index", model);
    }

    private List<SelectListItem> GetTimeZones()
    {
        return TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => new SelectListItem 
            { 
                Value = tz.Id, 
                Text = tz.DisplayName 
            })
            .ToList();
    }

    private List<SelectListItem> GetLanguages()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = "ru", Text = "Русский" },
            new SelectListItem { Value = "en", Text = "English" },
            new SelectListItem { Value = "es", Text = "Español" }
        };
    }

    private List<SelectListItem> GetTemperatureUnits()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = "celsius", Text = "Цельсий (°C)" },
            new SelectListItem { Value = "fahrenheit", Text = "Фаренгейт (°F)" }
        };
    }
}