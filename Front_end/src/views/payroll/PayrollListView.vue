<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">Bảng lương</h1>
        <p style="margin: 4px 0 0; font-size: 14px; color: var(--color-text-muted)">
          Quản lý và tính lương nhân viên
        </p>
      </div>
      <a-space>
        <a-button @click="loadPayrolls"><ReloadOutlined /></a-button>
        <!-- Chỉ Admin mới tính lương, duyệt lương và thêm mới -->
        <a-button v-if="auth.isAdmin" @click="calculateAll" :loading="calculating">
          <ThunderboltOutlined /> Tính lương tháng
        </a-button>
        <a-button
          v-if="auth.isAdmin"
          class="payroll-approve-btn"
          type="primary"
          @click="approvePayroll"
          :loading="approving"
        >
          <MailOutlined /> Duyệt & Gửi Email
        </a-button>
        <a-button v-if="auth.isAdmin" type="primary" @click="openCreate"
          ><PlusOutlined /> Thêm bảng lương</a-button
        >
      </a-space>
    </div>

    <!-- Filters: chỉ hiện với Admin/HR/Manager, Employee không cần filter -->
    <a-card v-if="isManagerOrAbove" :bordered="false" style="margin-bottom: 20px">
      <a-row :gutter="[16, 12]" align="middle">
        <a-col :xs="24" :sm="8" :lg="5">
          <a-date-picker
            v-model:value="filterMonth"
            picker="month"
            format="MM/YYYY"
            placeholder="Chọn tháng"
            style="width: 100%"
            @change="loadPayrolls"
          />
        </a-col>
        <a-col :xs="24" :sm="8" :lg="5">
          <a-select
            v-model:value="filterStatus"
            placeholder="Trạng thái"
            allow-clear
            style="width: 100%"
            @change="loadPayrolls"
          >
            <a-select-option value="">Tất cả</a-select-option>
            <a-select-option value="Draft">Nháp</a-select-option>
            <a-select-option value="Approved">Đã duyệt</a-select-option>
          </a-select>
        </a-col>
        <a-col>
          <span style="font-size: 13px; color: var(--color-text-muted)">
            <DollarOutlined /> Tổng:
            <strong style="color: var(--color-text)">{{ formatCurrency(totalAmount) }}</strong>
          </span>
        </a-col>
      </a-row>
    </a-card>
    <!-- Employee: chỉ hiện thông tin tháng hiện tại -->
    <a-card v-else :bordered="false" style="margin-bottom: 20px">
      <span style="font-size: 13px; color: var(--color-text-muted)"
        >Bảng lương cá nhân của bạn</span
      >
    </a-card>

    <!-- Table -->
    <a-card :bordered="false">
      <a-table
        :data-source="payrolls"
        :columns="columns"
        :loading="loading"
        :pagination="{
          current: page,
          pageSize,
          total,
          showSizeChanger: true,
          showTotal: (t) => `${t} bản ghi`,
        }"
        row-key="id"
        @change="onTableChange"
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
              <div>
                <div style="font-weight: 600">
                  {{ record.employeeName || `#${record.employeeId}` }}
                </div>
                <div style="font-size: 11px; color: var(--color-text-muted)">
                  {{ record.departmentName || '' }}
                </div>
              </div>
            </div>
          </template>
          <template v-else-if="column.key === 'period'">
            <span style="font-weight: 500">{{ formatPeriod(record) }}</span>
          </template>
          <template v-else-if="column.key === 'baseSalary'">
            <span style="color: var(--color-text-sec)">{{
              formatCurrency(record.baseSalary)
            }}</span>
          </template>
          <template v-else-if="column.key === 'netSalary'">
            <!-- Backend field là finalSalary -->
            <span style="font-weight: 700; color: var(--color-primary); font-size: 15px">
              {{ formatCurrency(record.finalSalary || record.netSalary || record.totalSalary) }}
            </span>
          </template>
          <template v-else-if="column.key === 'status'">
            <span class="status-badge" :class="getStatusBadge(record.payrollStatus)">{{
              getStatusLabel(record.payrollStatus)
            }}</span>
          </template>
          <template v-else-if="column.key === 'actions'">
            <a-space>
              <a-button type="text" size="small" @click="openView(record)"
                ><EyeOutlined style="color: var(--color-accent-blue)"
              /></a-button>
              <a-button v-if="auth.isAdmin" type="text" size="small" @click="openEdit(record)"
                ><EditOutlined
              /></a-button>
              <a-popconfirm
                v-if="auth.isAdmin"
                title="Xoá bảng lương?"
                ok-type="danger"
                ok-text="Xoá"
                cancel-text="Huỷ"
                @confirm="deletePayroll(record.id)"
              >
                <a-button type="text" size="small" danger><DeleteOutlined /></a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <!-- Create/Edit Modal -->
    <a-modal
      v-model:open="modalOpen"
      :title="
        editingId ? (viewOnly ? 'Chi tiết bảng lương' : 'Chỉnh sửa bảng lương') : 'Thêm bảng lương'
      "
      :confirm-loading="saving"
      :ok-text="viewOnly ? null : 'Lưu'"
      :cancel-text="viewOnly ? 'Đóng' : 'Huỷ'"
      :footer="viewOnly ? null : undefined"
      width="680px"
      @ok="savePayroll"
      destroy-on-close
    >
      <!-- View mode -->
      <div v-if="viewOnly && viewRecord" class="payroll-detail">
        <a-descriptions :column="2" bordered size="small">
          <a-descriptions-item label="Họ tên">{{
            viewRecord.fullName || viewRecord.employeeName
          }}</a-descriptions-item>
          <a-descriptions-item label="Mã NV / Phòng ban"
            >{{ viewRecord.employeeCode || viewRecord.employeeId }} —
            {{ viewRecord.departmentName || '—' }}</a-descriptions-item
          >
          <a-descriptions-item label="Kỳ lương">{{ formatPeriod(viewRecord) }}</a-descriptions-item>
          <a-descriptions-item label="Loại thuế">{{
            viewRecord.taxType === 'Progressive' ? 'Lũy tiến' : 'Khấu trừ 10%'
          }}</a-descriptions-item>
          <a-descriptions-item label="Lương CB HĐ">{{
            formatCurrency(viewRecord.contractBasicSalary)
          }}</a-descriptions-item>
          <a-descriptions-item label="Hệ số lương">{{
            viewRecord.salaryRatio || '1.0'
          }}</a-descriptions-item>
          <a-descriptions-item label="Lương theo công TT">{{
            formatCurrency(viewRecord.baseSalary)
          }}</a-descriptions-item>
          <a-descriptions-item label="Thu nhập gộp (Gross)">{{
            formatCurrency(viewRecord.grossIncome)
          }}</a-descriptions-item>
          <a-descriptions-item label="Ngày công TT / Chuẩn"
            >{{ viewRecord.workingDays || 0 }} /
            {{ viewRecord.standardWorkingDays || 26 }}</a-descriptions-item
          >
          <a-descriptions-item label="Nghỉ phép / Không lương"
            >{{ viewRecord.leaveDays || 0 }} /
            {{ viewRecord.unpaidLeaveDays || 0 }}</a-descriptions-item
          >
          <a-descriptions-item label="Tăng ca (OT)">{{
            formatCurrency(viewRecord.overtimePay)
          }}</a-descriptions-item>
          <a-descriptions-item label="Thưởng">{{
            formatCurrency(viewRecord.bonus)
          }}</a-descriptions-item>
          <a-descriptions-item label="BHXH (8%)">{{
            formatCurrency(viewRecord.bhxhEmployee)
          }}</a-descriptions-item>
          <a-descriptions-item label="BHYT (1.5%)">{{
            formatCurrency(viewRecord.bhytEmployee)
          }}</a-descriptions-item>
          <a-descriptions-item label="BHTN (1%)">{{
            formatCurrency(viewRecord.bhtnEmployee)
          }}</a-descriptions-item>
          <a-descriptions-item label="Người phụ thuộc"
            >{{ viewRecord.dependentCount || 0 }} người ({{
              formatCurrency(viewRecord.dependentDeduction)
            }})</a-descriptions-item
          >
          <a-descriptions-item label="Thu nhập tính thuế">{{
            formatCurrency(viewRecord.taxableIncome)
          }}</a-descriptions-item>
          <a-descriptions-item label="Thuế TNCN">{{
            formatCurrency(viewRecord.personalTax)
          }}</a-descriptions-item>
          <a-descriptions-item label="Khấu trừ khác">{{
            formatCurrency(viewRecord.deduction)
          }}</a-descriptions-item>
          <a-descriptions-item label="Tổng khấu trừ">{{
            formatCurrency(viewRecord.totalDeduction)
          }}</a-descriptions-item>
          <a-descriptions-item label="Trạng thái bảng lương">
            <span class="status-badge" :class="getStatusBadge(viewRecord.payrollStatus)">{{
              getStatusLabel(viewRecord.payrollStatus)
            }}</span>
          </a-descriptions-item>
          <a-descriptions-item v-if="viewRecord.employeeStatus" label="TT Nhân viên">
            <span
              class="status-badge"
              :class="viewRecord.employeeStatus === 'Active' ? 'badge-success' : 'badge-default'"
            >
              {{ viewRecord.employeeStatus === 'Active' ? 'Đang làm việc' : 'Đã nghỉ việc' }}
            </span>
          </a-descriptions-item>
          <a-descriptions-item v-if="viewRecord.approvedAt" label="Duyệt lúc">{{
            new Date(viewRecord.approvedAt).toLocaleString('vi-VN')
          }}</a-descriptions-item>
        </a-descriptions>
        <a-divider />
        <div
          style="
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 12px 16px;
            background: rgba(0, 177, 79, 0.06);
            border-radius: 10px;
            border: 1px solid rgba(0, 177, 79, 0.15);
          "
        >
          <div>
            <div style="font-weight: 600; font-size: 15px">Thực lĩnh (NET)</div>
            <div style="font-size: 12px; color: var(--color-text-muted)">
              Tổng khấu trừ: {{ formatCurrency(viewRecord.totalDeduction) }}
            </div>
          </div>
          <span
            style="
              font-family: var(--font-display);
              font-size: 24px;
              font-weight: 700;
              color: var(--color-primary);
            "
          >
            {{ formatCurrency(viewRecord.finalSalary) }}
          </span>
        </div>
      </div>

      <!-- Edit/Create mode -->
      <a-form v-else :model="form" layout="vertical" ref="formRef" :rules="rules">
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item name="employeeId" label="Mã nhân viên (ID)">
              <a-input
                v-model:value="form.employeeId"
                placeholder="Ví dụ: NV001"
                :disabled="!!editingId"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="employeeName" label="Tên nhân viên">
              <a-input
                v-model:value="form.employeeName"
                placeholder="Tên nhân viên"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="month" label="Kỳ lương">
              <a-date-picker
                v-model:value="form.month"
                picker="month"
                format="MM/YYYY"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="contractBasicSalary" label="Lương CB Hợp đồng">
              <a-input-number
                v-model:value="form.contractBasicSalary"
                :disabled="true"
                :formatter="fmt"
                :parser="prs"
                style="width: 100%"
                placeholder="10,000,000"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="salaryRatio" label="Hệ số lương">
              <a-input-number
                v-model:value="form.salaryRatio"
                :disabled="true"
                :min="0.1"
                :max="10"
                :step="0.05"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="workingDays" label="Ngày công làm việc">
              <a-input-number
                v-model:value="form.workingDays"
                :min="0"
                :max="31"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="standardWorkingDays" label="Số công chuẩn">
              <a-input-number
                v-model:value="form.standardWorkingDays"
                :min="1"
                :max="31"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="leaveDays" label="Nghỉ phép hưởng lương">
              <a-input-number
                v-model:value="form.leaveDays"
                :min="0"
                :max="31"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="unpaidLeaveDays" label="Nghỉ không lương">
              <a-input-number
                v-model:value="form.unpaidLeaveDays"
                :min="0"
                :max="31"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="dependentCount" label="Số người phụ thuộc">
              <a-input-number
                v-model:value="form.dependentCount"
                :min="0"
                :max="20"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="overtimePay" label="Lương tăng ca (OT)">
              <a-input-number
                v-model:value="form.overtimePay"
                :formatter="fmt"
                :parser="prs"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="bonus" label="Thưởng">
              <a-input-number
                v-model:value="form.bonus"
                :formatter="fmt"
                :parser="prs"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="deduction" label="Khấu trừ khác">
              <a-input-number
                v-model:value="form.deduction"
                :formatter="fmt"
                :parser="prs"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
        </a-row>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { payrollApiService } from '@/api'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'
import {
  ReloadOutlined,
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  EyeOutlined,
  DollarOutlined,
  ThunderboltOutlined,
  MailOutlined,
} from '@ant-design/icons-vue'

const auth = useAuthStore()
const isManagerOrAbove = computed(() => auth.isAdmin || auth.isHR || auth.isManager)

const loading = ref(false)
const saving = ref(false)
const calculating = ref(false)
const payrolls = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(10)
const filterMonth = ref(dayjs())
const filterStatus = ref('')
const modalOpen = ref(false)
const viewOnly = ref(false)
const viewRecord = ref(null)
const editingId = ref(null)
const formRef = ref()
const approving = ref(false)

const form = reactive({
  employeeId: '',
  employeeName: '',
  month: dayjs(),
  contractBasicSalary: null,
  salaryRatio: 1.0,
  taxType: 'Progressive',
  isSocialInsuranceSubject: true,
  workingDays: 26,
  standardWorkingDays: 26,
  leaveDays: 0,
  unpaidLeaveDays: 0,
  dependentCount: 0,
  overtimePay: 0,
  bonus: 0,
  deduction: 0,
  payrollStatus: 'Draft',
})
const rules = {
  employeeId: [{ required: true, message: 'Nhập ID nhân viên' }],
  employeeName: [{ required: true, message: 'Nhập tên nhân viên' }],
  contractBasicSalary: [{ required: true, message: 'Nhập lương cơ bản hợp đồng' }],
}

const colors = ['#00b14f', '#00b4d8', '#7c5cfc', '#ffb020', '#ff4757']
const getColor = (n) => colors[(n || '').charCodeAt(0) % colors.length]
const formatCurrency = (v) =>
  v === null || v === undefined || v === '' ? '—' : new Intl.NumberFormat('vi-VN').format(v) + '₫'
const fmt = (v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')
const prs = (v) => v.replace(/,/g, '')
const formatPeriod = (r) => {
  // Thử nhiều tên field khác nhau mà backend có thể trả
  if (r.payMonth) return dayjs(r.payMonth).format('MM/YYYY')
  if (r.month && typeof r.month === 'string' && r.month.length >= 7)
    return dayjs(r.month).format('MM/YYYY')
  if (r.month && typeof r.month === 'object') return dayjs(r.month).format('MM/YYYY')
  if (r.year && r.month2) return `${String(r.month2).padStart(2, '0')}/${r.year}`
  if (r.year && r.monthNumber) return `${String(r.monthNumber).padStart(2, '0')}/${r.year}`
  if (r.period) return r.period
  if (r.payPeriod) return r.payPeriod
  // Fallback: backend chưa có field kỳ lương riêng → dùng createdAt
  if (r.createdAt) return dayjs(r.createdAt).format('MM/YYYY')
  return '—'
}
const getStatusBadge = (s) =>
  ({
    Draft: 'badge-default',
    Approved: 'badge-success',
    Confirmed: 'badge-info',
    Paid: 'badge-success',
  })[s] || 'badge-default'
const getStatusLabel = (s) =>
  ({ Draft: 'Nháp', Approved: 'Đã duyệt', Confirmed: 'Đã xác nhận', Paid: 'Đã thanh toán' })[s] ||
  (s ? s : 'Đã tính')

const totalAmount = computed(() => payrolls.value.reduce((sum, r) => sum + (r.finalSalary || 0), 0))

const columns = [
  { title: 'Nhân viên', key: 'employee', dataIndex: 'employeeName' },
  { title: 'Kỳ lương', key: 'period', width: 110 },
  { title: 'Lương CB', key: 'baseSalary', dataIndex: 'baseSalary', width: 140 },
  {
    title: 'Thực lĩnh',
    key: 'netSalary',
    dataIndex: 'finalSalary',
    width: 160,
    sorter: (a, b) => (a.finalSalary || 0) - (b.finalSalary || 0),
  },
  { title: 'Ngày công', dataIndex: 'workingDays', width: 100, align: 'center' },
  { title: 'Trạng thái', key: 'status', width: 130 },
  { title: '', key: 'actions', width: 110, align: 'center' },
]

async function loadPayrolls() {
  loading.value = true
  try {
    const params = {}
    if (filterMonth.value) {
      params.payPeriod = filterMonth.value.format('YYYY-MM')
    }
    const res = await payrollApiService.getAll(params)
    const data = res.data
    let list = Array.isArray(data) ? data : data.data || data.items || data.payrolls || []

    if (filterStatus.value) {
      list = list.filter((r) => r.payrollStatus === filterStatus.value)
    }

    if (!isManagerOrAbove.value && auth.userId) {
      list = list.filter((r) => r.employeeId === auth.userId)
    }

    payrolls.value = list
    total.value = list.length
  } catch {
    message.error('Không tải được bảng lương')
  } finally {
    loading.value = false
  }
}

async function calculateAll() {
  calculating.value = true
  try {
    // Backend yêu cầu body JSON: { month: 'YYYY-MM' }
    const body = {
      month: filterMonth.value ? filterMonth.value.format('YYYY-MM') : dayjs().format('YYYY-MM'),
    }
    const res = await payrollApiService.calculateAll(body)
    const result = res.data
    message.success(
      `Đã tính lương tháng ${body.month}! Tạo mới: ${result?.created ?? 0}, Tổng: ${result?.totalPayrolls ?? 0} bảng lương.`,
    )
    loadPayrolls()
  } catch (e) {
    message.error(e.response?.data?.message || e.response?.data?.error || 'Tính lương thất bại')
  } finally {
    calculating.value = false
  }
}

function onTableChange(pag) {
  page.value = pag.current
  pageSize.value = pag.pageSize
  loadPayrolls()
}

function openCreate() {
  editingId.value = null
  viewOnly.value = false
  viewRecord.value = null
  Object.assign(form, {
    employeeId: '',
    employeeName: '',
    month: dayjs(),
    contractBasicSalary: null,
    salaryRatio: 1.0,
    taxType: 'Progressive',
    isSocialInsuranceSubject: true,
    workingDays: 26,
    standardWorkingDays: 26,
    leaveDays: 0,
    unpaidLeaveDays: 0,
    dependentCount: 0,
    overtimePay: 0,
    bonus: 0,
    deduction: 0,
    payrollStatus: 'Draft',
  })
  modalOpen.value = true
}
function openEdit(rec) {
  editingId.value = rec.id
  viewOnly.value = false
  viewRecord.value = null
  Object.assign(form, {
    employeeId: rec.employeeId,
    employeeName: rec.employeeName || '',
    month: rec.payPeriod ? dayjs(rec.payPeriod, 'YYYY-MM') : rec.month ? dayjs(rec.month) : dayjs(),
    contractBasicSalary: rec.contractBasicSalary || rec.baseSalary || 0,
    salaryRatio: rec.salaryRatio || 1.0,
    taxType: rec.taxType || 'Progressive',
    isSocialInsuranceSubject: rec.isSocialInsuranceSubject !== false,
    workingDays: rec.workingDays || 0,
    standardWorkingDays: rec.standardWorkingDays || 26,
    leaveDays: rec.leaveDays || 0,
    unpaidLeaveDays: rec.unpaidLeaveDays || 0,
    dependentCount: rec.dependentCount || 0,
    overtimePay: rec.overtimePay || 0,
    bonus: rec.bonus || 0,
    deduction: rec.deduction || 0,
    payrollStatus: rec.payrollStatus || 'Draft',
  })
  modalOpen.value = true
}
function openView(rec) {
  viewOnly.value = true
  viewRecord.value = rec
  modalOpen.value = true
}

async function savePayroll() {
  try {
    await formRef.value.validate()
  } catch {
    return
  }
  saving.value = true
  const draftBaseSalary = (form.contractBasicSalary || 0) * (form.salaryRatio || 1.0)
  const payload = {
    employeeId: String(form.employeeId),
    employeeName: form.employeeName,
    payPeriod: form.month ? form.month.format('YYYY-MM') : dayjs().format('YYYY-MM'),
    baseSalary: draftBaseSalary,
    contractBasicSalary: form.contractBasicSalary || 0,
    salaryRatio: form.salaryRatio || 1.0,
    taxType: form.taxType || 'Progressive',
    isSocialInsuranceSubject: form.isSocialInsuranceSubject,
    workingDays: form.workingDays || 0,
    standardWorkingDays: form.standardWorkingDays || 26,
    leaveDays: form.leaveDays || 0,
    unpaidLeaveDays: form.unpaidLeaveDays || 0,
    dependentCount: form.dependentCount || 0,
    overtimePay: form.overtimePay || 0,
    bonus: form.bonus || 0,
    deduction: form.deduction || 0,
  }
  try {
    if (editingId.value) {
      await payrollApiService.update(editingId.value, payload)
      message.success('Đã cập nhật')
    } else {
      await payrollApiService.create(payload)
      message.success('Đã tạo bảng lương')
    }
    modalOpen.value = false
    loadPayrolls()
  } catch (e) {
    message.error(e.response?.data?.message || 'Lưu thất bại')
  } finally {
    saving.value = false
  }
}

async function approvePayroll() {
  approving.value = true
  try {
    const period = filterMonth.value
      ? filterMonth.value.format('YYYY-MM')
      : dayjs().format('YYYY-MM')
    const payload = {
      payPeriod: period,
      sendEmail: true,
    }
    const res = await payrollApiService.approve(payload)
    const result = res.data || {}
    const totalApproved = result.totalApproved ?? 0
    const emailSent = result.emailSent ?? 0
    const emailFailed = result.emailFailed ?? 0

    if (totalApproved === 0) {
      message.warning(
        `Kỳ lương ${period} không có bảng lương nào được duyệt. Email gửi thành công: ${emailSent}, thất bại: ${emailFailed}.`,
      )
    } else {
      message.success(
        `Đã duyệt thành công kỳ lương ${period}! Tổng số đã duyệt: ${totalApproved}. Đã gửi ${emailSent} email, thất bại ${emailFailed}.`,
      )
    }
    loadPayrolls()
  } catch (e) {
    message.error(e.response?.data?.message || 'Duyệt bảng lương thất bại')
  } finally {
    approving.value = false
  }
}

async function deletePayroll(id) {
  try {
    await payrollApiService.remove(id)
    message.success('Đã xoá')
    loadPayrolls()
  } catch {
    message.error('Xoá thất bại')
  }
}

onMounted(loadPayrolls)
</script>

<style scoped>
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 24px;
}
.payroll-detail {
  padding: 4px 0;
}

.payroll-approve-btn {
  color: #fff !important;
}
.payroll-approve-btn :deep(.anticon) {
  color: #fff !important;
}
</style>
