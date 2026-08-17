<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">Báo cáo lương</h1>
        <p style="margin: 4px 0 0; font-size: 14px; color: var(--color-text-muted)">
          Tổng hợp và phân tích chi phí lương
        </p>
      </div>
      <a-button type="primary" @click="loadReport" :loading="loading">
        <BarChartOutlined /> Tạo báo cáo
      </a-button>
    </div>

    <!-- Filter bar -->
    <a-card :bordered="false" style="margin-bottom: 20px">
      <a-row :gutter="[16, 12]" align="middle">
        <a-col :xs="24" :sm="8" :lg="5">
          <a-date-picker
            v-model:value="filterMonth"
            picker="month"
            format="MM/YYYY"
            placeholder="Tháng báo cáo"
            style="width: 100%"
          />
        </a-col>
        <a-col :xs="24" :sm="8" :lg="5">
          <a-select
            v-model:value="filterDept"
            placeholder="Phòng ban"
            allow-clear
            style="width: 100%"
          >
            <a-select-option value="">Tất cả phòng ban</a-select-option>
            <a-select-option v-for="d in departments" :key="d.id" :value="d.id">{{
              d.name
            }}</a-select-option>
          </a-select>
        </a-col>
        <a-col :xs="24" :sm="8" :lg="4">
          <a-button @click="loadReport" :loading="loading" block>
            <SearchOutlined /> Xem báo cáo
          </a-button>
        </a-col>
      </a-row>
    </a-card>

    <!-- KPI cards -->
    <a-row :gutter="[16, 16]" style="margin-bottom: 24px" v-if="summary">
      <a-col :xs="12" :sm="6" v-for="kpi in kpiCards" :key="kpi.label">
        <div class="kpi-card">
          <div class="kpi-icon" :style="{ background: kpi.gradient }">
            <component :is="kpi.icon" style="font-size: 18px; color: #fff" />
          </div>
          <div class="kpi-value">{{ kpi.value }}</div>
          <div class="kpi-label">{{ kpi.label }}</div>
        </div>
      </a-col>
    </a-row>

    <!-- Charts row -->
    <a-row v-if="summary" :gutter="[20, 20]" style="margin-bottom: 24px">
      <!-- Salary breakdown donut -->
      <a-col :xs="24" :lg="10">
        <a-card title="Cơ cấu chi phí lương" :bordered="false">
          <div class="chart-wrap">
            <svg viewBox="0 0 200 200" class="donut-chart">
              <g v-for="(seg, i) in donutSegments" :key="i">
                <path :d="seg.d" :fill="seg.color" opacity="0.9" />
              </g>
              <circle cx="100" cy="100" r="55" fill="white" />
              <text
                x="100"
                y="96"
                text-anchor="middle"
                font-size="11"
                fill="#5a6578"
                font-family="DM Sans"
              >
                Tổng quỹ
              </text>
              <text
                x="100"
                y="114"
                text-anchor="middle"
                font-size="13"
                font-weight="700"
                fill="#0d1b2a"
                font-family="Plus Jakarta Sans"
              >
                {{ shortCurrency(summary.totalNetSalary || 0) }}
              </text>
            </svg>
            <div class="donut-legend">
              <div v-for="(seg, i) in donutSegments" :key="i" class="legend-item">
                <span class="legend-dot" :style="{ background: seg.color }"></span>
                <span class="legend-label">{{ seg.label }}</span>
                <span class="legend-value">{{ formatCurrency(seg.amount) }}</span>
              </div>
            </div>
          </div>
        </a-card>
      </a-col>

      <!-- Bar chart by dept -->
      <a-col :xs="24" :lg="14">
        <a-card title="Phân bổ lương theo phòng ban" :bordered="false">
          <div class="bar-chart-wrap">
            <div v-for="(row, i) in deptBars" :key="i" class="bar-row">
              <div class="bar-label">{{ row.dept }}</div>
              <div class="bar-track">
                <div
                  class="bar-fill"
                  :style="{ width: row.pct + '%', background: barColors[i % barColors.length] }"
                ></div>
              </div>
              <div class="bar-amount">{{ formatCurrency(row.amount) }}</div>
            </div>
            <a-empty v-if="!deptBars.length" description="Không có dữ liệu" />
          </div>
        </a-card>
      </a-col>
    </a-row>

    <!-- Detail table -->
    <a-card title="Chi tiết bảng lương" :bordered="false" v-if="details.length">
      <template #extra>
        <a-button size="small" @click="exportCSV"> <DownloadOutlined /> Xuất CSV </a-button>
      </template>
      <a-table
        :data-source="details"
        :columns="detailColumns"
        row-key="id"
        :loading="loading"
        size="small"
        :pagination="{ pageSize: 10, showTotal: (t) => `${t} bản ghi` }"
        :scroll="{ x: 800 }"
        :summary="tableSummary"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'employee'">
            <div style="display: flex; align-items: center; gap: 8px">
              <a-avatar
                size="small"
                :style="{
                  background: getColor(record.employeeName || ''),
                  fontSize: '11px',
                  fontWeight: 700,
                }"
              >
                {{ (record.employeeName || 'N')[0] }}
              </a-avatar>
              <span style="font-weight: 500">{{
                record.employeeName || `#${record.employeeId}`
              }}</span>
            </div>
          </template>
          <template v-else-if="column.key === 'net'">
            <span style="font-weight: 700; color: var(--color-primary)">{{
              formatCurrency(netOf(record))
            }}</span>
          </template>
          <template v-else-if="column.key === 'status'">
            <span class="status-badge" :class="getStatusBadge(record.payrollStatus)">{{
              getStatusLabel(record.payrollStatus)
            }}</span>
          </template>
        </template>
        <template #summary>
          <a-table-summary-row>
            <a-table-summary-cell
              :index="0"
              :col-span="4"
              style="font-weight: 700; color: var(--color-text)"
              >Tổng cộng</a-table-summary-cell
            >
            <a-table-summary-cell
              :index="4"
              style="font-weight: 700; color: var(--color-primary); font-size: 15px"
              >{{ formatCurrency(totalNet) }}</a-table-summary-cell
            >
            <a-table-summary-cell :index="5" />
          </a-table-summary-row>
        </template>
      </a-table>
    </a-card>

    <a-empty
      v-if="!loading && !summary"
      description="Chọn tháng và nhấn 'Xem báo cáo'"
      style="padding: 80px 0"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { payrollApiService } from '@/api'
import { departmentApi } from '@/api'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'
import {
  BarChartOutlined,
  SearchOutlined,
  DownloadOutlined,
  TeamOutlined,
  DollarOutlined,
  BankOutlined,
  CalculatorOutlined,
} from '@ant-design/icons-vue'

const loading = ref(false)
const summary = ref(null)
const details = ref([])
const departments = ref([])
const filterMonth = ref(dayjs())
const filterDept = ref('')

const colors = ['#00b14f', '#00b4d8', '#7c5cfc', '#ffb020', '#ff4757']
const barColors = ['#00b14f', '#00b4d8', '#7c5cfc', '#ffb020', '#ff4757', '#1a2332']
const getColor = (n) => colors[(n || '').charCodeAt(0) % colors.length]

const formatCurrency = (v) =>
  v === null || v === undefined || v === ''
    ? '—'
    : new Intl.NumberFormat('vi-VN').format(Math.round(v)) + '₫'
const shortCurrency = (v) => {
  if (!v) return '0₫'
  if (v >= 1e9) return (v / 1e9).toFixed(1) + 'T₫'
  if (v >= 1e6) return (v / 1e6).toFixed(1) + 'Tr₫'
  return formatCurrency(v)
}
const getStatusBadge = (s) =>
  ({
    Draft: 'badge-default',
    Confirmed: 'badge-info',
    Paid: 'badge-success',
    Pending: 'badge-warning',
    Approved: 'badge-success',
    Rejected: 'badge-error',
    'Đã tính': 'badge-info',
  })[s] || 'badge-default'
const getStatusLabel = (s) =>
  ({
    Draft: 'Nháp',
    Confirmed: 'Đã xác nhận',
    Paid: 'Đã thanh toán',
    Pending: 'Chờ xử lý',
    Approved: 'Đã duyệt',
    Rejected: 'Từ chối',
    'Đã tính': 'Đã tính',
  })[s] ||
  s ||
  'Đã tính'

const netOf = (r) => r.finalSalary ?? r.netSalary ?? r.totalSalary ?? 0
const totalNet = computed(() => details.value.reduce((s, r) => s + netOf(r), 0))

const kpiCards = computed(() =>
  summary.value
    ? [
        {
          label: 'Nhân viên',
          value: summary.value.totalEmployees || details.value.length,
          icon: TeamOutlined,
          gradient: 'linear-gradient(135deg,#00b14f,#00d46a)',
        },
        {
          label: 'Tổng quỹ lương',
          value: shortCurrency(summary.value.totalNetSalary),
          icon: DollarOutlined,
          gradient: 'linear-gradient(135deg,#00b4d8,#7c5cfc)',
        },
        {
          label: 'Lương trung bình',
          value: shortCurrency(
            summary.value.avgNetSalary || totalNet.value / Math.max(details.value.length, 1),
          ),
          icon: CalculatorOutlined,
          gradient: 'linear-gradient(135deg,#ffb020,#ff8c00)',
        },
        {
          label: 'Đã thanh toán',
          value: details.value.filter((r) => r.payrollStatus === 'Paid').length,
          icon: BankOutlined,
          gradient: 'linear-gradient(135deg,#7c5cfc,#00b4d8)',
        },
      ]
    : [],
)

// Donut chart segments
const donutSegments = computed(() => {
  if (!summary.value) return []
  const s = summary.value
  const items = [
    { label: 'Lương net', amount: s.totalNetSalary || totalNet.value, color: '#00b14f' },
    { label: 'BHXH', amount: s.totalSocialInsurance || 0, color: '#00b4d8' },
    { label: 'Thuế TNCN', amount: s.totalIncomeTax || 0, color: '#7c5cfc' },
    { label: 'Khấu trừ', amount: s.totalDeduction || 0, color: '#ffb020' },
  ].filter((i) => i.amount > 0)

  const total = items.reduce((s, i) => s + i.amount, 0) || 1
  let startAngle = -Math.PI / 2
  return items.map((item) => {
    const sweep = (item.amount / total) * 2 * Math.PI
    const x1 = 100 + 85 * Math.cos(startAngle)
    const y1 = 100 + 85 * Math.sin(startAngle)
    const x2 = 100 + 85 * Math.cos(startAngle + sweep)
    const y2 = 100 + 85 * Math.sin(startAngle + sweep)
    const lg = sweep > Math.PI ? 1 : 0
    const d = `M100,100 L${x1},${y1} A85,85 0 ${lg},1 ${x2},${y2} Z`
    startAngle += sweep
    return { ...item, d }
  })
})

const deptBars = computed(() => {
  if (!summary.value?.byDepartment && !details.value.length) return []
  const byDept =
    summary.value?.byDepartment ||
    (() => {
      const m = {}
      for (const r of details.value) {
        const d = r.departmentName || 'Khác'
        m[d] = (m[d] || 0) + netOf(r)
      }
      return Object.entries(m).map(([dept, amount]) => ({ dept, amount }))
    })()
  const amt = (r) => r.amount ?? r.finalSalary ?? r.totalSalary ?? 0
  const max = Math.max(...(Array.isArray(byDept) ? byDept.map(amt) : [1]), 1)
  return (Array.isArray(byDept) ? byDept : []).map((r) => ({
    dept: r.dept || r.departmentName || r.name,
    amount: amt(r),
    pct: Math.round((amt(r) / max) * 100),
  }))
})

const detailColumns = [
  { title: 'Nhân viên', key: 'employee', dataIndex: 'employeeName' },
  { title: 'Phòng ban', dataIndex: 'departmentName', ellipsis: true },
  { title: 'Lương CB', dataIndex: 'baseSalary', render: (v) => formatCurrency(v), width: 140 },
  {
    title: 'Tăng ca + Thưởng',
    render: (_, r) => formatCurrency((r.overtimePay || 0) + (r.bonus || 0)),
    width: 140,
  },
  {
    title: 'Thực lĩnh',
    key: 'net',
    width: 150,
    sorter: (a, b) => (a.finalSalary || 0) - (b.finalSalary || 0),
  },
  { title: 'Trạng thái', key: 'status', dataIndex: 'payrollStatus', width: 130 },
]

async function loadReport() {
  loading.value = true
  summary.value = null
  details.value = []
  try {
    const params = {}
    if (filterMonth.value) {
      params.payPeriod = filterMonth.value.format('YYYY-MM')
    }

    const detailRes = await payrollApiService.getAll({ ...params, pageSize: 200 })
    const d = detailRes.data
    let list = Array.isArray(d) ? d : d.data || d.items || d.payrolls || []

    // Lọc theo phòng ban local nếu có chọn phòng ban
    if (filterDept.value) {
      const selectedDept = departments.value.find((dept) => dept.id === filterDept.value)
      if (selectedDept) {
        list = list.filter((r) => r.departmentName === selectedDept.name)
      }
    }

    details.value = list

    // Tính toán summary động dựa trên dữ liệu thực tế đã lọc
    const totalEmployees = list.length
    const totalNetSalary = list.reduce((s, r) => s + netOf(r), 0)
    const avgNetSalary = totalNetSalary / Math.max(totalEmployees, 1)
    const totalSocialInsurance = list.reduce((s, r) => s + (r.bhxhEmployee || 0), 0)
    const totalIncomeTax = list.reduce((s, r) => s + (r.personalTax || 0), 0)
    const totalDeduction = list.reduce((s, r) => s + (r.deduction || 0), 0)

    summary.value = {
      totalEmployees,
      totalNetSalary,
      avgNetSalary,
      totalSocialInsurance,
      totalIncomeTax,
      totalDeduction,
    }
  } catch {
    message.error('Không tải được báo cáo')
  } finally {
    loading.value = false
  }
}

function exportCSV() {
  const headers = ['Nhân viên', 'Phòng ban', 'Lương CB', 'Thực lĩnh', 'Trạng thái']
  const rows = details.value.map((r) => [
    r.employeeName || r.employeeId,
    r.departmentName || '',
    r.baseSalary || '',
    netOf(r) || '',
    getStatusLabel(r.payrollStatus),
  ])
  const csv = [headers, ...rows].map((r) => r.join(',')).join('\n')
  const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `luong_${filterMonth.value?.format('MM_YYYY') || 'report'}.csv`
  a.click()
  URL.revokeObjectURL(url)
  message.success('Đã xuất CSV')
}

async function loadDepartments() {
  try {
    const res = await departmentApi.getTree()
    const flat = (d) => {
      const r = []
      for (const n of d) {
        r.push(n)
        if (n.children) r.push(...flat(n.children))
      }
      return r
    }
    const data = Array.isArray(res.data) ? res.data : res.data.data || []
    departments.value = flat(data)
  } catch {}
}

const tableSummary = () => null // handled by template #summary slot

onMounted(loadDepartments)
</script>

<style scoped>
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 24px;
}

.kpi-card {
  background: #fff;
  border-radius: var(--radius-xl);
  border: 1px solid var(--color-border);
  box-shadow: var(--shadow-card);
  padding: 20px 16px;
  text-align: center;
  transition: all var(--transition);
}
.kpi-card:hover {
  box-shadow: var(--shadow-card-hover);
  transform: translateY(-2px);
}
.kpi-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 12px;
}
.kpi-value {
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 700;
  color: var(--color-text);
  line-height: 1;
}
.kpi-label {
  font-size: 12px;
  color: var(--color-text-muted);
  margin-top: 6px;
  font-weight: 500;
}

.chart-wrap {
  display: flex;
  align-items: center;
  gap: 24px;
  flex-wrap: wrap;
}
.donut-chart {
  width: 180px;
  height: 180px;
  flex-shrink: 0;
}
.donut-legend {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.legend-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
}
.legend-dot {
  width: 10px;
  height: 10px;
  border-radius: 3px;
  flex-shrink: 0;
}
.legend-label {
  color: var(--color-text-sec);
  flex: 1;
}
.legend-value {
  font-weight: 600;
  color: var(--color-text);
}

.bar-chart-wrap {
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.bar-row {
  display: grid;
  grid-template-columns: 120px 1fr 130px;
  align-items: center;
  gap: 12px;
}
.bar-label {
  font-size: 13px;
  color: var(--color-text-sec);
  font-weight: 500;
  text-align: right;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.bar-track {
  height: 8px;
  background: var(--color-surface);
  border-radius: 9999px;
  overflow: hidden;
}
.bar-fill {
  height: 100%;
  border-radius: 9999px;
  transition: width 0.6s cubic-bezier(0.4, 0, 0.2, 1);
}
.bar-amount {
  font-size: 13px;
  font-weight: 600;
  color: var(--color-text);
}
</style>
