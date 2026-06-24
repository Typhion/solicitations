using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Solicitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location_ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Location_Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location_StreetNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Website_Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Website_Link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Contact_Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Contact_PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Contact_Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Solicitations");
        }
    }
}
