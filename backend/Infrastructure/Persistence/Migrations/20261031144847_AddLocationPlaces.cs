using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LocationBackfillAttemptedAt",
                table: "Solicitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Latitude",
                table: "Solicitations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Longitude",
                table: "Solicitations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location_PlaceId",
                table: "Solicitations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Location_VerifiedAtUtc",
                table: "Solicitations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationBackfillAttemptedAt",
                table: "Solicitations");

            migrationBuilder.DropColumn(
                name: "Location_Latitude",
                table: "Solicitations");

            migrationBuilder.DropColumn(
                name: "Location_Longitude",
                table: "Solicitations");

            migrationBuilder.DropColumn(
                name: "Location_PlaceId",
                table: "Solicitations");

            migrationBuilder.DropColumn(
                name: "Location_VerifiedAtUtc",
                table: "Solicitations");
        }
    }
}
