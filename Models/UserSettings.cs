// Models/NewTable.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mhrc.Models
{

    public class UserSettings
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)] // Длина Id пользователя в Identity
        public string UserId { get; set; }

        [MaxLength(50)]
        public string TimeZone { get; set; } = "UTC";

        [MaxLength(10)]
        public string Language { get; set; } = "ru";

        public bool Is24HourFormat { get; set; } = true;

        [MaxLength(10)]
        public string TemperatureUnit { get; set; } = "celsius";
    }
}