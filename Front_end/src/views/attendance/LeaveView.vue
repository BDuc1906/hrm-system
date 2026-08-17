<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">Quản lý nghỉ phép</h1>
        <p style="margin: 4px 0 0; font-size: 14px; color: var(--color-text-muted)">
          Đăng ký và duyệt đơn nghỉ phép
        </p>
      </div>
      <a-button type="primary" @click="openRequest"><PlusOutlined /> Đăng ký nghỉ phép</a-button>
    </div>

    <a-tabs v-model:activeKey="activeTab" @change="onTabChange">
      <!-- Pending approvals — Admin, HR, Manager -->
      <a-tab-pane v-if="auth.isAdmin || auth.isHR || auth.isManager" key="pending" tab="Chờ duyệt">
        <template #tab>
          <span>
            Chờ duyệt
            <a-badge
              v-if="pending.length"
              :count="pending.length"
              :overflow-count="99"
              style="margin-left: 6px"
            />
          </span>
        </template>
        <a-card :bordered="false">
          <a-table
            :data-source="pending"
            :columns="pendingColumns"
            :loading="loadingPending"
            row-key="id"
            :pagination="{ pageSize: 8 }"
            :scroll="{ x: 900 }"
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
              <template v-else-if="column.key === 'dates'">
                <div style="font-size: 13px">
                  <div>{{ formatDate(record.startDate) }}</div>
                  <div style="color: var(--color-text-muted); font-size: 11px">
                    → {{ formatDate(record.endDate) }}
                  </div>
                </div>
              </template>
              <template v-else-if="column.key === 'days'">
                <span class="status-badge badge-info"
                  >{{ record.leaveDays || record.days || '?' }} ngày</span
                >
              </template>
              <template v-else-if="column.key === 'type'">
                <a-tag :color="getLeaveTypeColor(getLeaveType(record))">{{
                  getLeaveType(record)
                }}</a-tag>
              </template>
              <template v-else-if="column.key === 'actions'">
                <a-space>
                  <a-button
                    size="small"
                    type="primary"
                    :loading="approvingId === record.id"
                    @click="approve(record.id)"
                  >
                    <CheckOutlined /> Duyệt
                  </a-button>
                  <a-popconfirm
                    title="Từ chối đơn này?"
                    ok-text="Từ chối"
                    cancel-text="Huỷ"
                    ok-type="danger"
                    @confirm="reject(record.id)"
                  >
                    <a-button size="small" danger :loading="rejectingId === record.id">
                      <CloseOutlined /> Từ chối
                    </a-button>
                  </a-popconfirm>
                </a-space>
              </template>
            </template>
          </a-table>
        </a-card>
      </a-tab-pane>

      <!-- My leave requests -->
      <a-tab-pane key="my" tab="Đơn của tôi">
        <a-card :bordered="false">
          <a-row :gutter="[12, 12]" style="margin-bottom: 16px">
            <!-- Với Admin/HR/Manager: dropdown chọn nhân viên -->
            <a-col :xs="24" :sm="12" :lg="8">
              <a-select
                v-if="isManagerOrAbove"
                v-model:value="myEmpId"
                show-search
                placeholder="Chọn nhân viên..."
                option-filter-prop="label"
                style="width: 100%"
                allow-clear
                @change="loadMyLeaves"
              >
                <a-select-option
                  v-for="e in employees"
                  :key="e.id"
                  :value="e.id"
                  :label="e.fullName"
                >
                  {{ e.fullName }}
                </a-select-option>
              </a-select>
              <!-- Với Employee: hiện tên bản thân -->
              <span v-else style="font-size: 13px; color: var(--color-text-muted)">
                Chỉ hiển thị đơn của bạn
              </span>
            </a-col>
          </a-row>
          <a-table
            :data-source="myLeaves"
            :columns="myColumns"
            :loading="loadingMy"
            row-key="id"
            :pagination="{ pageSize: 8 }"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'dates'">
                <div style="font-size: 13px">
                  <div>{{ formatDate(record.startDate) }}</div>
                  <div style="color: var(--color-text-muted); font-size: 11px">
                    → {{ formatDate(record.endDate) }}
                  </div>
                </div>
              </template>
              <template v-else-if="column.key === 'days'">
                <span class="status-badge badge-info"
                  >{{ record.leaveDays || record.days || '?' }} ngày</span
                >
              </template>
              <template v-else-if="column.key === 'status'">
                <span class="status-badge" :class="getStatusBadge(record.status)">
                  {{ getStatusLabel(record.status) }}
                </span>
              </template>
              <template v-else-if="column.key === 'type'">
                <a-tag :color="getLeaveTypeColor(getLeaveType(record))">{{
                  getLeaveType(record)
                }}</a-tag>
              </template>
            </template>
          </a-table>
        </a-card>
      </a-tab-pane>
    </a-tabs>

    <!-- Request Modal -->
    <a-modal
      v-model:open="modalOpen"
      title="Đăng ký nghỉ phép"
      :confirm-loading="saving"
      ok-text="Gửi đơn"
      cancel-text="Huỷ"
      @ok="submitRequest"
      destroy-on-close
    >
      <a-form :model="form" layout="vertical" ref="formRef" :rules="rules">
        <!-- Dropdown chọn nhân viên: chỉ hiện với Admin/HR/Manager -->
        <a-form-item v-if="isManagerOrAbove" name="employeeId" label="Nhân viên">
          <a-select
            v-model:value="form.employeeId"
            show-search
            option-filter-prop="label"
            placeholder="Chọn nhân viên..."
            style="width: 100%"
          >
            <a-select-option v-for="e in employees" :key="e.id" :value="e.id" :label="e.fullName">
              {{ e.fullName }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <!-- Với Employee: hiện rõ là đang tạo đơn cho chính mình -->
        <a-form-item v-else label="Nhân viên">
          <a-input :value="auth.userName" disabled />
        </a-form-item>
        <a-form-item name="leaveType" label="Loại nghỉ">
          <a-select v-model:value="form.leaveType" style="width: 100%">
            <a-select-option value="Phép năm">Phép năm</a-select-option>
            <a-select-option value="Nghỉ bệnh">Nghỉ bệnh</a-select-option>
            <a-select-option value="Nghỉ không lương">Nghỉ không lương</a-select-option>
            <a-select-option value="Nghỉ thai sản">Nghỉ thai sản</a-select-option>
            <a-select-option value="Nghỉ lễ">Nghỉ lễ</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item name="dateRange" label="Thời gian nghỉ">
          <a-range-picker
            v-model:value="form.dateRange"
            format="DD/MM/YYYY"
            :placeholder="['Ngày bắt đầu', 'Ngày kết thúc']"
            style="width: 100%"
          />
        </a-form-item>
        <a-form-item name="reason" label="Lý do">
          <a-textarea
            v-model:value="form.reason"
            :rows="3"
            placeholder="Nêu rõ lý do nghỉ phép..."
          />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { leaveApi, employeeApi } from '@/api'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'
import { PlusOutlined, CheckOutlined, CloseOutlined } from '@ant-design/icons-vue'

const auth = useAuthStore()
const isManagerOrAbove = computed(() => auth.isAdmin || auth.isHR || auth.isManager)

const activeTab = ref('pending')
const pending = ref([])
const myLeaves = ref([])
const employees = ref([])
const loadingPending = ref(false)
const loadingMy = ref(false)
const modalOpen = ref(false)
const saving = ref(false)
const approvingId = ref(null)
const rejectingId = ref(null)
const myEmpId = ref(null)
const formRef = ref()

const form = reactive({ employeeId: null, leaveType: 'Phép năm', dateRange: null, reason: '' })
// employeeId chỉ bắt buộc với Admin/HR/Manager (Employee tự động lấy từ auth)
const rules = computed(() => ({
  employeeId: isManagerOrAbove.value ? [{ required: true, message: 'Chọn nhân viên' }] : [],
  dateRange: [{ required: true, message: 'Chọn ngày nghỉ' }],
  reason: [{ required: true, message: 'Nhập lý do nghỉ' }],
}))

const formatDate = (t) => (t ? dayjs(t).format('DD/MM/YYYY') : '—')
const getColor = (n) =>
  ['#00b14f', '#00b4d8', '#7c5cfc', '#ffb020', '#ff4757'][(n || '').charCodeAt(0) % 5]
const getLeaveTypeColor = (t) =>
  ({
    'Phép năm': 'green',
    'Nghỉ bệnh': 'orange',
    'Nghỉ không lương': 'default',
    'Nghỉ thai sản': 'purple',
    'Nghỉ lễ': 'blue',
    Annual: 'green',
    Sick: 'orange',
    Unpaid: 'default',
    Maternity: 'purple',
  })[t] || 'default'
// Helper: thử các tên field khác nhau mà backend có thể trả
const getLeaveType = (r) =>
  r.leaveType || r.leaveTypeName || r.type || r.leave_type || r.leaveTypeDisplay || '—'
const getStatusBadge = (s) =>
  ({ Approved: 'badge-success', Pending: 'badge-warning', Rejected: 'badge-error' })[s] ||
  'badge-default'
const getStatusLabel = (s) =>
  ({ Approved: 'Đã duyệt', Pending: 'Chờ duyệt', Rejected: 'Từ chối' })[s] || s || 'Chờ duyệt'

// Tính số ngày lịch (calendar days) từ ngày bắt đầu đến ngày kết thúc, bao gồm cả 2 đầu
// VD: 19/06 → 21/06 = 3 ngày (kể cả T7, CN — chuẩn hệ thống nghỉ phép)
function calcLeaveDays(startDate, endDate) {
  if (!startDate || !endDate) return null
  const start = dayjs(startDate).startOf('day')
  const end = dayjs(endDate).startOf('day')
  const days = end.diff(start, 'day') + 1
  return days > 0 ? days : 1
}

const pendingColumns = [
  { title: 'Nhân viên', key: 'employee', dataIndex: 'employeeName', minWidth: 140 },
  { title: 'Loại nghỉ', key: 'type', width: 120 },
  { title: 'Thời gian', key: 'dates', width: 160 },
  { title: 'Số ngày', key: 'days', width: 95, align: 'center' },
  { title: 'Lý do', dataIndex: 'reason', ellipsis: true, minWidth: 100 },
  { title: 'Hành động', key: 'actions', width: 185, align: 'center', fixed: 'right' },
]
const myColumns = [
  { title: 'Loại nghỉ', key: 'type', width: 130 },
  { title: 'Thời gian', key: 'dates', width: 150 },
  { title: 'Số ngày', key: 'days', width: 90, align: 'center' },
  { title: 'Lý do', dataIndex: 'reason', ellipsis: true },
  { title: 'Trạng thái', key: 'status', width: 120 },
]

async function loadEmployees() {
  try {
    const res = await leaveApi.getEmployees()
    const data = res.data
    employees.value = Array.isArray(data) ? data : data.data || data.items || data.employees || []
    if (!employees.value.length) {
      const res2 = await employeeApi.getAll({ pageSize: 200 })
      const d2 = res2.data
      employees.value = Array.isArray(d2) ? d2 : d2.data || d2.items || d2.employees || []
    }
  } catch {
    try {
      const res2 = await employeeApi.getAll({ pageSize: 200 })
      const d2 = res2.data
      employees.value = Array.isArray(d2) ? d2 : d2.data || d2.items || d2.employees || []
    } catch {}
  }
}

async function loadPending() {
  loadingPending.value = true
  try {
    const res = await leaveApi.pendingList()
    const data = res.data
    let list = Array.isArray(data) ? data : data.data || data.items || []

    // Manager chỉ thấy đơn của nhân viên thuộc cùng phòng ban
    if (auth.isManager && auth.departmentId) {
      const myDeptEmpIds = new Set(
        employees.value.filter((e) => e.departmentId === auth.departmentId).map((e) => e.id),
      )
      list = list.filter((item) => myDeptEmpIds.has(item.employeeId))
    }

    pending.value = list.map((r) => ({
      ...r,
      // Tính số ngày làm việc nếu backend không trả
      leaveDays: r.leaveDays || r.days || calcLeaveDays(r.startDate, r.endDate),
    }))
  } catch {
    pending.value = []
  } finally {
    loadingPending.value = false
  }
}

async function loadMyLeaves() {
  const empId = auth.isEmployee ? auth.userId : myEmpId.value
  if (!empId) {
    myLeaves.value = []
    return
  }
  loadingMy.value = true
  try {
    const res = await leaveApi.getByEmployee(empId)
    const data = res.data
    myLeaves.value = (Array.isArray(data) ? data : data.data || data.items || []).map((r) => ({
      ...r,
      leaveDays: r.leaveDays || r.days || calcLeaveDays(r.startDate, r.endDate),
    }))
  } catch {
    myLeaves.value = []
  } finally {
    loadingMy.value = false
  }
}

function onTabChange(key) {
  if (key === 'pending') loadPending()
  else if (key === 'my') loadMyLeaves()
}

function openRequest() {
  // Employee: tự động điền ID của chính mình, không hiện dropdown
  const defaultEmpId = isManagerOrAbove.value ? null : auth.userId || null
  Object.assign(form, {
    employeeId: defaultEmpId,
    leaveType: 'Phép năm',
    dateRange: null,
    reason: '',
  })
  modalOpen.value = true
}

async function submitRequest() {
  // Với Employee: validate bỏ qua trường employeeId (tự động lấy từ auth)
  if (isManagerOrAbove.value) {
    try {
      await formRef.value.validate()
    } catch {
      return
    }
  } else {
    try {
      await formRef.value.validate(['leaveType', 'dateRange', 'reason'])
    } catch {
      return
    }
    form.employeeId = auth.userId
  }
  if (!form.employeeId) {
    message.error('Không xác định được nhân viên')
    return
  }
  saving.value = true
  try {
    const payload = {
      employeeId: form.employeeId,
      leaveType: form.leaveType,
      startDate: form.dateRange[0].toISOString(),
      endDate: form.dateRange[1].toISOString(),
      reason: form.reason,
    }
    await leaveApi.request(payload)
    message.success('Đã gửi đơn nghỉ phép!')
    modalOpen.value = false
    if (isManagerOrAbove.value) loadPending()
    else loadMyLeaves()
  } catch (e) {
    message.error(e.response?.data?.message || 'Gửi đơn thất bại')
  } finally {
    saving.value = false
  }
}

async function approve(id) {
  approvingId.value = id
  try {
    await leaveApi.approve(id, auth.userId)
    message.success('Đã duyệt đơn nghỉ phép')
    loadPending()
  } catch {
    message.error('Duyệt thất bại')
  } finally {
    approvingId.value = null
  }
}

async function reject(id) {
  rejectingId.value = id
  try {
    await leaveApi.reject(id, auth.userId)
    message.success('Đã từ chối đơn')
    loadPending()
  } catch {
    message.error('Từ chối thất bại')
  } finally {
    rejectingId.value = null
  }
}

onMounted(() => {
  if (isManagerOrAbove.value) {
    // Admin/HR/Manager: load danh sách nhân viên + tab chờ duyệt
    loadEmployees()
    activeTab.value = 'pending'
    loadPending()
  } else {
    // Employee: chỉ thấy tab "Đơn của tôi", tự động load đơn của mình
    activeTab.value = 'my'
    if (auth.userId) {
      myEmpId.value = auth.userId
      loadMyLeaves()
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
</style>
