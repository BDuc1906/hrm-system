using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace payroll_service.Migrations
{
    [Migration("202606110350_AddHrFields")]
    public partial class AddHrFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "employee_code",
                table: "payrolls",
                type: "VARCHAR(50)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "full_name",
                table: "payrolls",
                type: "NVARCHAR(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "department_name",
                table: "payrolls",
                type: "NVARCHAR(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "employee_code", table: "payrolls");
            migrationBuilder.DropColumn(name: "full_name", table: "payrolls");
            migrationBuilder.DropColumn(name: "department_name", table: "payrolls");
        }
    }
}
