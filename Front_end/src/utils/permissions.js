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
  KT: 'Kế toán',
}

// Route → role được phép truy cập
export const ROUTE_ROLES = {
  Dashboard: ['Admin', 'HR', 'Manager', 'Employee','KT'],
  Employees: ['Admin', 'HR'],
  Departments: ['Admin', 'HR'],
  AttendanceCheck: ['Admin', 'HR', 'Manager', 'Employee','KT'],
  AttendanceHistory: ['Admin', 'HR', 'Manager', 'Employee','KT'],
  LeaveManagement: ['Admin', 'HR', 'Manager', 'Employee','KT'],
  PayrollList: ['Admin', 'HR', 'Employee','KT'],
  PayrollReport: ['Admin', 'HR', 'Manager','KT'],
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
    KT: ['attendance-own', 'leave-own', 'payroll', 'report', 'calculate'],
  }
  return map[role]?.includes(resource) ?? false
}
