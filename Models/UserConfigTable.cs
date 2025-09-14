// Models/NewTable.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mhrc.Models
{

    [Table("Settings")] // Имя таблицы
    public class UserSettings
    {
        [Key]
        public int Id { get; set; }

        public string LanguageCode { get; set; } = "en"; // ru, en, es, etc.

        public bool UseFarenheit { get; set; } = false;

        public bool Use12HourFormat { get; set; } = false;

        public string TimeZoneId { get; set; } // Например: "Europe/Moscow"
    }
}