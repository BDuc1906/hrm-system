using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using payroll_service.DTOs;
using payroll_service.Repositories;
using payroll_service.Services;
using System.Security.Claims;
using System.Text.Json;

namespace payroll_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PayrollsController : ControllerBase
    {
        private readonly IPayrollService _payrollService;
        private readonly IHrServiceClient _hrClient;
        private readonly IAttendanceServiceClient _attendanceClient;
        private readonly IPayrollRepository _repository;
        private readonly ILogger<PayrollsController> _logger;

        public PayrollsController(
            IPayrollService payrollService,
            IHrServiceClient hrClient,
            IAttendanceServiceClient attendanceClient,
            IPayrollRepository repository,
            ILogger<PayrollsController> logger)
        {
            _payrollService = payrollService;
            _hrClient = hrClient;
            _attendanceClient = attendanceClient;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>Lấy role và employee ID từ JWT token</summary>
        private (string? role, string? employeeId) GetUserInfo()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ??
                      User.FindFirst("role")?.Value;

            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                            User.FindFirst("sub")?.Value ??
                            User.FindFirst("employeeId")?.Value;

            return (role, employeeId);
        }

        /// <summary>Lấy tên phòng ban của Manager hiện tại (để lọc dữ liệu theo phòng ban)</summary>
        private async Task<string?> GetManagerDepartmentAsync(string? employeeId)
        {
            if (string.IsNullOrEmpty(employeeId)) return null;
            var emp = await _hrClient.GetEmployeeExtendedAsync(employeeId);
            return emp?.DepartmentName;
        }

        /// <summary>
        /// Kiểm tra quyền xem lương.
        /// FIX: trước đây chỉ Admin mới xem được payroll của người khác, HR và
        /// Manager bị coi như Employee (chỉ xem của chính mình) dù frontend đã
        /// cho phép HR/Manager vào các trang quản lý/báo cáo lương → HR/Manager
        /// vào trang nhưng luôn bị 403 hoặc chỉ thấy đúng 1 dòng của bản thân.
        /// Giờ: Admin/HR xem toàn công ty, Manager xem phòng ban mình quản lý,
        /// Employee chỉ xem của chính mình.
        /// </summary>
        private async Task<bool> CanViewPayrollAsync(string payrollEmployeeId, string? payrollDepartment)
        {
            var (role, employeeId) = GetUserInfo();

            if (role == "Admin" || role == "HR")
                return true;

            if (role == "Manager")
            {
                var deptName = await GetManagerDepartmentAsync(employeeId);
                return !string.IsNullOrEmpty(deptName) && deptName == payrollDepartment;
            }

            return employeeId == payrollEmployeeId;
        }

        /// <summary>
        /// Lấy danh sách bảng lương theo phạm vi role:
        /// Admin/HR = toàn công ty, Manager = phòng ban mình quản lý, Employee = của chính mình.
        /// FIX: trước đây chỉ Admin xem được toàn bộ, mọi role khác (kể cả HR, Manager —
        /// dù frontend cho phép họ vào trang "Bảng lương"/"Báo cáo lương") đều bị coi như
        /// Employee, chỉ nhận về đúng 1 dòng của chính họ → trang quản lý/báo cáo lương
        /// của HR, Manager luôn trống hoặc sai dữ liệu.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PayrollDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PayrollDTO>>> GetAllPayrolls(
            [FromQuery] string? payPeriod = null)
        {
            try
            {
                var (role, employeeId) = GetUserInfo();

                var all = await _payrollService.GetAllPayrollsAsync();
                if (!string.IsNullOrEmpty(payPeriod))
                    all = all.Where(p => p.PayPeriod == payPeriod).ToList();

                // Admin + HR: xem toàn công ty
                if (role == "Admin" || role == "HR")
                {
                    _logger.LogInformation("{Role} lấy danh sách tất cả bảng lương", role);
                    return Ok(all);
                }

                // Manager: chỉ xem bảng lương của phòng ban mình quản lý
                if (role == "Manager")
                {
                    var deptName = await GetManagerDepartmentAsync(employeeId);
                    var deptPayrolls = string.IsNullOrEmpty(deptName)
                        ? new List<PayrollDTO>()
                        : all.Where(p => p.DepartmentName == deptName).ToList();

                    _logger.LogInformation(
                        "Manager (phòng '{Dept}') lấy {Count} bản ghi lương",
                        deptName ?? "(không xác định)", deptPayrolls.Count);
                    return Ok(deptPayrolls);
                }

                // Employee: chỉ xem của chính mình
                if (string.IsNullOrEmpty(employeeId))
                {
                    _logger.LogWarning("Employee không có employeeId trong token");
                    return Unauthorized(new { message = "Không tìm thấy mã nhân viên trong token" });
                }

                _logger.LogInformation("Employee {EmployeeId} lấy bảng lương của mình", employeeId);
                var myPayrolls = all.Where(p => p.EmployeeId == employeeId).ToList();
                return Ok(myPayrolls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách bảng lương");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }

        /// <summary>Lấy bảng lương theo ID (kiểm tra quyền)</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PayrollDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PayrollDTO>> GetPayrollById(int id)
        {
            try
            {
                _logger.LogInformation("Lấy bảng lương với id: {Id}", id);
                var payroll = await _payrollService.GetPayrollByIdAsync(id);

                if (!await CanViewPayrollAsync(payroll.EmployeeId, payroll.DepartmentName))
                {
                    _logger.LogWarning("Từ chối truy cập: nhân viên {UserId} cố xem lương của {EmployeeId}",
                        GetUserInfo().employeeId, payroll.EmployeeId);
                    return Forbid();
                }

                return Ok(payroll);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy bảng lương");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }

        /// <summary>Lấy phiếu lương chi tiết theo ID</summary>
        [HttpGet("{id}/payslip")]
        [ProducesResponseType(typeof(PayslipDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PayslipDTO>> GetPayslip(int id)
        {
            try
            {
                _logger.LogInformation("Lấy phiếu lương với id: {Id}", id);
                var payslip = await _payrollService.GetPayslipAsync(id);

                if (!await CanViewPayrollAsync(payslip.EmployeeId, payslip.DepartmentName))
                {
                    _logger.LogWarning("Từ chối truy cập phiếu lương: nhân viên {UserId} cố xem của {EmployeeId}",
                        GetUserInfo().employeeId, payslip.EmployeeId);
                    return Forbid();
                }

                return Ok(payslip);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy phiếu lương");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }

        /// <summary>Lấy lương của nhân viên hiện tại</summary>
        [HttpGet("my-payroll")]
        [ProducesResponseType(typeof(List<PayrollDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<PayrollDTO>>> GetMyPayroll(
            [FromQuery] string? payPeriod = null)
        {
            try
            {
                var (_, employeeId) = GetUserInfo();

                if (string.IsNullOrEmpty(employeeId))
                {
                    _logger.LogWarning("Không tìm thấy employeeId trong token");
                    return Unauthorized(new { message = "Không tìm thấy mã nhân viên trong token" });
                }

                _logger.LogInformation("Nhân viên {EmployeeId} lấy lương của mình", employeeId);
                var allPayrolls = await _payrollService.GetAllPayrollsAsync();
                var myPayrolls = allPayrolls.Where(p => p.EmployeeId == employeeId);
                if (!string.IsNullOrEmpty(payPeriod))
                    myPayrolls = myPayrolls.Where(p => p.PayPeriod == payPeriod);
                return Ok(myPayrolls.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lương của nhân viên");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }

        /// <summary>Tạo bảng lương mới (chỉ Admin)</summary>
        [HttpPost]
        [Authorize(Roles = "Admin, KT")]
        [ProducesResponseType(typeof(PayrollDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PayrollDTO>> CreatePayroll([FromBody] PayrollCreateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _logger.LogInformation("Tạo bảng lương mới");
                var payroll = await _payrollService.CreatePayrollAsync(dto);
                return CreatedAtAction(nameof(GetPayrollById), new { id = payroll.Id }, payroll);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo bảng lương");
                var baseEx = ex.GetBaseException();
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Lỗi máy chủ",
                    error = baseEx.Message,
                    detail = ex.ToString()
                });
            }
        }

        /// <summary>Cập nhật bảng lương (chỉ Admin)</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, KT")]
        [ProducesResponseType(typeof(PayrollDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PayrollDTO>> UpdatePayroll(int id, [FromBody] PayrollUpdateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _logger.LogInformation("Admin cập nhật bảng lương id: {Id}", id);
                var payroll = await _payrollService.UpdatePayrollAsync(id, dto);
                return Ok(payroll);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật bảng lương");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }

        /// <summary>Xóa bảng lương (chỉ Admin)</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, KT")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeletePayroll(int id)
        {
            try
            {
                _logger.LogInformation("Admin xóa bảng lương id: {Id}", id);
                var result = await _payrollService.DeletePayrollAsync(id);

                if (!result)
                    return NotFound(new { message = $"Không tìm thấy bảng lương với id {id}" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa bảng lương");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }

        /// <summary>Lấy báo cáo tổng lương (chỉ Admin)</summary>
        [HttpGet("report/summary")]
        [Authorize(Roles = "Admin, KT")]
        [ProducesResponseType(typeof(PayrollReportDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PayrollReportDTO>> GetPayrollReport()
        {
            try
            {
                _logger.LogInformation("Admin lấy báo cáo tổng lương");
                var report = await _payrollService.GetPayrollReportAsync();
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy báo cáo");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }

        /// <summary>Duyệt bảng lương và gửi phiếu lương qua email cho nhân viên</summary>
        [HttpPost("approve")]
        [Authorize(Roles = "Admin, KT")]
        [ProducesResponseType(typeof(ApprovePayrollResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveAndSendPayslips([FromBody] ApprovePayrollRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.PayPeriod))
                return BadRequest(new { error = "PayPeriod is required (format: yyyy-MM)" });

            try
            {
                _logger.LogInformation("[Payroll] Admin duyệt bảng lương kỳ {Period}, sendEmail={Send}",
                    request.PayPeriod, request.SendEmail);

                var result = await _payrollService.ApproveAndSendPayslipAsync(request);

                _logger.LogInformation(
                    "[Payroll] Duyệt xong: {Approved} approved, {Sent} email sent, {Failed} failed",
                    result.TotalApproved, result.EmailSent, result.EmailFailed);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Payroll] Lỗi khi duyệt bảng lương");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Lỗi máy chủ", error = ex.Message });
            }
        }

        /// <summary>
        /// Tính lương tất cả nhân viên và lưu vào database (POST)
        /// Nâng cấp: tích hợp Contract + xử lý nhân viên Inactive nghỉ giữa tháng
        /// </summary>
        [HttpPost("calculate-all")]
        [Authorize(Roles = "Admin, KT")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CalculateAndSaveAllPayrolls([FromBody] CalculatePayrollRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Month))
                return BadRequest(new { error = "Month is required (format: yyyy-MM)" });

            var month = request.Month;

            try
            {
                _logger.LogInformation("[Payroll] Admin tính lương tất cả nhân viên cho tháng: {Month}", month);

                // ============================================================
                // BƯỚC 1: Lấy danh sách tất cả nhân viên từ HR Service
                // ============================================================
                var hrJson = await _hrClient.GetEmployeesAsync();
                var employees = new List<HrEmployeeDTO>();

                try
                {
                    if (string.IsNullOrWhiteSpace(hrJson))
                        throw new Exception("Empty response from HR service");

                    using var doc = JsonDocument.Parse(hrJson);
                    var root = doc.RootElement;

                    JsonElement? arrayElem = null;

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        arrayElem = root;
                    }
                    else if (root.TryGetProperty("data", out var d1))
                    {
                        if (d1.ValueKind == JsonValueKind.Array)
                            arrayElem = d1;
                        else if (d1.ValueKind == JsonValueKind.Object &&
                                 d1.TryGetProperty("data", out var d2) &&
                                 d2.ValueKind == JsonValueKind.Array)
                            arrayElem = d2;
                    }
                    else if (root.TryGetProperty("employees", out var empProp) &&
                             empProp.ValueKind == JsonValueKind.Array)
                    {
                        arrayElem = empProp;
                    }

                    if (arrayElem.HasValue)
                    {
                        foreach (var item in arrayElem.Value.EnumerateArray())
                        {
                            var emp = new HrEmployeeDTO
                            {
                                EmployeeId = item.TryGetProperty("id", out var id) ? id.GetString() : null,
                                EmployeeCode = item.TryGetProperty("employeeCode", out var ec) ? ec.GetString() : null,
                                FullName = item.TryGetProperty("fullName", out var fn) ? fn.GetString() : null,
                                DepartmentName = item.TryGetProperty("departmentName", out var dn) ? dn.GetString() : null,
                                TaxCode = item.TryGetProperty("taxCode", out var tc) ? tc.GetString() : null,
                                DependentsCount = item.TryGetProperty("dependentsCount", out var dc)
                                                  && dc.ValueKind == JsonValueKind.Number ? dc.GetInt32() : 0,
                                BaseSalary = item.TryGetProperty("baseSalary", out var bs)
                                                  && bs.ValueKind == JsonValueKind.Number
                                                  ? bs.GetDecimal() : (decimal?)null
                            };
                            if (!string.IsNullOrEmpty(emp.EmployeeId))
                                employees.Add(emp);
                        }
                    }
                    else
                    {
                        try
                        {
                            var list = System.Text.Json.JsonSerializer.Deserialize<List<HrEmployeeDTO>>(hrJson);
                            if (list != null && list.Any())
                                employees.AddRange(list.Where(e => !string.IsNullOrEmpty(e.EmployeeId)));
                        }
                        catch (Exception exFallback)
                        {
                            _logger.LogWarning(exFallback,
                                "[Payroll] HR response did not match expected shapes. Trying alternate parsing failed.");
                        }
                    }

                    if (!employees.Any())
                        throw new Exception("No employees parsed from HR response");
                }
                catch (Exception ex)
                {
                    var raw = hrJson ?? "(null)";
                    if (raw.Length > 1000) raw = raw.Substring(0, 1000) + "... [truncated]";
                    _logger.LogError(ex,
                        "[Payroll] Lỗi parse danh sách nhân viên từ HR. Raw response (truncated): {HrJson}", raw);
                    return StatusCode(502, new { error = "Không parse được danh sách nhân viên từ HR", detail = ex.Message });
                }

                if (!employees.Any())
                {
                    _logger.LogWarning("[Payroll] Không có nhân viên nào từ HR");
                    return Ok(new { month, totalEmployees = 0, created = 0, updated = 0, payrolls = new List<object>() });
                }

                _logger.LogInformation("[Payroll] Tìm thấy {Count} nhân viên từ HR", employees.Count);

                // ============================================================
                // BƯỚC 2: Với từng nhân viên — lấy Contract + Attendance rồi tính lương
                // Xử lý đặc biệt: nhân viên Inactive nghỉ giữa tháng
                // ============================================================
                var createdCount = 0;
                var updatedCount = 0;
                var payrollResults = new List<object>();

                foreach (var emp in employees)
                {
                    try
                    {
                        _logger.LogInformation("[Payroll] Xử lý nhân viên: {EmployeeId}", emp.EmployeeId);

                        // Gọi song song HR PayrollData (employee + contract + contractType) và Attendance
                        var hrDataTask = _hrClient.GetPayrollDataAsync(emp.EmployeeId!);
                        var attendanceTask = _attendanceClient.GetAttendanceForEmployeeAsync(emp.EmployeeId!, month);
                        await Task.WhenAll(hrDataTask, attendanceTask);

                        var hrData = hrDataTask.Result;
                        var attendance = attendanceTask.Result;

                        // ── XỬ LÝ NHÂN VIÊN INACTIVE ─────────────────────────────
                        string employeeStatus = hrData.Employee?.Status ?? "Active";
                        bool isInactive = employeeStatus.Equals("Inactive", StringComparison.OrdinalIgnoreCase);

                        if (isInactive)
                        {
                            int actualWorkingDays = attendance?.WorkingDays ?? 0;

                            // Inactive xuyên suốt cả tháng, không có ngày công → bỏ qua
                            if (actualWorkingDays == 0)
                            {
                                _logger.LogInformation(
                                    "[Payroll] Bỏ qua nhân viên Inactive không có ngày công: {EmployeeId}",
                                    emp.EmployeeId);
                                payrollResults.Add(new
                                {
                                    employeeId = emp.EmployeeId,
                                    fullName = hrData.Employee?.FullName ?? emp.FullName,
                                    status = "Skipped",
                                    note = "Nhân viên Inactive, không có ngày công trong tháng"
                                });
                                continue;
                            }

                            // Có ngày công → vẫn tính lương cho số ngày đã làm
                            _logger.LogInformation(
                                "[Payroll] Nhân viên Inactive {EmployeeId} có {Days} ngày công → vẫn tính lương",
                                emp.EmployeeId, actualWorkingDays);
                        }

                        // ── TÍNH LƯƠNG (chung cho Active và Inactive có ngày công) ──
                        int standardDays = attendance?.StandardWorkingDays > 0
                            ? attendance.StandardWorkingDays : 26;
                        int workingDays = attendance?.WorkingDays ?? standardDays;
                        int paidLeave = attendance?.PaidLeaveDays ?? 0;
                        int unpaidLeave = attendance?.UnpaidLeaveDays ?? 0;

                        double hourlyRate = hrData.EffectiveSalary > 0
                            ? (double)(hrData.EffectiveSalary / (standardDays * 8)) : 0;

                        double overtimeNormal = attendance?.OvertimeNormalHours ?? 0;
                        double overtimeWeekend = attendance?.OvertimeWeekendHours ?? 0;
                        double overtimeHoliday = attendance?.OvertimeHolidayHours ?? 0;
                        double overtimeTotal = attendance?.OvertimeHours ?? 0;

                        double overtimePayDouble = (overtimeNormal * 1.5 * hourlyRate) +
                                                   (overtimeWeekend * 2.0 * hourlyRate) +
                                                   (overtimeHoliday * 3.0 * hourlyRate);
                        if (overtimePayDouble == 0 && overtimeTotal > 0)
                            overtimePayDouble = overtimeTotal * 1.5 * hourlyRate;

                        decimal overtimePay = (decimal)overtimePayDouble;

                        var payrollCreateDto = new PayrollCreateDTO
                        {
                            EmployeeId = emp.EmployeeId,
                            EmployeeName = hrData.Employee?.FullName ?? emp.FullName ?? emp.EmployeeCode,
                            PayPeriod = month,
                            ContractBasicSalary = hrData.ContractBasicSalary,
                            BaseSalary = hrData.EffectiveSalary,
                            SalaryRatio = hrData.SalaryRatio,
                            TaxType = hrData.TaxType,
                            IsSocialInsuranceSubject = hrData.IsSocialInsuranceSubject,
                            TaxCode = hrData.TaxCode,
                            WorkingDays = workingDays,
                            StandardWorkingDays = standardDays,
                            LeaveDays = paidLeave,
                            UnpaidLeaveDays = unpaidLeave,
                            DependentCount = hrData.DependentsCount,
                            OvertimePay = overtimePay,
                            Bonus = 0,
                            Deduction = 0
                        };

                        try
                        {
                            var savedPayroll = await _payrollService.CreatePayrollAsync(payrollCreateDto);

                            // Ghi thêm EmployeeStatus nếu Inactive
                            if (isInactive)
                            {
                                var entity = await _repository.GetPayrollByIdAsync(savedPayroll.Id);
                                if (entity != null)
                                {
                                    entity.EmployeeStatus = employeeStatus;
                                    entity.UpdatedAt = DateTime.UtcNow;
                                    await _repository.UpdatePayrollAsync(entity);
                                }
                            }

                            createdCount++;
                            payrollResults.Add(new
                            {
                                employeeId = emp.EmployeeId,
                                employeeCode = hrData.Employee?.EmployeeCode ?? emp.EmployeeCode,
                                fullName = hrData.Employee?.FullName ?? emp.FullName,
                                departmentName = hrData.Employee?.DepartmentName ?? emp.DepartmentName,
                                employeeStatus,
                                status = "Created",
                                payrollId = savedPayroll.Id,
                                contractBasicSalary = hrData.ContractBasicSalary,
                                salaryRatio = hrData.SalaryRatio,
                                taxType = hrData.TaxType,
                                isSocialInsurance = hrData.IsSocialInsuranceSubject,
                                workingDays,
                                overtimePay = Math.Round(overtimePayDouble, 0),
                                grossIncome = savedPayroll.GrossIncome,
                                totalDeduction = savedPayroll.TotalDeduction,
                                netSalary = savedPayroll.FinalSalary
                            });
                        }
                        catch (ArgumentException argEx)
                        {
                            _logger.LogWarning("[Payroll] Lỗi tạo payroll cho {EmployeeId}: {Message}",
                                emp.EmployeeId, argEx.Message);
                            payrollResults.Add(new
                            {
                                employeeId = emp.EmployeeId,
                                status = "Error",
                                error = argEx.Message
                            });
                        }
                    }
                    catch (Exception empEx)
                    {
                        _logger.LogError(empEx, "[Payroll] Lỗi xử lý nhân viên {EmployeeId}", emp.EmployeeId);
                        payrollResults.Add(new
                        {
                            employeeId = emp.EmployeeId,
                            status = "Error",
                            error = empEx.Message
                        });
                    }
                }

                _logger.LogInformation("[Payroll] Hoàn thành: Tạo {Created}, Cập nhật {Updated}",
                    createdCount, updatedCount);

                return Ok(new
                {
                    month,
                    totalEmployees = employees.Count,
                    created = createdCount,
                    updated = updatedCount,
                    totalPayrolls = createdCount + updatedCount,
                    payrolls = payrollResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Payroll] Lỗi calculate-and-save");
                return StatusCode(500, new { error = "Lỗi khi tính lương", detail = ex.Message });
            }
        }
    }
}
