using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mhrc.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    TimeZone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Is24HourFormat = table.Column<bool>(type: "INTEGER", nullable: false),
                    TemperatureUnit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");
        }
    }
}
