import axios from 'axios'

// ─── Base URLs ────────────────────────────────────────────────────────────────
// Trong dev: dùng Vite proxy (cùng origin, tránh CORS + HTTPS cert issue)
// Khi gọi /api-hr/... → Vite proxy forward sang VITE_HR_URL
const useProxy = true // Luôn dùng relative path → Vite proxy (dev) hoặc Nginx proxy (production Docker)
export const HR_BASE = useProxy
  ? '/api-hr'
  : import.meta.env.VITE_HR_URL || 'https://localhost:7084'
export const ATTEND_BASE = useProxy
  ? '/api-attend'
  : import.meta.env.VITE_ATTEND_URL || 'https://localhost:7108'
export const PAYROLL_BASE = useProxy
  ? '/api-payroll'
  : import.meta.env.VITE_PAYROLL_URL || 'https://localhost:5000'

// ─── Factory ──────────────────────────────────────────────────────────────────
function createClient(baseURL) {
  const client = axios.create({
    baseURL,
    timeout: 15000,
    headers: { 'Content-Type': 'application/json' },
  })

  // Request: attach JWT
  client.interceptors.request.use((cfg) => {
    const token = localStorage.getItem('hrm_token')
    if (token) cfg.headers.Authorization = `Bearer ${token}`
    return cfg
  })

  // Response: 401 → logout
  client.interceptors.response.use(
    (res) => res,
    (err) => {
      if (err.response?.status === 401) {
        localStorage.removeItem('hrm_token')
        localStorage.removeItem('hrm_user')
        window.location.href = '/login'
      }
      return Promise.reject(err)
    },
  )
  return client
}

export const hrApi = createClient(HR_BASE)
export const attendApi = createClient(ATTEND_BASE)
export const payrollApi = createClient(PAYROLL_BASE)

// ─── Auth ─────────────────────────────────────────────────────────────────────
export const authApi = {
  login: (data) => hrApi.post('/api/v1/hr/auth/login', data),
  getEmployeesWithoutAccount: () => hrApi.get('/api/v1/hr/auth/employees-without-account'),
  createAccount: (data) => hrApi.post('/api/v1/hr/auth/create-account', data),
  // Account management — gọi khi backend HR đã có endpoint
  listAccounts: () => hrApi.get('/api/v1/hr/auth/accounts'),
  resetPassword: (data) => hrApi.post('/api/v1/hr/auth/reset-password', data),
  changeRole: (data) => hrApi.put('/api/v1/hr/auth/change-role', data),
}

// ─── HR Core ──────────────────────────────────────────────────────────────────
export const departmentApi = {
  getTree: () => hrApi.get('/api/v1/hr/Departments/tree'),
  create: (data) => hrApi.post('/api/v1/hr/Departments', data),
  update: (id, d) => hrApi.put(`/api/v1/hr/Departments/${id}`, d),
  remove: (id) => hrApi.delete(`/api/v1/hr/Departments/${id}`),
}

export const employeeApi = {
  getAll: (params) => hrApi.get('/api/v1/hr/Employees', { params }),
  getById: (id) => hrApi.get(`/api/v1/hr/Employees/${id}`),
  create: (data) => hrApi.post('/api/v1/hr/Employees', data),
  update: (id, d) => hrApi.put(`/api/v1/hr/Employees/${id}`, d),
  remove: (id) => hrApi.delete(`/api/v1/hr/Employees/${id}`),
  bulkCreate: (data) => hrApi.post('/api/v1/hr/Employees/bulk-create', data),
  bulkUpdate: (data) => hrApi.put('/api/v1/hr/Employees/bulk-update', data),
  bulkDelete: (data) => hrApi.delete('/api/v1/hr/Employees/bulk-delete', { data }),
}

export const contractApi = {
  getTypes: () => hrApi.get('/api/v1/hr/Contracts/types'),
  getAll: (params) => hrApi.get('/api/v1/hr/Contracts', { params }),
  getById: (id) => hrApi.get(`/api/v1/hr/Contracts/${id}`),
  create: (data) => hrApi.post('/api/v1/hr/Contracts', data),
  update: (id, d) => hrApi.put(`/api/v1/hr/Contracts/${id}`, d),
  remove: (id) => hrApi.delete(`/api/v1/hr/Contracts/${id}`),
  sign: (id) => hrApi.put(`/api/v1/hr/Contracts/${id}/sign`),
}

// ─── Attendance ───────────────────────────────────────────────────────────────
export const attendanceApi = {
  checkIn: (data, params) =>
    attendApi.post('/api/Attendance/check-in', JSON.stringify(data), { params }),
  checkOut: (data) => attendApi.post('/api/Attendance/check-out', JSON.stringify(data)),
  history: (empId, params) => attendApi.get(`/api/Attendance/history/${empId}`, { params }),
  closeMonth: (data) => attendApi.post('/api/Attendance/close-month', data),
  monthlySummary: (empId, month, year) =>
    attendApi.get(`/api/Attendance/monthly-summary/${empId}`, { params: { month, year } }),
  leaveSummary: (empId, month, year) =>
    attendApi.get(`/api/Attendance/leave-summary/${empId}`, { params: { month, year } }),
  // Debug helpers (Attendance local DB)
  listLocalEmployees: () => attendApi.get('/api/employee-sync/local-employees'),
  getSyncStatus: () => attendApi.get('/api/employee-sync/status'),
  syncFromHr: () => attendApi.post('/api/employee-sync/sync'),
}

export const leaveApi = {
  getEmployees: () => attendApi.get('/api/Leave/employees'),
  request: (data) => attendApi.post('/api/Leave/request', data),
  pendingList: () => attendApi.get('/api/Leave/pending-list'),
  getByEmployee: (empId) => attendApi.get(`/api/Leave/employee/${empId}`),
  approve: (id, approverId) => attendApi.put(`/api/Leave/approve/${id}`, { approverId }),
  reject: (id, approverId) => attendApi.put(`/api/Leave/reject/${id}`, { approverId }),
}

// ─── Payroll ──────────────────────────────────────────────────────────────────
export const payrollApiService = {
  getAll: (params) => payrollApi.get('/api/Payrolls', { params }),
  getById: (id) => payrollApi.get(`/api/Payrolls/${id}`),
  getPayslip: (id) => payrollApi.get(`/api/Payrolls/${id}/payslip`),
  getMyPayroll: (params) => payrollApi.get('/api/Payrolls/my-payroll', { params }),
  create: (data) => payrollApi.post('/api/Payrolls', data),
  update: (id, d) => payrollApi.put(`/api/Payrolls/${id}`, d),
  remove: (id) => payrollApi.delete(`/api/Payrolls/${id}`),
  reportSummary: () => payrollApi.get('/api/Payrolls/report/summary'),
  // calculateAll yêu cầu body JSON { month: 'YYYY-MM' } — KHÔNG dùng query params
  calculateAll: (body) => payrollApi.post('/api/Payrolls/calculate-all', body),
  approve: (data) => payrollApi.post('/api/Payrolls/approve', data),
}
