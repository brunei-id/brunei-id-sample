using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSC.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IcNo = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });



            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "IcNo", "Name" },
                values: new object[,] {{ 1, "farhan.norazman@itpss.com", "01-072141", "Iman Izzat Farhan Bin Mohd Norazman" }, { 2, "fahmi.osman@itpss.com", "01-034560", "Isyrah Fahmi Osman" }, {3, "safwan.ahman@itpss.com", "01-075963", "Safwan Ahman"} });


         
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

        }
    }
}
