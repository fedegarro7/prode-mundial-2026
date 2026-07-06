using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prode.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWentToExtraTimeToMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WentToExtraTime",
                table: "Matches",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WentToExtraTime",
                table: "Matches");
        }
    }
}
