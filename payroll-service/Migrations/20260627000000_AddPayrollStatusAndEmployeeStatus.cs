using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace payroll_service.Migrations
{
    [Migration("20260627000000_AddPayrollStatusAndEmployeeStatus")]
    public partial class AddPayrollStatusAndEmployeeStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payroll_status",
                table: "payrolls",
                type: "VARCHAR(20)",
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                table: "payrolls",
                type: "DATETIME",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employee_status",
                table: "payrolls",
                type: "VARCHAR(20)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "payroll_status", table: "payrolls");
            migrationBuilder.DropColumn(name: "approved_at", table: "payrolls");
            migrationBuilder.DropColumn(name: "employee_status", table: "payrolls");
        }
    }
}
