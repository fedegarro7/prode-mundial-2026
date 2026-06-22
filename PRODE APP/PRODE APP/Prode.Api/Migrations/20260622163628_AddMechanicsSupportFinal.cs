using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prode.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMechanicsSupportFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BasePointsEarned",
                table: "Predictions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CaptainBonusPoints",
                table: "Predictions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MultiplierBonusPoints",
                table: "Predictions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "WasDecidedByPenalties",
                table: "Matches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BombMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    RoundKey = table.Column<string>(type: "text", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BombMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BombMatches_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaptainPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaptainPicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaptainPicks_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaptainPicks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoldenGoalPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    RoundKey = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldenGoalPicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoldenGoalPicks_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoldenGoalPicks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OraclePredictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundKey = table.Column<string>(type: "text", nullable: false),
                    DrawsAfterNinetyPrediction = table.Column<int>(type: "integer", nullable: false),
                    PenaltyShootoutsPrediction = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OraclePredictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OraclePredictions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoundAwards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundKey = table.Column<string>(type: "text", nullable: false),
                    AwardType = table.Column<string>(type: "text", nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundAwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoundAwards_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharpShooterPredictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    RoundKey = table.Column<string>(type: "text", nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharpShooterPredictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharpShooterPredictions_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharpShooterPredictions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BombMatches_MatchId",
                table: "BombMatches",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BombMatches_RoundKey",
                table: "BombMatches",
                column: "RoundKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaptainPicks_TeamId",
                table: "CaptainPicks",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CaptainPicks_UserId",
                table: "CaptainPicks",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoldenGoalPicks_MatchId",
                table: "GoldenGoalPicks",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldenGoalPicks_UserId_RoundKey",
                table: "GoldenGoalPicks",
                columns: new[] { "UserId", "RoundKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OraclePredictions_UserId_RoundKey",
                table: "OraclePredictions",
                columns: new[] { "UserId", "RoundKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoundAwards_UserId",
                table: "RoundAwards",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharpShooterPredictions_MatchId",
                table: "SharpShooterPredictions",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SharpShooterPredictions_UserId_RoundKey",
                table: "SharpShooterPredictions",
                columns: new[] { "UserId", "RoundKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BombMatches");

            migrationBuilder.DropTable(
                name: "CaptainPicks");

            migrationBuilder.DropTable(
                name: "GoldenGoalPicks");

            migrationBuilder.DropTable(
                name: "OraclePredictions");

            migrationBuilder.DropTable(
                name: "RoundAwards");

            migrationBuilder.DropTable(
                name: "SharpShooterPredictions");

            migrationBuilder.DropColumn(
                name: "BasePointsEarned",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "CaptainBonusPoints",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "MultiplierBonusPoints",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "WasDecidedByPenalties",
                table: "Matches");
        }
    }
}
