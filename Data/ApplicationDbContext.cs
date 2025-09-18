using mhrc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace mhrc.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
        
    public DbSet<UserSettings> UserSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Настройка связи с AspNetUsers
        builder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(us => us.Id);

            // Внешний ключ на AspNetUsers
            entity.HasIndex(us => us.UserId);

            // Ограничение уникальности на UserId (один пользователь - одна запись настроек)
            entity.HasIndex(us => us.UserId).IsUnique();

            // Настройка внешнего ключа (опционально, EF обычно справляется сам)
            entity.Property(us => us.UserId).IsRequired().HasMaxLength(450);
        });
    }

}
