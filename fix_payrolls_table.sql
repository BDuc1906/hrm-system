-- Chạy script này để thêm các cột còn thiếu vào bảng `payrolls`
-- (tương ứng với 3 migration: AddHrFields, AddContractAndTaxFields,
-- AddPayrollStatusAndEmployeeStatus mà EF Core không tự áp dụng được)

USE payrolldb;

-- Từ AddHrFields
ALTER TABLE payrolls ADD COLUMN employee_code VARCHAR(50) NULL;
ALTER TABLE payrolls ADD COLUMN full_name NVARCHAR(255) NULL;
ALTER TABLE payrolls ADD COLUMN department_name NVARCHAR(255) NULL;

-- Từ AddContractAndTaxFields
ALTER TABLE payrolls ADD COLUMN pay_period VARCHAR(7) NULL;
ALTER TABLE payrolls ADD COLUMN tax_code VARCHAR(20) NULL;
ALTER TABLE payrolls ADD COLUMN contract_basic_salary DECIMAL(12,2) NOT NULL DEFAULT 0;
ALTER TABLE payrolls ADD COLUMN salary_ratio DECIMAL(5,4) NOT NULL DEFAULT 1.0;
ALTER TABLE payrolls ADD COLUMN tax_type VARCHAR(20) NOT NULL DEFAULT 'Progressive';
ALTER TABLE payrolls ADD COLUMN is_social_insurance_subject TINYINT(1) NOT NULL DEFAULT 1;
ALTER TABLE payrolls ADD COLUMN standard_working_days INT NOT NULL DEFAULT 26;
ALTER TABLE payrolls ADD COLUMN unpaid_leave_days INT NOT NULL DEFAULT 0;
ALTER TABLE payrolls ADD COLUMN personal_deduction DECIMAL(12,2) NOT NULL DEFAULT 0;
ALTER TABLE payrolls ADD INDEX IX_payrolls_pay_period (pay_period);
ALTER TABLE payrolls ADD INDEX IX_payrolls_employee_pay_period (employee_id, pay_period);

-- Từ AddPayrollStatusAndEmployeeStatus
ALTER TABLE payrolls ADD COLUMN payroll_status VARCHAR(20) NOT NULL DEFAULT 'Draft';
ALTER TABLE payrolls ADD COLUMN approved_at DATETIME NULL;
ALTER TABLE payrolls ADD COLUMN employee_status VARCHAR(20) NULL;

-- Đánh dấu 3 migration này là "đã áp dụng" để EF Core không cố chạy lại
-- (chạy lại sẽ báo lỗi "column already exists")
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES
  ('202606110350_AddHrFields', '8.0.0'),
  ('20260625000000_AddContractAndTaxFields', '8.0.0'),
  ('20260627000000_AddPayrollStatusAndEmployeeStatus', '8.0.0');
