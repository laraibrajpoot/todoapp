using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace todolist.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaVerifiedToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MfaVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MfaVerified",
                table: "Users");
        }
    }
}
