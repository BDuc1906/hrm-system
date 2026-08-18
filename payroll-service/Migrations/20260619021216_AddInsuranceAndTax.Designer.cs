using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using payroll_service.Data;

#nullable disable

namespace payroll_service.Migrations
{
    [DbContext(typeof(PayrollDbContext))]
    [Migration("20260625000000_AddContractAndTaxFields")]
    public partial class AddContractAndTaxFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pay_period", table: "payrolls",
                type: "VARCHAR(7)", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_code", table: "payrolls",
                type: "VARCHAR(20)", nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "contract_basic_salary", table: "payrolls",
                type: "DECIMAL(12,2)", nullable: false, defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "salary_ratio", table: "payrolls",
                type: "DECIMAL(5,4)", nullable: false, defaultValue: 1.0m);

            migrationBuilder.AddColumn<string>(
                name: "tax_type", table: "payrolls",
                type: "VARCHAR(20)", nullable: false, defaultValue: "Progressive");

            migrationBuilder.AddColumn<bool>(
                name: "is_social_insurance_subject", table: "payrolls",
                type: "TINYINT(1)", nullable: false, defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "standard_working_days", table: "payrolls",
                type: "INT", nullable: false, defaultValue: 26);

            migrationBuilder.AddColumn<int>(
                name: "unpaid_leave_days", table: "payrolls",
                type: "INT", nullable: false, defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "personal_deduction", table: "payrolls",
                type: "DECIMAL(12,2)", nullable: false, defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_pay_period",
                table: "payrolls", column: "pay_period");

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_employee_pay_period",
                table: "payrolls",
                columns: new[] { "employee_id", "pay_period" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_payrolls_pay_period", table: "payrolls");
            migrationBuilder.DropIndex(name: "IX_payrolls_employee_pay_period", table: "payrolls");
            migrationBuilder.DropColumn(name: "pay_period", table: "payrolls");
            migrationBuilder.DropColumn(name: "tax_code", table: "payrolls");
            migrationBuilder.DropColumn(name: "contract_basic_salary", table: "payrolls");
            migrationBuilder.DropColumn(name: "salary_ratio", table: "payrolls");
            migrationBuilder.DropColumn(name: "tax_type", table: "payrolls");
            migrationBuilder.DropColumn(name: "is_social_insurance_subject", table: "payrolls");
            migrationBuilder.DropColumn(name: "standard_working_days", table: "payrolls");
            migrationBuilder.DropColumn(name: "unpaid_leave_days", table: "payrolls");
            migrationBuilder.DropColumn(name: "personal_deduction", table: "payrolls");
        }
    }
}
