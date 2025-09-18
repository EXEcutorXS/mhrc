using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class UserSettingsViewModel
{
    [Required]
    [Display(Name = "Часовой пояс")]
    public string TimeZone { get; set; }

    [Required]
    [Display(Name = "Язык интерфейса")]
    public string Language { get; set; }

    [Display(Name = "24-часовой формат времени")]
    public bool Is24HourFormat { get; set; }

    [Required]
    [Display(Name = "Единицы измерения температуры")]
    public string TemperatureUnit { get; set; }

    // Списки для выбора
    public List<SelectListItem> AvailableTimeZones { get; set; }
    public List<SelectListItem> AvailableLanguages { get; set; }
    public List<SelectListItem> AvailableTemperatureUnits { get; set; }
}