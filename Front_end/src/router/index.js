import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { message } from 'ant-design-vue'

const routes = [
  {
    path: '/',
    name: 'Landing',
    component: () => import('@/views/LandingPage.vue'),
    meta: { public: true },
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/auth/LoginView.vue'),
    meta: { public: true },
  },
  {
    path: '/app',
    component: () => import('@/layouts/AppLayout.vue'),
    redirect: '/app/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'Tổng quan' },
      },
      // HR Core
      {
        path: 'hr/employees',
        name: 'Employees',
        component: () => import('@/views/hr/EmployeesView.vue'),
        meta: { title: 'Nhân viên', roles: ['Admin', 'HR'] },
      },
      {
        path: 'hr/departments',
        name: 'Departments',
        component: () => import('@/views/hr/DepartmentsView.vue'),
        meta: { title: 'Phòng ban', roles: ['Admin', 'HR'] },
      },
      {
        path: 'hr/my-contracts',
        name: 'MyContracts',
        component: () => import('@/views/hr/MyContractsView.vue'),
        meta: { title: 'Hợp đồng của tôi', roles: ['Admin', 'HR', 'Manager', 'Employee', 'KT'] },
      },
      // Attendance
      {
        path: 'attendance/check',
        name: 'AttendanceCheck',
        component: () => import('@/views/attendance/CheckInOutView.vue'),
        meta: { title: 'Chấm công', roles: ['Admin', 'HR', 'Manager', 'Employee', 'KT'] },
      },
      {
        path: 'attendance/history',
        name: 'AttendanceHistory',
        component: () => import('@/views/attendance/AttendanceHistoryView.vue'),
        meta: { title: 'Lịch sử chấm công', roles: ['Admin', 'HR', 'Manager', 'Employee', 'KT'] },
      },
      {
        path: 'attendance/leave',
        name: 'LeaveManagement',
        component: () => import('@/views/attendance/LeaveView.vue'),
        meta: { title: 'Quản lý nghỉ phép', roles: ['Admin', 'HR', 'Manager', 'Employee', 'KT'] },
      },
      // Payroll
      {
        path: 'payroll/list',
        name: 'PayrollList',
        component: () => import('@/views/payroll/PayrollListView.vue'),
        meta: { title: 'Bảng lương', roles: ['Admin', 'HR', 'Employee', 'KT'] },
      },
      {
        path: 'payroll/report',
        name: 'PayrollReport',
        component: () => import('@/views/payroll/PayrollReportView.vue'),
        meta: { title: 'Báo cáo lương', roles: ['Admin', 'HR', 'Manager', 'KT'] },
      },
    ],
  },
  { path: '/:pathMatch(.*)*', redirect: '/' },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  // Chưa login → login
  if (!to.meta.public && !auth.isLoggedIn) {
    return { name: 'Login', query: { redirect: to.fullPath } }
  }

  // Đã login mà vào login → dashboard
  if (to.name === 'Login' && auth.isLoggedIn) {
    return { path: '/app/dashboard' }
  }

  // Kiểm tra role: nếu route có meta.roles thì user phải thuộc 1 trong các role đó
  const allowedRoles = to.meta?.roles
  if (allowedRoles && allowedRoles.length > 0) {
    if (!auth.userRole || !allowedRoles.includes(auth.userRole)) {
      // FIX: trước đây redirect âm thầm về dashboard, không có thông báo gì —
      // khiến người dùng tưởng bấm menu "không ăn". Giờ báo rõ lý do + log
      // để dễ debug khi role trả về từ backend không khớp định dạng mong đợi.
      console.warn(
        `[router] Role "${auth.userRole}" không có quyền truy cập "${to.path}" (cần: ${allowedRoles.join(', ')})`,
      )
      message.warning('Tài khoản của bạn không có quyền truy cập trang này')
      return { path: '/app/dashboard' }
    }
  }
})

export default router
