/**
 * Phân quyền theo role
 *
 * Admin  — toàn quyền
 * HR     — quản lý nhân sự (không xoá nhân viên)
 * Manager — quản lý phòng mình
 * Employee — chỉ thấy dữ liệu cá nhân
 */

export const ROLE_LABELS = {
  Admin: 'Quản trị viên',
  HR: 'Nhân sự',
  Manager: 'Quản lý',
  Employee: 'Nhân viên',
}

// Route → role được phép truy cập
export const ROUTE_ROLES = {
  Dashboard: ['Admin', 'HR', 'Manager', 'Employee'],
  Employees: ['Admin', 'HR'],
  Departments: ['Admin', 'HR'],
  AttendanceCheck: ['Admin', 'HR', 'Manager', 'Employee'],
  AttendanceHistory: ['Admin', 'HR', 'Manager', 'Employee'],
  LeaveManagement: ['Admin', 'HR', 'Manager', 'Employee'],
  PayrollList: ['Admin', 'HR', 'Employee'],
  PayrollReport: ['Admin', 'HR', 'Manager'],
}

// Check user có quyền truy cập route không
export function canAccessRoute(role, routeName) {
  const allowed = ROUTE_ROLES[routeName]
  if (!allowed) return false
  if (!role) return true // Chưa load xong auth → cho qua để check tiếp
  return allowed.includes(role)
}

// Check resource theo role
export function canAccess(role, resource) {
  const map = {
    Admin: ['employees', 'departments', 'attendance', 'leave', 'payroll', 'report', 'calculate'],
    HR: ['employees', 'departments', 'attendance', 'leave', 'report'],
    Manager: ['attendance-own', 'leave-own', 'report-own'],
    Employee: ['attendance-own', 'leave-own', 'payroll-own'],
  }
  return map[role]?.includes(resource) ?? false
}
