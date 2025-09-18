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

        // ��������� ����� � AspNetUsers
        builder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(us => us.Id);

            // ������� ���� �� AspNetUsers
            entity.HasIndex(us => us.UserId);

            // ����������� ������������ �� UserId (���� ������������ - ���� ������ ��������)
            entity.HasIndex(us => us.UserId).IsUnique();

            // ��������� �������� ����� (�����������, EF ������ ����������� ���)
            entity.Property(us => us.UserId).IsRequired().HasMaxLength(450);
        });
    }

}
