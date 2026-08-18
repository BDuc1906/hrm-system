using payroll_service.DTOs;

namespace payroll_service.Services
{
    public interface IPayrollService
    {
        Task<List<PayrollDTO>> GetAllPayrollsAsync();
        Task<PayrollDTO> GetPayrollByIdAsync(int id);
        Task<PayslipDTO> GetPayslipAsync(int id);
        Task<PayrollDTO> CreatePayrollAsync(PayrollCreateDTO dto);
        Task<PayrollDTO> UpdatePayrollAsync(int id, PayrollUpdateDTO dto);
        Task<bool> DeletePayrollAsync(int id);
        Task<PayrollReportDTO> GetPayrollReportAsync();

        /// <summary>Duyệt bảng lương và gửi email payslip cho nhân viên</summary>
        Task<ApprovePayrollResult> ApproveAndSendPayslipAsync(ApprovePayrollRequest request);
    }


}