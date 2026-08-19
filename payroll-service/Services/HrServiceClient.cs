using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using payroll_service.DTOs;

namespace payroll_service.Services;

public class HrServiceClient : IHrServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<HrServiceClient> _logger;

    private string BaseUrl => _config["HrService:BaseUrl"]?.TrimEnd('/') ?? "";

    public HrServiceClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<HrServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    // ── JWT nội bộ ───────────────────────────────────────────────────
    private Task<string?> GetTokenAsync()
    {
        try
        {
            var secret = _config["Jwt:Secret"] ?? string.Empty;
            var issuer = _config["Jwt:Issuer"] ?? string.Empty;
            var audience = _config["Jwt:Audience"] ?? string.Empty;
            if (!int.TryParse(_config["Jwt:ExpiryMinutes"], out var expiry)) expiry = 60;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, issuer),
                new(ClaimTypes.Role, "Admin")
            };
            var token = new JwtSecurityToken(issuer, audience, claims,
                expires: DateTime.UtcNow.AddMinutes(expiry),
                signingCredentials: creds);
            return Task.FromResult<string?>(new JwtSecurityTokenHandler().WriteToken(token));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi tạo JWT nội bộ");
            return Task.FromResult<string?>(null);
        }
    }

    private HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        // Tránh treo vô thời hạn nếu HR Service không phản hồi (trước đây không
        // set Timeout -> mặc định 100s, khiến việc "tính lương tất cả" bị treo
        // rất lâu trước khi âm thầm trả về contract=null cho từng nhân viên).
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var client = CreateHttpClient();
        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── GetEmployeesAsync (existing) ─────────────────────────────────
    public async Task<string> GetEmployeesAsync()
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var endpoint = _config["HrService:EmployeesEndpoint"] ?? "/api/v1/hr/Employees";
            var url = $"{BaseUrl}{endpoint}";
            _logger.LogInformation("Gọi HR employees: {Url}", url);

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();

            var err = $"HR lỗi: {response.StatusCode}";
            _logger.LogWarning(err);
            return JsonSerializer.Serialize(new { error = err });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi gọi HR employees");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // ── GetEmployeeByIdAsync — dùng extended, map về basic DTO ───────
    public async Task<HrEmployeeDTO?> GetEmployeeByIdAsync(string employeeId)
    {
        var ext = await GetEmployeeExtendedAsync(employeeId);
        if (ext == null) return null;
        return new HrEmployeeDTO
        {
            EmployeeId = ext.EmployeeId,
            EmployeeCode = ext.EmployeeCode,
            FullName = ext.FullName,
            DepartmentName = ext.DepartmentName,
            TaxCode = ext.TaxCode,
            DependentsCount = ext.DependentsCount
        };
    }

    // ── GetEmployeeExtendedAsync ─────────────────────────────────────
    public async Task<HrEmployeeExtendedDTO?> GetEmployeeExtendedAsync(string employeeId)
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var template = _config["HrService:EmployeeByIdEndpoint"] ?? "/api/v1/hr/Employees/{id}";
            var url = $"{BaseUrl}{template.Replace("{id}", employeeId)}";
            _logger.LogInformation("Gọi HR employee {Id}: {Url}", employeeId, url);

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HR trả {Status} cho {Id}", response.StatusCode, employeeId);
                return null;
            }
            var content = await response.Content.ReadAsStringAsync();
            return ParseEmployeeExtended(content, employeeId);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "[HR] TIMEOUT khi gọi Employee {Id} tại {Url} — HR Service không phản hồi trong 10s. " +
                "Kiểm tra HR Service có đang chạy và có truy cập được từ payroll-service không.",
                employeeId, BaseUrl);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[HR] KHÔNG KẾT NỐI ĐƯỢC tới HR Service ({Url}) khi lấy Employee {Id}. " +
                "Kiểm tra HrService:BaseUrl trong appsettings.json và kết nối mạng.",
                BaseUrl, employeeId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HR] Lỗi không xác định khi gọi Employee {Id}", employeeId);
            return null;
        }
    }

    // ── GetActiveContractAsync ───────────────────────────────────────
    public async Task<HrContractDTO?> GetActiveContractAsync(string employeeId)
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var endpoint = _config["HrService:ContractsEndpoint"] ?? "/api/contracts";
            var url = $"{BaseUrl}{endpoint}?employeeId={employeeId}";
            _logger.LogInformation("Gọi HR contracts {Id}: {Url}", employeeId, url);

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                // TRƯỚC ĐÂY: âm thầm return null, không log status/body -> không
                // thể phân biệt được "401 do JWT sai", "404 do sai route" hay
                // "HR trả về danh sách rỗng vì nhân viên chưa có hợp đồng".
                var body = await response.Content.ReadAsStringAsync();
                if (body.Length > 500) body = body.Substring(0, 500) + "...";
                _logger.LogWarning(
                    "[HR] Contracts trả {Status} cho nhân viên {Id} — url={Url} — body={Body}",
                    response.StatusCode, employeeId, url, body);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            List<HrContractDTO>? contracts = null;
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
                contracts = JsonSerializer.Deserialize<List<HrContractDTO>>(content, opts);
            else if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array)
                contracts = JsonSerializer.Deserialize<List<HrContractDTO>>(d.GetRawText(), opts);

            if (contracts == null || contracts.Count == 0)
            {
                _logger.LogWarning(
                    "[HR] Nhân viên {Id} KHÔNG CÓ hợp đồng nào trong HR (danh sách rỗng) — " +
                    "lương sẽ tính = 0 cho tới khi tạo Hợp đồng cho nhân viên này.",
                    employeeId);
                return null;
            }

            var active = contracts
                .Where(c => c.Status?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefault();

            if (active == null)
            {
                var statuses = string.Join(", ", contracts.Select(c => c.Status ?? "(null)"));
                _logger.LogWarning(
                    "[HR] Nhân viên {Id} có {Count} hợp đồng nhưng KHÔNG hợp đồng nào Active " +
                    "(statuses: {Statuses}) — dùng tạm hợp đồng mới nhất, lương có thể không chính xác.",
                    employeeId, contracts.Count, statuses);
                active = contracts.OrderByDescending(c => c.StartDate).FirstOrDefault();
            }

            return active;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "[HR] TIMEOUT khi gọi Contracts cho {Id} — HR Service không phản hồi trong 10s.",
                employeeId);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[HR] KHÔNG KẾT NỐI ĐƯỢC tới HR Service ({Url}) khi lấy Contract {Id}.",
                BaseUrl, employeeId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HR] Lỗi không xác định khi gọi Contract {Id}", employeeId);
            return null;
        }
    }

    // ── GetContractTypeAsync ─────────────────────────────────────────
    public async Task<HrContractTypeDTO?> GetContractTypeAsync(string contractTypeId)
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var endpoint = _config["HrService:ContractTypesEndpoint"] ?? "/api/contracts/types";
            var url = $"{BaseUrl}{endpoint}";
            _logger.LogInformation("Gọi HR contract types: {Url}", url);

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[HR] Contract types trả {Status} — url={Url}", response.StatusCode, url);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            List<HrContractTypeDTO>? types = null;
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
                types = JsonSerializer.Deserialize<List<HrContractTypeDTO>>(content, opts);
            else if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array)
                types = JsonSerializer.Deserialize<List<HrContractTypeDTO>>(d.GetRawText(), opts);

            return types?.FirstOrDefault(t =>
                t.Id?.Equals(contractTypeId, StringComparison.OrdinalIgnoreCase) == true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HR] Lỗi gọi contract types");
            return null;
        }
    }

    // ── GetActiveContractAsync (compat wrapper cho các nơi khác gọi) ──
    // (giữ nguyên chữ ký cũ ở IHrServiceClient.cs.cs nếu có)

    // ── GetPayrollDataAsync — gọi song song, tổng hợp ────────────────
    public async Task<HrPayrollDataDTO> GetPayrollDataAsync(string employeeId)
    {
        var empTask = GetEmployeeExtendedAsync(employeeId);
        var contractTask = GetActiveContractAsync(employeeId);
        await Task.WhenAll(empTask, contractTask);

        var employee = empTask.Result;
        var contract = contractTask.Result;

        HrContractTypeDTO? contractType = null;
        if (!string.IsNullOrEmpty(contract?.ContractTypeId))
            contractType = await GetContractTypeAsync(contract.ContractTypeId);

        _logger.LogInformation(
            "[HR] {Id}: salary={S}, ratio={R}, taxType={T}, bhxh={B}, hasContract={HasContract}, hasEmployee={HasEmployee}",
            employeeId,
            contract?.BasicSalary ?? 0,
            contract?.SalaryRatio ?? 1,
            contractType?.TaxType ?? "Progressive",
            contractType?.IsSocialInsuranceSubject ?? true,
            contract != null,
            employee != null);

        return new HrPayrollDataDTO
        {
            Employee = employee,
            ActiveContract = contract,
            ContractType = contractType
        };
    }

    // ── Parse helpers ────────────────────────────────────────────────
    private HrEmployeeExtendedDTO? ParseEmployeeExtended(string content, string employeeId)
    {
        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in new[] { "data", "result", "payload", "employee" })
                    if (root.TryGetProperty(key, out var inner) &&
                        inner.ValueKind == JsonValueKind.Object)
                    { root = inner; break; }
            }
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                root = root[0];

            try
            {
                var dto = JsonSerializer.Deserialize<HrEmployeeExtendedDTO>(root.GetRawText(), opts);
                if (dto != null)
                {
                    if (string.IsNullOrEmpty(dto.EmployeeId)) dto.EmployeeId = employeeId;
                    return dto;
                }
            }
            catch { /* fallthrough */ }

            return new HrEmployeeExtendedDTO
            {
                EmployeeId = Str(root, "id", "employeeId") ?? employeeId,
                EmployeeCode = Str(root, "employeeCode", "code"),
                FullName = Str(root, "fullName", "name"),
                DepartmentName = Str(root, "departmentName", "department"),
                TaxCode = Str(root, "taxCode"),
                DependentsCount = root.TryGetProperty("dependentsCount", out var dc) &&
                                  dc.ValueKind == JsonValueKind.Number ? dc.GetInt32() : 0,
                Status = Str(root, "status"),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi parse employee extended");
            return null;
        }
    }

    private static string? Str(JsonElement el, params string[] keys)
    {
        foreach (var k in keys)
            if (el.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        return null;
    }
}
