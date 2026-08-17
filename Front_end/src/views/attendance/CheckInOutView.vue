<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">Chấm công</h1>
        <p style="margin: 4px 0 0; font-size: 14px; color: var(--color-text-muted)">
          {{ todayStr }}
        </p>
      </div>
      <a-space>
        <template v-if="isManagerOrAbove">
          <a-button @click="debugListLocal"><DatabaseOutlined /> List local</a-button>
          <a-button type="primary" @click="debugSync" :loading="syncing"
            ><SyncOutlined /> Sync từ HR</a-button
          >
        </template>
        <a-button @click="loadToday"><ReloadOutlined /> Làm mới</a-button>
      </a-space>
    </div>

    <!-- DEBUG PANEL: hiện danh sách local employee + trạng thái sync -->
    <a-alert
      v-if="debugInfo"
      :type="debugInfo.type"
      :message="debugInfo.title"
      style="margin-bottom: 16px"
      show-icon
      closable
      @close="debugInfo = null"
    >
      <template #description>
        <div style="font-size: 12px">
          <div>
            Local DB count: <b>{{ debugInfo.localCount }}</b>
          </div>
          <div>
            HR Service count: <b>{{ debugInfo.hrCount }}</b>
          </div>
          <div>
            Healthy: <b>{{ debugInfo.isHealthy }}</b>
          </div>
          <div v-if="debugInfo.message" style="margin-top: 4px">
            Message: <i>{{ debugInfo.message }}</i>
          </div>
          <div v-if="debugInfo.hasTarget !== null" style="margin-top: 6px; color: #d4380d">
            Employee {{ selectedEmpId }} có trong local DB:
            <b>{{ debugInfo.hasTarget ? 'CÓ' : 'KHÔNG ❌' }}</b>
          </div>
          <div
            v-if="debugInfo.localList && debugInfo.localList.length"
            style="margin-top: 6px; max-height: 120px; overflow: auto"
          >
            <b>Local IDs:</b>
            <div v-for="e in debugInfo.localList" :key="e.id" style="font-family: monospace">
              {{ e.id }} — {{ e.fullName }} ({{ e.employeeCode }}) — status: {{ e.status }}
            </div>
          </div>
        </div>
      </template>
    </a-alert>

    <a-row :gutter="[20, 20]">
      <!-- Clock panel -->
      <a-col :xs="24" :lg="10">
        <a-card :bordered="false" class="clock-card">
          <div class="clock-display">
            <div class="clock-time">{{ currentTime }}</div>
            <div class="clock-date">{{ todayStr }}</div>
          </div>

          <!-- Quy định thời gian làm việc hành chính -->
          <div class="shift-policy-banner">
            <div class="policy-header">
              <span class="policy-title">
                <InfoCircleOutlined /> Quy định ca làm việc hành chính
              </span>
            </div>
            <div class="policy-grid">
              <div class="policy-item">
                <span class="p-label">Bắt đầu</span>
                <span class="p-val">08:00</span>
              </div>
              <div class="policy-item">
                <span class="p-label">Kết thúc</span>
                <span class="p-val">17:30</span>
              </div>
              <div class="policy-item">
                <span class="p-label">Ân hạn muộn</span>
                <span class="p-val warning">15 phút</span>
              </div>
            </div>
            <div class="policy-note">
              * Đi muộn sau <b>08:15</b> hoặc về sớm trước <b>17:30</b> sẽ tính công theo tỷ lệ thời
              gian có mặt thực tế.
            </div>
          </div>

          <a-divider />

          <!-- Employee selector: chỉ hiện với Admin/HR/Manager -->
          <a-form layout="vertical">
            <template v-if="isManagerOrAbove">
              <a-form-item label="Nhân viên">
                <a-select
                  v-model:value="selectedEmpId"
                  show-search
                  placeholder="Chọn nhân viên..."
                  option-filter-prop="label"
                  style="width: 100%"
                  :loading="loadingEmps"
                  @change="onEmpChange"
                >
                  <a-select-option
                    v-for="e in employees"
                    :key="e.id"
                    :value="e.id"
                    :label="e.fullName"
                  >
                    <div style="display: flex; align-items: center; gap: 8px">
                      <a-avatar
                        size="small"
                        :style="{
                          background: getColor(e.fullName),
                          fontSize: '11px',
                          fontWeight: 700,
                        }"
                      >
                        {{ (e.fullName || 'N')[0] }}
                      </a-avatar>
                      {{ e.fullName }}
                      <span style="color: var(--color-text-muted); font-size: 12px"
                        >· {{ e.departmentName || '' }}</span
                      >
                    </div>
                  </a-select-option>
                </a-select>
              </a-form-item>
            </template>

            <!-- Với Employee: hiện card thông tin cá nhân thay dropdown -->
            <template v-else>
              <div class="self-employee-card">
                <a-avatar
                  size="large"
                  :style="{
                    background: getColor(auth.userName),
                    fontSize: '18px',
                    fontWeight: 700,
                    flexShrink: 0,
                  }"
                >
                  {{ (auth.userName || 'U')[0] }}
                </a-avatar>
                <div>
                  <div style="font-weight: 600; font-size: 15px; color: var(--color-text)">
                    {{ auth.userName }}
                  </div>
                  <div style="font-size: 12px; color: var(--color-text-muted); margin-top: 2px">
                    Chấm công của bạn hôm nay
                  </div>
                </div>
              </div>
            </template>

            <a-form-item label="Ghi chú (tuỳ chọn)" style="margin-top: 12px">
              <a-textarea v-model:value="note" :rows="2" placeholder="Ghi chú ca làm..." />
            </a-form-item>
          </a-form>

          <!-- Status summary (multiple sessions) -->
          <div v-if="todayRecords.length" class="today-status">
            <div class="status-row">
              <span class="status-label">Vào lần đầu</span>
              <span class="status-value success" v-if="todayFirstCheckIn">
                <CheckCircleOutlined /> {{ formatTime(todayFirstCheckIn) }}
                <span
                  style="font-size: 11px; margin-left: 4px"
                  :style="{
                    color: isLate(todayFirstCheckIn)
                      ? 'var(--color-warning,#fa8c16)'
                      : 'var(--color-primary)',
                  }"
                >
                  {{ isLate(todayFirstCheckIn) ? '● Đi muộn' : '● Đúng giờ' }}
                </span>
              </span>
              <span class="status-value muted" v-else>Chưa chấm</span>
            </div>
            <div class="status-row">
              <span class="status-label">Ra lần cuối</span>
              <span class="status-value" :class="todayStatus?.checkOutTime ? 'info' : 'muted'">
                <CheckCircleOutlined v-if="todayStatus?.checkOutTime" />
                {{
                  todayStatus?.checkOutTime
                    ? formatTime(todayStatus.checkOutTime)
                    : hasOpenSession
                      ? 'Đang làm việc'
                      : '—'
                }}
              </span>
            </div>
            <div class="status-row">
              <span class="status-label">Tổng giờ làm</span>
              <span class="status-value" :class="todayTotalHours > 0 ? 'success' : 'muted'">
                {{ todayTotalHours > 0 ? `${todayTotalHours}h` : '—' }}
              </span>
            </div>
            <div v-if="todayRecords.length > 1" class="status-row">
              <span class="status-label">Số phiên</span>
              <span class="status-value muted">{{ todayRecords.length }} lần chấm</span>
            </div>
          </div>

          <!-- Action buttons -->
          <div class="action-buttons">
            <a-button
              type="primary"
              size="large"
              block
              :loading="loadingIn"
              :disabled="!selectedEmpId || hasOpenSession"
              @click="doCheckIn"
              style="
                height: 48px;
                font-size: 15px;
                font-weight: 600;
                border-radius: 10px;
                margin-bottom: 10px;
              "
            >
              <LoginOutlined /> Check In
            </a-button>
            <a-button
              size="large"
              block
              :loading="loadingOut"
              :disabled="!selectedEmpId || !hasOpenSession"
              @click="doCheckOut"
              style="
                height: 48px;
                font-size: 15px;
                font-weight: 600;
                border-radius: 10px;
                border-color: var(--color-accent-blue);
                color: var(--color-accent-blue);
              "
            >
              <LogoutOutlined /> Check Out
            </a-button>
          </div>
        </a-card>
      </a-col>

      <!-- Today's attendance list -->
      <a-col :xs="24" :lg="14">
        <a-card title="Chấm công hôm nay" :bordered="false">
          <template #extra>
            <a-tag color="green"
              >{{ todayRecords.filter((r) => r.checkInTime).length }} đã check-in</a-tag
            >
          </template>
          <a-table
            :data-source="todayRecords"
            :columns="columns"
            :loading="loadingTable"
            row-key="id"
            size="small"
            :pagination="{ pageSize: 8 }"
            :scroll="{ x: 500 }"
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
              <template v-else-if="column.key === 'checkIn'">
                <span :class="record.checkInTime ? 'time-chip success' : 'time-chip muted'">
                  {{ record.checkInTime ? formatTime(record.checkInTime) : '—' }}
                </span>
              </template>
              <template v-else-if="column.key === 'checkOut'">
                <span :class="record.checkOutTime ? 'time-chip info' : 'time-chip muted'">
                  {{ record.checkOutTime ? formatTime(record.checkOutTime) : '—' }}
                </span>
              </template>
              <template v-else-if="column.key === 'hours'">
                <span
                  :style="{
                    fontWeight: 600,
                    color:
                      (record.workHours || 0) > 0 ? 'var(--color-primary)' : 'var(--color-error)',
                  }"
                >
                  {{ record.workHours != null ? `${record.workHours}h` : '—' }}
                </span>
              </template>
              <template v-else-if="column.key === 'status'">
                <span class="status-badge" :class="getAttendBadge(record)">
                  {{ getAttendLabel(record) }}
                </span>
              </template>
            </template>
          </a-table>
        </a-card>
      </a-col>
    </a-row>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { attendanceApi, employeeApi } from '@/api'
import { useAuthStore } from '@/stores/auth'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'
import {
  ReloadOutlined,
  LoginOutlined,
  LogoutOutlined,
  CheckCircleOutlined,
  DatabaseOutlined,
  SyncOutlined,
  InfoCircleOutlined,
} from '@ant-design/icons-vue'

const auth = useAuthStore()
// Admin, HR, Manager có quyền chọn nhân viên bất kỳ
const isManagerOrAbove = computed(() => auth.isAdmin || auth.isHR || auth.isManager)

const currentTime = ref(dayjs().format('HH:mm:ss'))
const todayStr = dayjs().format('dddd, DD/MM/YYYY')
const selectedEmpId = ref(null)
const note = ref('')
const employees = ref([])
const todayRecords = ref([])
const todayStatus = ref(null)
const loadingEmps = ref(false)
const loadingIn = ref(false)
const loadingOut = ref(false)
const loadingTable = ref(false)

// ─── Computed từ todayRecords (hỗ trợ nhiều phiên/ngày) ─────────
const hasOpenSession = computed(() => {
  const last = todayRecords.value.at(-1)
  return !!last?.checkInTime && !last?.checkOutTime
})
const todayFirstCheckIn = computed(
  () => todayRecords.value.find((r) => r.checkInTime)?.checkInTime || null,
)
const todayTotalHours = computed(() => {
  let total = 0
  for (const r of todayRecords.value) {
    if (r.checkInTime && r.checkOutTime)
      total += dayjs(r.checkOutTime).diff(dayjs(r.checkInTime), 'hour', true)
  }
  return Math.round(total * 10) / 10
})

let timer
onMounted(async () => {
  timer = setInterval(() => {
    currentTime.value = dayjs().format('HH:mm:ss')
  }, 1000)
  if (isManagerOrAbove.value) {
    // Admin/HR/Manager: load danh sách nhân viên để chọn
    await loadEmployees()
    await loadToday()
  } else {
    // Employee: tự động dùng ID của chính mình
    const myId = auth.userId
    if (myId) {
      selectedEmpId.value = myId
      // Thêm chính mình vào employees list để tra cứu tên
      employees.value = [{ id: myId, fullName: auth.userName }]
      await onEmpChange(myId)
    }
  }
})
onUnmounted(() => clearInterval(timer))

const colors = ['#00b14f', '#00b4d8', '#7c5cfc', '#ffb020', '#ff4757']
const getColor = (n) => colors[(n || '').charCodeAt(0) % colors.length]
const formatTime = (t) => (t ? dayjs(t).format('HH:mm') : '')

// ─── Quy định hành chính: check-in muộn hơn 08:00 → Đi muộn ──────────────
const LATE_HOUR = 8 // 08:00
const LATE_MIN = 0
function isLate(checkInTime) {
  if (!checkInTime) return false
  const d = dayjs(checkInTime)
  return d.hour() * 60 + d.minute() > LATE_HOUR * 60 + LATE_MIN
}

const columns = [
  { title: 'Nhân viên', key: 'employee', dataIndex: 'employeeName' },
  { title: 'Check In', key: 'checkIn', dataIndex: 'checkInTime', width: 100, align: 'center' },
  { title: 'Check Out', key: 'checkOut', dataIndex: 'checkOutTime', width: 100, align: 'center' },
  { title: 'Giờ làm', key: 'hours', width: 90, align: 'center' },
  { title: 'Trạng thái', key: 'status', width: 120 },
]

function getAttendBadge(r) {
  if (!r.checkInTime) return 'badge-error'
  if (isLate(r.checkInTime)) return 'badge-warning'
  if (!r.checkOutTime) return 'badge-default'
  return 'badge-success'
}
function getAttendLabel(r) {
  if (!r.checkInTime) return 'Vắng'
  if (isLate(r.checkInTime)) return 'Đi muộn'
  if (!r.checkOutTime) return 'Đang làm'
  return 'Đúng giờ'
}

async function loadEmployees() {
  loadingEmps.value = true
  try {
    // NOTE: auto-sync tạm tắt vì backend /api/employee-sync/* đang trả 405.
    // Sau khi backend Attendance (172.16.2.57) được restart + route hoạt động, bỏ comment dòng dưới để auto sync.
    // try {
    //   const syncRes = await attendanceApi.syncFromHr()
    //   console.log('[Attendance] auto-sync employees:', syncRes.data)
    // } catch (e) {
    //   console.warn('[Attendance] auto-sync failed (non-fatal):', e?.message)
    // }

    const res = await employeeApi.getAll({ pageSize: 200 })
    const data = res.data
    employees.value = Array.isArray(data) ? data : data.data || data.items || data.employees || []
  } catch {
    /* silent */
  } finally {
    loadingEmps.value = false
  }
}

async function loadToday() {
  if (selectedEmpId.value) {
    await onEmpChange(selectedEmpId.value)
  } else {
    todayRecords.value = []
    todayStatus.value = null
  }
}

async function onEmpChange(id) {
  if (!id) {
    todayStatus.value = null
    todayRecords.value = []
    return
  }
  loadingTable.value = true
  try {
    const today = dayjs()
    const todayStr = today.format('YYYY-MM-DD')
    const res = await attendanceApi.history(id, {
      year: today.year(),
      month: today.month() + 1,
    })
    const data = res.data
    const list = Array.isArray(data) ? data : data.data || data.items || []

    // Find employee name – support multiple field name conventions
    const emp = employees.value.find((e) => e.id === id)
    const empName = emp ? emp.fullName || emp.name || emp.firstName || '' : ''

    const mappedList = list.map((r) => {
      // date may be '2026-06-18T00:00:00' or '2026-06-18' – always take first 10 chars
      const datePart = r.date ? r.date.substring(0, 10) : todayStr
      // Backend trả TimeSpan UTC – thêm 'Z' để dayjs biết đây là UTC và convert sang giờ local
      // '00:00:00' = giá trị rỗng mà backend trả khi chưa chấm – cần lọc ra
      const isValidTime = (t) => t && t !== '00:00:00'
      const checkInTime = isValidTime(r.checkIn) ? `${datePart}T${r.checkIn.split('.')[0]}Z` : null
      const checkOutTime = isValidTime(r.checkOut)
        ? `${datePart}T${r.checkOut.split('.')[0]}Z`
        : null
      const workHours =
        checkInTime && checkOutTime
          ? Number(dayjs(checkOutTime).diff(dayjs(checkInTime), 'hour', true).toFixed(1))
          : null
      return {
        ...r,
        checkInTime,
        checkOutTime,
        employeeId: id,
        employeeName: empName,
        workHours,
      }
    })

    // Chỉ hiển thị bản ghi của ngày hôm nay trong bảng
    todayRecords.value = mappedList.filter((r) => {
      const rDate = r.date
        ? r.date.substring(0, 10)
        : r.checkInTime
          ? r.checkInTime.substring(0, 10)
          : null
      return rDate === todayStr
    })
    // Sắp xếp theo checkInTime tăng dần để bản ghi mới nhất ở cuối
    todayRecords.value.sort((a, b) => {
      if (!a.checkInTime) return -1
      if (!b.checkInTime) return 1
      return a.checkInTime.localeCompare(b.checkInTime)
    })
    // Lấy trạng thái hôm nay (bản ghi mới nhất nếu checkout nhiều lần)
    todayStatus.value = todayRecords.value.at(-1) || null
  } catch (e) {
    todayStatus.value = null
    todayRecords.value = []
  } finally {
    loadingTable.value = false
  }
}

async function doCheckIn() {
  loadingIn.value = true
  try {
    // Backend signature: CheckIn([FromBody] Guid employeeId, [FromQuery] int? shiftId)
    const res = await attendanceApi.checkIn(selectedEmpId.value, { shiftId: null })
    const checkData = res.data || {}
    message.success('Check-in thành công!')

    // ── Immediate UI update from check-in response ──────────────────────────
    const emp = employees.value.find((e) => e.id === selectedEmpId.value)
    const empName = emp ? emp.fullName || emp.name || emp.firstName || '' : selectedEmpId.value
    const datePart = dayjs().format('YYYY-MM-DD')
    // checkInTime từ backend là TimeSpan UTC, ví dụ: '10:40:01.6111201'
    // Thêm 'Z' để dayjs hiểu đây là UTC và hiển thị đúng giờ local
    const rawCI = checkData.checkInTime || ''
    const checkInTime = rawCI && rawCI !== '00:00:00' ? `${datePart}T${rawCI.split('.')[0]}Z` : null

    const immediateRecord = {
      id: checkData.attendanceLogId,
      employeeId: selectedEmpId.value,
      employeeName: empName,
      checkInTime,
      checkOutTime: null,
      status: checkData.status || 'OnTime',
      lateMinutes: checkData.lateMinutes || 0,
      workHours: null,
    }

    todayStatus.value = immediateRecord

    // Thêm phiên mới vào cuối (chỉ replace nếu cùng ID)
    const existIdx = todayRecords.value.findIndex((r) => r.id && r.id === immediateRecord.id)
    if (existIdx >= 0) {
      todayRecords.value = todayRecords.value.map((r, i) => (i === existIdx ? immediateRecord : r))
    } else {
      todayRecords.value = [...todayRecords.value, immediateRecord]
    }
    todayStatus.value = todayRecords.value.at(-1)
    // ──────────────────────────────────────────────────────────────────────────

    // Reload history from API to get the full fresh data
    await onEmpChange(selectedEmpId.value)
  } catch (e) {
    console.error('[CheckIn] full error:', e)
    console.error('[CheckIn] response data:', e.response?.data)
    console.error('[CheckIn] response status:', e.response?.status)
    console.error('[CheckIn] request payload:', { body: selectedEmpId.value, shiftId: null })

    const httpStatus = e.response?.status
    const serverMsg = e.response?.data?.message || e.response?.data?.title || ''

    if (httpStatus === 403) {
      message.error('Tài khoản nhân viên đã bị khoá. Liên hệ HR để được hỗ trợ.')
    } else if (
      serverMsg.toLowerCase().includes('shift not found') ||
      serverMsg.toLowerCase().includes('shift')
    ) {
      message.error('Chưa có ca làm việc nào được cấu hình. Liên hệ Admin thiết lập ca làm việc.')
    } else {
      message.error(
        'Check-in thất bại: ' + (serverMsg || JSON.stringify(e.response?.data) || e.message),
      )
    }
  } finally {
    loadingIn.value = false
  }
}

async function doCheckOut() {
  loadingOut.value = true
  try {
    const res = await attendanceApi.checkOut(selectedEmpId.value)
    const checkData = res.data || {}
    const workdayInfo =
      checkData.standardWorkday != null
        ? ` | Ngày công: ${checkData.standardWorkday.toFixed(1)}`
        : ''
    const otInfo =
      (checkData.overtimeHours || 0) > 0 ? ` | OT: ${checkData.overtimeHours.toFixed(1)}h` : ''
    message.success(`Check-out thành công!${workdayInfo}${otInfo}`)

    // ── Immediate UI update from check-out response ─────────────────────────
    const datePart = dayjs().format('YYYY-MM-DD')
    // checkOutTime từ backend là TimeSpan UTC – thêm 'Z' để dayjs convert sang local
    const rawCO = checkData.checkOutTime || ''
    const checkOutTime =
      rawCO && rawCO !== '00:00:00' ? `${datePart}T${rawCO.split('.')[0]}Z` : null
    // Tìm phiên đang mở (checkIn nhưng chưa checkOut) từ cuối lên
    let openIdx = -1
    for (let i = todayRecords.value.length - 1; i >= 0; i--) {
      if (todayRecords.value[i].checkInTime && !todayRecords.value[i].checkOutTime) {
        openIdx = i
        break
      }
    }
    if (openIdx >= 0) {
      const openRec = todayRecords.value[openIdx]
      const updated = { ...openRec, checkOutTime }
      if (checkOutTime && updated.checkInTime) {
        updated.workHours = Number(
          dayjs(checkOutTime).diff(dayjs(updated.checkInTime), 'hour', true).toFixed(1),
        )
      }
      todayRecords.value = todayRecords.value.map((r, i) => (i === openIdx ? updated : r))
      todayStatus.value = todayRecords.value.at(-1)
    }
    // ──────────────────────────────────────────────────────────────────────────

    await onEmpChange(selectedEmpId.value)
  } catch (e) {
    console.error('[CheckOut] full error:', e)
    console.error('[CheckOut] response data:', e.response?.data)
    console.error('[CheckOut] request payload:', { body: selectedEmpId.value })
    const httpStatus = e.response?.status
    const serverMsg = e.response?.data?.message || e.response?.data?.title || ''

    if (httpStatus === 403) {
      message.error('Tài khoản nhân viên đã bị khoá. Liên hệ HR để được hỗ trợ.')
    } else {
      message.error(
        'Check-out thất bại: ' + (serverMsg || JSON.stringify(e.response?.data) || e.message),
      )
    }
  } finally {
    loadingOut.value = false
  }
}

// ─── DEBUG: kiểm tra Attendance local DB ─────────────────────────────────────
const debugInfo = ref(null)
const syncing = ref(false)

async function debugListLocal() {
  try {
    const [statusRes, listRes] = await Promise.all([
      attendanceApi.getSyncStatus(),
      attendanceApi.listLocalEmployees(),
    ])
    const status = statusRes.data?.data || statusRes.data
    const list = listRes.data?.data || listRes.data || []
    const hasTarget = selectedEmpId.value ? list.some((e) => e.id === selectedEmpId.value) : null

    debugInfo.value = {
      type: hasTarget === false ? 'error' : 'info',
      title: 'Debug: Attendance local DB',
      localCount: status.localEmployeeCount ?? list.length,
      hrCount: status.hrEmployeeCount ?? '?',
      isHealthy: status.isHealthy ?? '?',
      message: status.message || '',
      hasTarget,
      localList: list.slice(0, 30),
    }
    console.log('[DEBUG] Attendance local employees:', list)
    console.log('[DEBUG] Sync status:', status)
  } catch (e) {
    console.error('[DEBUG] listLocal failed:', e)
    message.error('Không gọi được /employee-sync endpoints: ' + e.message)
  }
}

async function debugSync() {
  syncing.value = true
  try {
    const res = await attendanceApi.syncFromHr()
    const data = res.data || {}
    message.success(data.message || `Synced ${data.syncedCount ?? 0} employees`)
    await debugListLocal()
  } catch (e) {
    console.error('[DEBUG] sync failed:', e)
    message.error('Sync thất bại: ' + (e.response?.data?.message || e.message))
  } finally {
    syncing.value = false
  }
}
</script>

<style scoped>
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 24px;
}

.clock-card {
  text-align: center;
}
.clock-display {
  padding: 20px 0 8px;
}

.shift-policy-banner {
  background: linear-gradient(135deg, rgba(0, 180, 216, 0.06), rgba(76, 92, 252, 0.06));
  border: 1px solid rgba(0, 180, 216, 0.18);
  border-radius: var(--radius-md);
  padding: 12px 14px;
  margin: 12px 0 16px;
  text-align: left;
}
.policy-header {
  display: flex;
  align-items: center;
  margin-bottom: 8px;
  font-weight: 600;
  font-size: 13px;
  color: var(--color-accent-blue);
}
.policy-title {
  display: flex;
  align-items: center;
  gap: 6px;
}
.policy-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
  margin-bottom: 6px;
}
.policy-item {
  display: flex;
  flex-direction: column;
  background: var(--color-surface);
  padding: 6px 8px;
  border-radius: var(--radius-sm);
  border: 1px solid var(--color-border);
}
.p-label {
  font-size: 10px;
  color: var(--color-text-muted);
  margin-bottom: 2px;
}
.p-val {
  font-size: 13px;
  font-weight: 700;
  color: var(--color-text);
}
.p-val.warning {
  color: var(--color-warning);
}
.policy-note {
  font-size: 11px;
  color: var(--color-text-muted);
  line-height: 1.3;
}
.clock-time {
  font-family: var(--font-display);
  font-size: 52px;
  font-weight: 700;
  color: var(--color-text);
  letter-spacing: -1px;
  line-height: 1;
}
.clock-date {
  margin-top: 8px;
  font-size: 14px;
  color: var(--color-text-muted);
}

.today-status {
  background: var(--color-surface);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  padding: 14px 16px;
  margin-bottom: 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.status-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.status-label {
  font-size: 13px;
  color: var(--color-text-muted);
  font-weight: 500;
}
.status-value {
  font-size: 13px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 5px;
}
.status-value.success {
  color: var(--color-primary);
}
.status-value.info {
  color: var(--color-accent-blue);
}
.status-value.muted {
  color: var(--color-text-muted);
  font-weight: 400;
}

.action-buttons {
  margin-top: 4px;
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

/* Card nhân viên tự chấm công (không có dropdown) */
.self-employee-card {
  display: flex;
  align-items: center;
  gap: 14px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 14px 16px;
  margin-bottom: 4px;
}
</style>
