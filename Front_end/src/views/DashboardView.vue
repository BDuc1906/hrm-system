<template>
  <div>
    <!-- Page header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">Tổng quan</h1>
        <p style="margin: 4px 0 0; color: var(--color-text-muted); font-size: 14px">
          {{ today }}
        </p>
      </div>
      <a-button type="primary" @click="refreshAll" :loading="loadingAll">
        <template #icon><ReloadOutlined /></template>
        Làm mới
      </a-button>
    </div>

    <!-- Stat cards -->
    <a-row :gutter="[20, 20]" style="margin-bottom: 24px">
      <!-- Admin, HR, Manager: total employees -->
      <a-col
        :xs="24"
        :sm="12"
        :lg="6"
        v-if="auth.isAdmin || auth.isHR || auth.isManager"
        v-for="s in statsEmployee"
        :key="s.key"
      >
        <div class="stat-card">
          <div class="stat-icon" :style="{ background: s.gradient }">
            <component :is="s.icon" style="font-size: 20px; color: #fff" />
          </div>
          <div class="stat-body">
            <div class="stat-value">
              <a-spin v-if="s.loading" size="small" />
              <span v-else>{{ s.value }}</span>
            </div>
            <div class="stat-label">{{ s.label }}</div>
          </div>
          <div class="stat-trend" :class="s.trend > 0 ? 'up' : s.trend < 0 ? 'down' : ''">
            <span v-if="s.trend !== undefined">
              <ArrowUpOutlined v-if="s.trend > 0" />
              <ArrowDownOutlined v-if="s.trend < 0" />
              {{ Math.abs(s.trend) }}%
            </span>
          </div>
        </div>
      </a-col>
      <!-- Leave stat — all roles -->
      <a-col :xs="24" :sm="12" :lg="6" v-for="s in statsLeave" :key="s.key">
        <div class="stat-card">
          <div class="stat-icon" :style="{ background: s.gradient }">
            <component :is="s.icon" style="font-size: 20px; color: #fff" />
          </div>
          <div class="stat-body">
            <div class="stat-value">
              <a-spin v-if="s.loading" size="small" />
              <span v-else>{{ s.value }}</span>
            </div>
            <div class="stat-label">{{ s.label }}</div>
          </div>
        </div>
      </a-col>
      <!-- Payroll stat — Admin, HR, Employee -->
      <a-col
        :xs="24"
        :sm="12"
        :lg="6"
        v-if="auth.isAdmin || auth.isHR || auth.isEmployee"
        v-for="s in statsPayroll"
        :key="s.key"
      >
        <div class="stat-card">
          <div class="stat-icon" :style="{ background: s.gradient }">
            <component :is="s.icon" style="font-size: 20px; color: #fff" />
          </div>
          <div class="stat-body">
            <div class="stat-value">
              <a-spin v-if="s.loading" size="small" />
              <span v-else>{{ s.value }}</span>
            </div>
            <div class="stat-label">{{ s.label }}</div>
          </div>
        </div>
      </a-col>
    </a-row>

    <!-- Middle row -->
    <a-row :gutter="[20, 20]" style="margin-bottom: 24px">
      <!-- Quick actions -->
      <a-col :xs="24" :lg="8">
        <a-card title="Thao tác nhanh" :bordered="false">
          <div class="quick-actions">
            <router-link
              v-for="qa in quickActions"
              :key="qa.to"
              :to="qa.to"
              class="quick-action-item"
            >
              <div class="qa-icon" :style="{ background: qa.color + '18', color: qa.color }">
                <component :is="qa.icon" style="font-size: 18px" />
              </div>
              <div>
                <div class="qa-label">{{ qa.label }}</div>
                <div class="qa-desc">{{ qa.desc }}</div>
              </div>
              <RightOutlined
                style="margin-left: auto; color: var(--color-text-muted); font-size: 12px"
              />
            </router-link>
          </div>
        </a-card>
      </a-col>

      <!-- Service status -->
      <a-col :xs="24" :lg="16">
        <a-card title="Trạng thái Service" :bordered="false">
          <div class="service-grid">
            <div v-for="svc in services" :key="svc.name" class="service-card">
              <div class="service-header">
                <span class="service-name">{{ svc.name }}</span>
                <span
                  class="status-badge"
                  :class="svc.status === 'online' ? 'badge-success' : 'badge-error'"
                >
                  <span class="dot" :class="svc.status"></span>
                  {{ svc.status === 'online' ? 'Online' : 'Offline' }}
                </span>
              </div>
              <div class="service-url">{{ svc.url }}</div>
              <div class="service-endpoints">
                <a-tag
                  v-for="ep in svc.endpoints"
                  :key="ep"
                  size="small"
                  style="font-size: 11px; margin: 2px"
                  >{{ ep }}</a-tag
                >
              </div>
            </div>
          </div>
        </a-card>
      </a-col>
    </a-row>

    <!-- Charts row — Admin, HR, Manager -->
    <a-row
      v-if="auth.isAdmin || auth.isHR || auth.isManager"
      :gutter="[20, 20]"
      style="margin-bottom: 24px"
    >
      <a-col :xs="24" :lg="12">
        <a-card title="Nhân viên theo phòng ban" :bordered="false" class="chart-card">
          <Bar :data="deptChartData" :options="barChartOptions" style="max-height: 280px" />
        </a-card>
      </a-col>
      <a-col :xs="24" :lg="12">
        <a-card title="Tổng lương theo tháng" :bordered="false" class="chart-card">
          <Line :data="payrollChartData" :options="lineChartOptions" style="max-height: 280px" />
        </a-card>
      </a-col>
    </a-row>

    <!-- Recent employees table — Admin, HR -->
    <a-card v-if="auth.isAdmin || auth.isHR" title="Nhân viên gần đây" :bordered="false">
      <template #extra>
        <router-link to="/app/hr/employees">
          <a-button type="link" style="color: var(--color-primary); font-weight: 600; padding: 0">
            Xem tất cả <ArrowRightOutlined />
          </a-button>
        </router-link>
      </template>
      <a-table
        :data-source="recentEmployees"
        :columns="empColumns"
        :loading="loadingEmployees"
        :pagination="false"
        size="small"
        row-key="id"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'fullName'">
            <div style="display: flex; align-items: center; gap: 10px">
              <a-avatar
                size="small"
                :style="{
                  background: getAvatarColor(record.fullName),
                  fontFamily: 'var(--font-display)',
                  fontWeight: 700,
                }"
              >
                {{ (record.fullName || 'N')[0] }}
              </a-avatar>
              <span style="font-weight: 500">{{ record.fullName }}</span>
            </div>
          </template>
          <template v-else-if="column.key === 'status'">
            <span
              class="status-badge"
              :class="record.status === 'Active' ? 'badge-success' : 'badge-default'"
            >
              {{ record.status === 'Active' ? 'Đang làm' : 'Nghỉ việc' }}
            </span>
          </template>
        </template>
      </a-table>
    </a-card>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { employeeApi, departmentApi, payrollApiService, leaveApi } from '@/api'
import { Bar, Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js'
import dayjs from 'dayjs'
import 'dayjs/locale/vi'
import {
  UserOutlined,
  TeamOutlined,
  ClockCircleOutlined,
  DollarOutlined,
  ReloadOutlined,
  ArrowUpOutlined,
  ArrowDownOutlined,
  RightOutlined,
  ArrowRightOutlined,
  ApartmentOutlined,
  FileTextOutlined,
  BarChartOutlined,
} from '@ant-design/icons-vue'

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
)

dayjs.locale('vi')
const auth = useAuthStore()
const today = computed(() => dayjs().format('dddd, DD/MM/YYYY'))

const loadingAll = ref(false)
const loadingEmployees = ref(false)
const recentEmployees = ref([])

const stats = ref([
  {
    key: 'employees',
    label: 'Tổng nhân viên',
    value: '—',
    icon: UserOutlined,
    gradient: 'linear-gradient(135deg,#00b14f,#00d46a)',
    loading: true,
  },
  {
    key: 'dept',
    label: 'Phòng ban',
    value: '—',
    icon: ApartmentOutlined,
    gradient: 'linear-gradient(135deg,#00b4d8,#7c5cfc)',
    loading: true,
  },
  {
    key: 'leave',
    label: 'Nghỉ phép chờ duyệt',
    value: '—',
    icon: FileTextOutlined,
    gradient: 'linear-gradient(135deg,#ffb020,#ff8c00)',
    loading: true,
  },
  {
    key: 'payroll',
    label: 'Bảng lương tháng',
    value: '—',
    icon: DollarOutlined,
    gradient: 'linear-gradient(135deg,#ff4757,#ff6b81)',
    loading: true,
  },
])

const statsEmployee = computed(() =>
  auth.isAdmin || auth.isHR || auth.isManager
    ? [stats.value[0], stats.value[1]].filter(Boolean)
    : [],
)
const statsLeave = computed(() => [stats.value[2]].filter(Boolean))
const statsPayroll = computed(() =>
  auth.isAdmin || auth.isHR || auth.isEmployee ? [stats.value[3]].filter(Boolean) : [],
)

const quickActions = computed(() => {
  const base = [
    {
      to: '/app/attendance/check',
      label: 'Chấm công',
      desc: 'Check-in/out hôm nay',
      icon: ClockCircleOutlined,
      color: '#00b4d8',
    },
  ]
  if (auth.isAdmin || auth.isHR) {
    base.push(
      {
        to: '/app/hr/employees',
        label: 'Thêm nhân viên',
        desc: 'Tạo hồ sơ nhân viên mới',
        icon: UserOutlined,
        color: '#00b14f',
      },
      {
        to: '/app/attendance/leave',
        label: 'Duyệt nghỉ phép',
        desc: 'Xem danh sách chờ duyệt',
        icon: FileTextOutlined,
        color: '#ffb020',
      },
      {
        to: '/app/payroll/list',
        label: 'Tính lương',
        desc: 'Tính lương tháng tự động',
        icon: DollarOutlined,
        color: '#7c5cfc',
      },
    )
  } else if (auth.isManager) {
    base.push(
      {
        to: '/app/attendance/leave',
        label: 'Duyệt nghỉ phép',
        desc: 'Phòng ban của bạn',
        icon: FileTextOutlined,
        color: '#ffb020',
      },
      {
        to: '/app/payroll/report',
        label: 'Xem báo cáo',
        desc: 'Báo cáo lương phòng ban',
        icon: BarChartOutlined,
        color: '#7c5cfc',
      },
    )
  } else if (auth.isEmployee) {
    base.push(
      {
        to: '/app/attendance/leave',
        label: 'Đăng ký nghỉ phép',
        desc: 'Gửi đơn nghỉ phép',
        icon: FileTextOutlined,
        color: '#ffb020',
      },
      {
        to: '/app/payroll/list',
        label: 'Bảng lương',
        desc: 'Xem lương của bạn',
        icon: DollarOutlined,
        color: '#7c5cfc',
      },
    )
  }
  return base
})

const services = ref([
  {
    name: 'HR Core Service',
    url: '/api-hr',
    localUrl: '/api-hr',
    status: 'checking',
    endpoint: '/api/v1/hr/Employees?pageSize=1',
    endpoints: ['Auth', 'Departments', 'Employees'],
  },
  {
    name: 'Attendance Service',
    url: '/api-attend',
    localUrl: '/api-attend',
    status: 'checking',
    endpoint: '/api/Leave/pending-list',
    endpoints: ['Attendance', 'Leave'],
  },
  {
    name: 'Payroll Service',
    url: '/api-payroll',
    localUrl: '/api-payroll',
    status: 'checking',
    endpoint: '/api/Payrolls?pageSize=1',
    endpoints: ['Payrolls', 'Report'],
  },
])

const empColumns = [
  { title: 'Họ tên', key: 'fullName', dataIndex: 'fullName' },
  { title: 'Phòng ban', key: 'departmentName', dataIndex: 'departmentName', ellipsis: true },
  { title: 'Chức vụ', key: 'position', dataIndex: 'position', ellipsis: true },
  { title: 'Email', key: 'email', dataIndex: 'email', ellipsis: true },
  { title: 'Trạng thái', key: 'status', dataIndex: 'status', width: 120 },
]

const avatarColors = ['#00b14f', '#00b4d8', '#7c5cfc', '#ffb020', '#ff4757']
function getAvatarColor(name = '') {
  return avatarColors[name.charCodeAt(0) % avatarColors.length]
}

function countDepartments(node = []) {
  if (!Array.isArray(node)) return 0
  let count = 0
  for (const item of node) {
    count += 1
    if (item.children) count += countDepartments(item.children)
  }
  return count
}

async function loadDepartments() {
  try {
    const res = await departmentApi.getTree()
    const data = res.data
    const count =
      typeof data?.total === 'number' ? data.total : countDepartments(data?.data || data || [])
    stats.value[1].value = count || '—'
  } catch {
    stats.value[1].value = 'N/A'
  } finally {
    stats.value[1].loading = false
  }
}

async function loadEmployees() {
  loadingEmployees.value = true
  try {
    const res = await employeeApi.getAll({ page: 1, pageSize: 6 })
    const data = res.data
    const list = Array.isArray(data) ? data : data.data || data.items || data.employees || []
    recentEmployees.value = list.slice(0, 6)
    stats.value[0].value = data.total || data.totalCount || list.length || 'N/A'
  } catch {
    stats.value[0].value = 'N/A'
    recentEmployees.value = []
  } finally {
    loadingEmployees.value = false
    stats.value[0].loading = false
  }
}

async function tryFetch(url) {
  const controller = new AbortController()
  const id = setTimeout(() => controller.abort(), 4000)
  const token = localStorage.getItem('hrm_token')
  const res = await fetch(url, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    mode: 'cors',
    signal: controller.signal,
  })
  clearTimeout(id)
  // Endpoint chỉ cần tồn tại (2xx hoặc 4xx do auth/perm) là tính online.
  // 5xx / network error / timeout = offline.
  if (res.status >= 500) throw new Error('Server error: ' + res.status)
}

async function checkServices() {
  for (let i = 0; i < services.value.length; i++) {
    const svc = services.value[i]
    const tails = `${svc.url}${svc.endpoint}`
    const local = `${svc.localUrl}${svc.endpoint}`
    svc.status = 'checking'
    try {
      await tryFetch(tails)
      svc.status = 'online'
    } catch (e1) {
      console.warn(`[ServiceCheck] ${svc.name} Tailscale FAILED:`, tails, e1?.message)
      try {
        await tryFetch(local)
        svc.url = svc.localUrl
        svc.status = 'online'
      } catch (e2) {
        console.warn(`[ServiceCheck] ${svc.name} Local FAILED:`, local, e2?.message)
        svc.status = 'offline'
      }
    }
  }
  stats.value.forEach((s) => (s.loading = false))
}

// Chart data
const deptChartData = ref({
  labels: [],
  datasets: [
    {
      label: 'Nhân viên',
      data: [],
      backgroundColor: '#6366F1',
      borderRadius: 4,
      borderSkipped: false,
    },
  ],
})

const payrollChartData = ref({
  labels: [],
  datasets: [
    {
      label: 'Tổng lương (triệu đồng)',
      data: [],
      borderColor: '#10B981',
      backgroundColor: 'rgba(16,185,129,0.1)',
      fill: true,
      tension: 0.4,
      pointBackgroundColor: '#10B981',
      pointRadius: 4,
      pointHoverRadius: 6,
    },
  ],
})

const barChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    tooltip: {
      backgroundColor: '#334155',
      titleColor: '#fff',
      bodyColor: '#fff',
      padding: 10,
      cornerRadius: 6,
    },
  },
  scales: {
    x: {
      grid: { display: false },
      ticks: { color: '#64748B', font: { size: 11 } },
    },
    y: {
      grid: { color: '#E4E4E7' },
      ticks: { color: '#64748B', font: { size: 11 } },
      beginAtZero: true,
    },
  },
}

const lineChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      display: true,
      position: 'top',
      labels: { color: '#64748B', font: { size: 11 }, boxWidth: 12 },
    },
    tooltip: {
      backgroundColor: '#334155',
      titleColor: '#fff',
      bodyColor: '#fff',
      padding: 10,
      cornerRadius: 6,
      callbacks: {
        label: (ctx) => ` ${ctx.dataset.label}: ${ctx.parsed.y.toLocaleString('vi-VN')} đ`,
      },
    },
  },
  scales: {
    x: {
      grid: { display: false },
      ticks: { color: '#64748B', font: { size: 11 } },
    },
    y: {
      grid: { color: '#E4E4E7' },
      ticks: { color: '#64748B', font: { size: 11 }, callback: (v) => (v / 1e6).toFixed(0) + 'M' },
      beginAtZero: true,
    },
  },
}

async function loadDeptChart() {
  try {
    // Lấy tất cả nhân viên rồi đếm theo phòng ban
    const res = await employeeApi.getAll({ pageSize: 500 })
    const data = res.data
    const empList = Array.isArray(data) ? data : data.data || data.items || data.employees || []

    const deptMap = {}
    for (const emp of empList) {
      const dept = emp.departmentName || emp.department || 'Khác'
      deptMap[dept] = (deptMap[dept] || 0) + 1
    }

    const entries = Object.entries(deptMap).sort((a, b) => b[1] - a[1])
    deptChartData.value = {
      labels: entries.map((e) => e[0]),
      datasets: [
        {
          label: 'Nhân viên',
          data: entries.map((e) => e[1]),
          backgroundColor: ['#6366F1', '#00b14f', '#00b4d8', '#ffb020', '#ff4757', '#7c5cfc'],
          borderRadius: 4,
          borderSkipped: false,
        },
      ],
    }
  } catch (e) {
    console.error('[deptChart] error', e)
  }
}

async function loadPayrollChart() {
  const now = dayjs()
  const months = Array.from({ length: 6 }, (_, i) => now.subtract(5 - i, 'month'))
  const labels = months.map((m) => m.format('MMM YYYY'))

  try {
    // Replace whole chart object so vue-chartjs reacts reliably.
    const res = await payrollApiService.getAll({ pageSize: 500 })
    const raw = res.data
    const list = Array.isArray(raw) ? raw : raw.data || raw.items || raw.payrolls || []

    const monthMap = {}
    for (const r of list) {
      const periodStr = r.payPeriod || r.period || r.monthYear || ''
      const parsed = periodStr ? dayjs(`${periodStr}-01`) : r.createdAt ? dayjs(r.createdAt) : null
      const key = parsed?.isValid?.() ? parsed.format('YYYY-MM') : null
      if (!key) continue
      const sal = r.finalSalary ?? r.netSalary ?? r.totalSalary ?? 0
      monthMap[key] = (monthMap[key] || 0) + sal
    }

    payrollChartData.value = {
      labels,
      datasets: [
        {
          ...payrollChartData.value.datasets[0],
          data: months.map((m) => monthMap[m.format('YYYY-MM')] || 0),
        },
      ],
    }
  } catch (e) {
    console.error('[payrollChart]', e)
    payrollChartData.value = {
      labels,
      datasets: [
        {
          ...payrollChartData.value.datasets[0],
          data: Array(6).fill(0),
        },
      ],
    }
  }
}

async function loadLeaveStat() {
  try {
    const res = await leaveApi.pendingList()
    const data = res.data
    stats.value[2].value = Array.isArray(data)
      ? data.length
      : data?.total || data?.count || data?.length || '—'
  } catch {
    stats.value[2].value = '—'
  } finally {
    stats.value[2].loading = false
  }
}

async function loadPayrollStat() {
  try {
    const now = dayjs()
    if (auth.isAdmin || auth.isHR || auth.isManager) {
      const res = await payrollApiService.reportSummary({
        year: now.year(),
        month: now.month() + 1,
      })
      const d = res.data
      const total = d?.totalFinalSalary ?? d?.totalNetSalary ?? d?.totalSalary ?? 0
      stats.value[3].value = total > 0 ? (total / 1e6).toFixed(1) + 'M' : '0'
      stats.value[3].label = `Tổng lương tháng ${now.format('MM/YYYY')}`
    } else {
      // Employee
      const res = await payrollApiService.getAll({
        employeeId: auth.userId || '',
        month: now.format('YYYY-MM'),
      })
      const list = res.data
      const record = Array.isArray(list) ? list[0] : list.data?.[0] || list.items?.[0] || null
      if (record) {
        const sal = record.finalSalary ?? record.netSalary ?? record.totalSalary ?? 0
        stats.value[3].value = sal.toLocaleString('vi-VN') + ' đ'
        stats.value[3].label = `Thực lĩnh ${now.format('MM/YYYY')}`
      } else {
        stats.value[3].value = '—'
        stats.value[3].label = `Lương tháng ${now.format('MM/YYYY')}`
      }
    }
  } catch {
    stats.value[3].value = '—'
  } finally {
    stats.value[3].loading = false
  }
}

async function refreshAll() {
  loadingAll.value = true
  await Promise.allSettled([
    loadEmployees(),
    loadDepartments(),
    loadDeptChart(),
    loadPayrollChart(),
    loadLeaveStat(),
    loadPayrollStat(),
    checkServices(),
  ])
  loadingAll.value = false
}

onMounted(refreshAll)
</script>

<style scoped>
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 24px;
}
.stat-card {
  background: #fff;
  border-radius: var(--radius-xl);
  border: 1px solid var(--color-border);
  box-shadow: var(--shadow-card);
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  transition: all var(--transition);
}
.stat-card:hover {
  box-shadow: var(--shadow-card-hover);
  transform: translateY(-2px);
}
.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.stat-body {
  flex: 1;
}
.stat-value {
  font-family: var(--font-display);
  font-size: 24px;
  font-weight: 700;
  color: var(--color-text);
}
.stat-label {
  font-size: 13px;
  color: var(--color-text-muted);
  margin-top: 2px;
}
.stat-trend {
  font-size: 12px;
  font-weight: 600;
}
.stat-trend.up {
  color: var(--color-primary);
}
.stat-trend.down {
  color: var(--color-error);
}

.quick-actions {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.quick-action-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 10px;
  border-radius: var(--radius-md);
  text-decoration: none;
  color: var(--color-text);
  transition: background var(--transition);
}
.quick-action-item:hover {
  background: var(--color-surface);
}
.qa-icon {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.qa-label {
  font-size: 14px;
  font-weight: 600;
  color: var(--color-text);
}
.qa-desc {
  font-size: 12px;
  color: var(--color-text-muted);
  margin-top: 2px;
}

.service-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}
.service-card {
  padding: 16px;
  border-radius: var(--radius-lg);
  border: 1px solid var(--color-border);
  background: var(--color-surface);
}
.service-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 6px;
}
.service-name {
  font-weight: 600;
  font-size: 13px;
  color: var(--color-text);
}
.service-url {
  font-size: 11px;
  color: var(--color-text-muted);
  margin-bottom: 10px;
  font-family: monospace;
}
.dot {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  margin-right: 4px;
  vertical-align: middle;
}
.dot.online {
  background: var(--color-primary);
  box-shadow: 0 0 4px rgba(0, 177, 79, 0.6);
}
.dot.offline {
  background: var(--color-error);
}
.dot.checking {
  background: var(--color-warning);
}

.chart-card :deep(.ant-card-head-title) {
  font-family: var(--font-display);
  font-size: 15px;
  font-weight: 600;
  color: var(--color-text);
}

@media (max-width: 768px) {
  .service-grid {
    grid-template-columns: 1fr;
  }
  .page-header {
    flex-direction: column;
    gap: 12px;
  }
  .app-content {
    padding: 16px;
  }
}
</style>
