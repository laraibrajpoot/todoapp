using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace todolist.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdAndRequestedByToTaskDeletionRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestedBy",
                table: "TaskDeletionRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "TaskDeletionRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedBy",
                table: "TaskDeletionRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TaskDeletionRequests");
        }
    }
}
