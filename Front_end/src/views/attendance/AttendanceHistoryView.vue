<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">Lịch sử chấm công</h1>
        <p style="margin: 4px 0 0; font-size: 14px; color: var(--color-text-muted)">
          Tra cứu lịch sử và tổng hợp tháng
        </p>
      </div>
      <a-button @click="loadHistory"><ReloadOutlined /> Làm mới</a-button>
    </div>

    <!-- Quy định thời gian làm việc hành chính -->
    <div class="policy-alert-bar">
      <InfoCircleOutlined /> <b>Quy định ca hành chính:</b> Sáng 08:00 – Chiều 17:30 (Ân hạn đi muộn
      15 phút, đi muộn tính từ 08:16)
    </div>

    <!-- Filters -->
    <a-card :bordered="false" style="margin-bottom: 20px">
      <a-row :gutter="[16, 12]" align="middle">
        <!-- Dropdown chỉ hiện với Admin/HR/Manager -->
        <a-col v-if="isManagerOrAbove" :xs="24" :sm="10" :lg="7">
          <a-select
            v-model:value="selectedEmpId"
            show-search
            placeholder="Chọn nhân viên..."
            option-filter-prop="label"
            style="width: 100%"
            allow-clear
            :loading="loadingEmps"
            @change="loadHistory"
          >
            <a-select-option v-for="e in employees" :key="e.id" :value="e.id" :label="e.fullName">
              {{ e.fullName }}
              <span style="color: var(--color-text-muted); font-size: 12px">
                · {{ e.departmentName || '' }}</span
              >
            </a-select-option>
          </a-select>
        </a-col>

        <!-- Với Employee: hiện tên của chính mình -->
        <a-col v-else :xs="24" :sm="10" :lg="7">
          <div class="self-info-chip">
            <a-avatar
              size="small"
              :style="{ background: getColor(auth.userName), fontSize: '11px', fontWeight: 700 }"
            >
              {{ (auth.userName || 'U')[0] }}
            </a-avatar>
            <span style="font-weight: 600; font-size: 14px">{{ auth.userName }}</span>
            <span style="font-size: 12px; color: var(--color-text-muted)">— lịch sử của tôi</span>
          </div>
        </a-col>

        <a-col :xs="24" :sm="10" :lg="7">
          <a-range-picker
            v-model:value="dateRange"
            format="DD/MM/YYYY"
            :placeholder="['Từ ngày', 'Đến ngày']"
            style="width: 100%"
            @change="loadHistory"
          />
        </a-col>
        <a-col :xs="24" :sm="4" :lg="4">
          <a-button type="primary" @click="loadMonthlySummary" :disabled="!selectedEmpId">
            <BarChartOutlined /> Tổng hợp tháng
          </a-button>
        </a-col>
      </a-row>
    </a-card>

    <!-- Summary cards (when month summary loaded) -->
    <a-row v-if="summary" :gutter="[16, 16]" style="margin-bottom: 20px">
      <a-col :xs="12" :sm="6" v-for="s in summaryCards" :key="s.label">
        <div class="summary-card">
          <div class="sum-value" :style="{ color: s.color }">{{ s.value }}</div>
          <div class="sum-label">{{ s.label }}</div>
        </div>
      </a-col>
    </a-row>

    <!-- Main table -->
    <a-card :bordered="false">
      <template #title>
        Bảng chấm công
        <a-tag v-if="records.length" style="margin-left: 8px">{{ records.length }} bản ghi</a-tag>
      </template>
      <a-table
        :data-source="records"
        :columns="columns"
        :loading="loading"
        row-key="id"
        size="small"
        :pagination="{ pageSize: 12, showSizeChanger: true }"
        :scroll="{ x: 1100 }"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'date'">
            <span style="font-weight: 600">{{
              formatDate(record.date || record.checkInTime)
            }}</span>
          </template>
          <template v-else-if="column.key === 'checkIn'">
            <span class="time-chip" :class="record.checkInTime ? 'success' : 'muted'">
              {{ record.checkInTime ? formatTime(record.checkInTime) : '—' }}
            </span>
          </template>
          <template v-else-if="column.key === 'checkOut'">
            <span class="time-chip" :class="record.checkOutTime ? 'info' : 'muted'">
              {{ record.checkOutTime ? formatTime(record.checkOutTime) : '—' }}
            </span>
          </template>
          <template v-else-if="column.key === 'hours'">
            <span :style="{ fontWeight: 600, color: getHoursColor(record.workHours) }">
              {{ record.workHours != null ? `${record.workHours}h` : '—' }}
            </span>
          </template>
          <template v-else-if="column.key === 'status'">
            <span class="status-badge" :class="getBadgeClass(record)">
              {{ getStatusLabel(record) }}
            </span>
          </template>
          <template v-else-if="column.key === 'shift'">
            <span style="font-size: 12px; color: var(--color-text-muted)">{{
              record.shiftName || '—'
            }}</span>
          </template>
          <template v-else-if="column.key === 'late'">
            <span
              v-if="record.lateCheckInMinutes > 0"
              style="color: var(--color-warning); font-weight: 600; font-size: 12px"
            >
              +{{ record.lateCheckInMinutes }}p
            </span>
            <span v-else style="color: var(--color-text-muted); font-size: 12px">—</span>
          </template>
          <template v-else-if="column.key === 'early'">
            <span
              v-if="record.earlyCheckOutMinutes > 0"
              style="color: var(--color-error); font-weight: 600; font-size: 12px"
            >
              -{{ record.earlyCheckOutMinutes }}p
            </span>
            <span v-else style="color: var(--color-text-muted); font-size: 12px">—</span>
          </template>
          <template v-else-if="column.key === 'workday'">
            <span
              :style="{
                fontWeight: 700,
                fontSize: '13px',
                color: !record.standardWorkday
                  ? 'var(--color-text-muted)'
                  : record.standardWorkday >= 1
                    ? 'var(--color-primary)'
                    : record.standardWorkday >= 0.5
                      ? 'var(--color-warning)'
                      : 'var(--color-error)',
              }"
            >
              {{ record.standardWorkday != null ? record.standardWorkday.toFixed(1) : '—' }}
            </span>
          </template>
        </template>
      </a-table>
    </a-card>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { attendanceApi, employeeApi } from '@/api'
import { useAuthStore } from '@/stores/auth'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'
import { ReloadOutlined, BarChartOutlined, InfoCircleOutlined } from '@ant-design/icons-vue'

const auth = useAuthStore()
const isManagerOrAbove = computed(() => auth.isAdmin || auth.isHR || auth.isManager)

const loading = ref(false)
const loadingEmps = ref(false)
const employees = ref([])
const records = ref([])
const summary = ref(null)
const selectedEmpId = ref(null)
const dateRange = ref([dayjs().startOf('month'), dayjs()])

const colors = ['#00b14f', '#00b4d8', '#7c5cfc', '#ffb020', '#ff4757']
const getColor = (n) => colors[(n || '').charCodeAt(0) % colors.length]

const formatDate = (t) => (t ? dayjs(t).format('DD/MM/YYYY (ddd)') : '—')
const formatTime = (t) => (t ? dayjs(t).format('HH:mm') : '—')
const getHoursColor = (h) =>
  h == null
    ? 'var(--color-text-muted)'
    : h === 0
      ? 'var(--color-error)'
      : h < 6
        ? 'var(--color-error)'
        : h < 8
          ? 'var(--color-warning)'
          : 'var(--color-primary)'

const columns = [
  {
    title: 'Ngày',
    key: 'date',
    width: 170,
    sorter: (a, b) => dayjs(a.date || '').unix() - dayjs(b.date || '').unix(),
  },
  { title: 'Ca làm', key: 'shift', width: 110 },
  { title: 'Check In', key: 'checkIn', width: 90, align: 'center' },
  { title: 'Check Out', key: 'checkOut', width: 90, align: 'center' },
  {
    title: 'Giờ làm',
    key: 'hours',
    width: 80,
    align: 'center',
    sorter: (a, b) => (a.workHours || 0) - (b.workHours || 0),
  },
  { title: 'Đi muộn', key: 'late', width: 80, align: 'center' },
  { title: 'Về sớm', key: 'early', width: 80, align: 'center' },
  {
    title: 'Ngày công',
    key: 'workday',
    width: 90,
    align: 'center',
    sorter: (a, b) => (a.standardWorkday || 0) - (b.standardWorkday || 0),
  },
  { title: 'Trạng thái', key: 'status', width: 110 },
  { title: 'Ghi chú', dataIndex: 'note', ellipsis: true },
]

const summaryCards = computed(() =>
  summary.value
    ? [
        {
          label: 'Ngày công',
          value:
            summary.value.presentDays != null
              ? `${summary.value.presentDays}/${summary.value.workingDays}`
              : '—',
          color: 'var(--color-primary)',
        },
        {
          label: 'Tổng giờ',
          value: summary.value.totalHours != null ? `${summary.value.totalHours}h` : '—',
          color: 'var(--color-accent-blue)',
        },
        { label: 'Đi muộn', value: summary.value.lateCount ?? 0, color: 'var(--color-warning)' },
        { label: 'Vắng', value: summary.value.absentCount ?? 0, color: 'var(--color-error)' },
      ]
    : [],
)

function getBadgeClass(r) {
  if (!r.checkInTime) return 'badge-error'
  if (r.status === 'Late') return 'badge-warning'
  if (!r.checkOutTime) return 'badge-warning'
  if ((r.workHours || 0) < 6) return 'badge-warning'
  return 'badge-success'
}
function getStatusLabel(r) {
  if (!r.checkInTime) return 'Vắng'
  if (r.status === 'Late') return 'Đi muộn'
  if (!r.checkOutTime) return 'Chưa checkout'
  if ((r.workHours || 0) < 6) return 'Thiếu giờ'
  return 'Đầy đủ'
}

// Helper: chuyển TimeSpan UTC từ backend sang datetime string local
// '00:00:00' là giá trị rỗng (C# default TimeSpan), cần lọc ra
function parseBackendTime(datePart, timeSpan) {
  if (!timeSpan || timeSpan === '00:00:00') return null
  // Thêm 'Z' để dayjs biết đây là UTC → tự convert sang local
  return `${datePart}T${timeSpan.split('.')[0]}Z`
}

async function loadEmployees() {
  loadingEmps.value = true
  try {
    const res = await employeeApi.getAll({ pageSize: 200 })
    const data = res.data
    employees.value = Array.isArray(data) ? data : data.data || data.items || data.employees || []
  } catch {
  } finally {
    loadingEmps.value = false
  }
}

async function loadHistory() {
  if (!selectedEmpId.value) {
    records.value = []
    return
  }
  loading.value = true
  try {
    const params = {}
    if (dateRange.value?.[0]) {
      params.year = dateRange.value[0].year()
      params.month = dateRange.value[0].month() + 1
    }
    const res = await attendanceApi.history(selectedEmpId.value, params)
    const data = res.data
    const list = Array.isArray(data) ? data : data.data || data.items || []

    // Tra cứu tên nhân viên
    const emp = employees.value.find((e) => e.id === selectedEmpId.value)
    const empName = emp ? emp.fullName || emp.name || emp.firstName || '' : auth.userName

    records.value = list.map((r) => {
      const datePart = r.date ? r.date.substring(0, 10) : dayjs().format('YYYY-MM-DD')
      const checkInTime = parseBackendTime(datePart, r.checkIn)
      const checkOutTime = parseBackendTime(datePart, r.checkOut)
      const workHours =
        checkInTime && checkOutTime
          ? Number(dayjs(checkOutTime).diff(dayjs(checkInTime), 'hour', true).toFixed(1))
          : null
      return {
        ...r,
        checkInTime,
        checkOutTime,
        employeeId: selectedEmpId.value,
        employeeName: empName,
        workHours,
      }
    })
    // Tự động load summary của tháng/năm đang lọc
    loadMonthlySummary()
  } catch (e) {
    if (e.response && e.response.status === 404) {
      // 404 là bình thường khi chưa có dữ liệu chấm công cho tháng này
      records.value = []
      loadMonthlySummary()
    } else {
      console.error('[History] load failed:', e)
      message.error('Không tải được lịch sử chấm công')
    }
  } finally {
    loading.value = false
  }
}

async function loadMonthlySummary() {
  if (!selectedEmpId.value) return
  try {
    const dateVal = dateRange.value?.[0] || dayjs()
    const m = dateVal.month() + 1
    const y = dateVal.year()
    const res = await attendanceApi.monthlySummary(selectedEmpId.value, m, y)
    summary.value = res.data?.data || res.data
  } catch (e) {
    if (e.response && e.response.status === 404) {
      summary.value = null
    } else {
      console.error('[Summary] load failed:', e)
      message.error('Không tải được tổng hợp tháng')
    }
  }
}

onMounted(async () => {
  if (isManagerOrAbove.value) {
    // Admin/HR/Manager: load danh sách nhân viên, chờ chọn để xem lịch sử
    await loadEmployees()
    // Nếu đã có nhân viên được chọn trước, tự load
    if (selectedEmpId.value) await loadHistory()
  } else {
    // Employee: tự động dùng ID của chính mình
    const myId = auth.userId
    if (myId) {
      selectedEmpId.value = myId
      employees.value = [{ id: myId, fullName: auth.userName }]
      await loadHistory()
    }
  }
})
</script>

<style scoped>
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 24px;
}

.policy-alert-bar {
  background: linear-gradient(135deg, rgba(0, 180, 216, 0.05), rgba(76, 92, 252, 0.05));
  border: 1px solid rgba(0, 180, 216, 0.15);
  color: var(--color-text);
  padding: 10px 16px;
  border-radius: var(--radius-md);
  margin-bottom: 16px;
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
}
.policy-alert-bar :deep(.anticon) {
  color: var(--color-accent-blue);
  font-size: 16px;
}

.self-info-chip {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
}

.summary-card {
  background: #fff;
  border-radius: var(--radius-lg);
  border: 1px solid var(--color-border);
  box-shadow: var(--shadow-card);
  padding: 16px 20px;
  text-align: center;
}
.sum-value {
  font-family: var(--font-display);
  font-size: 28px;
  font-weight: 700;
  line-height: 1;
}
.sum-label {
  font-size: 12px;
  color: var(--color-text-muted);
  margin-top: 6px;
  font-weight: 500;
}
.time-chip {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 9999px;
  font-size: 12px;
  font-weight: 600;
}
.time-chip.success {
  background: rgba(0, 177, 79, 0.1);
  color: var(--color-primary);
}
.time-chip.info {
  background: rgba(0, 180, 216, 0.1);
  color: var(--color-accent-blue);
}
.time-chip.muted {
  background: var(--color-surface);
  color: var(--color-text-muted);
}
</style>
