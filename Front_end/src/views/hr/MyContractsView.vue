<template>
  <div>
    <!-- Page header -->
    <div class="page-header">
      <div>
        <h1 class="page-title">
          <AuditOutlined style="margin-right: 8px; color: var(--color-primary)" /> Hợp đồng của tôi
        </h1>
        <p style="margin: 4px 0 0; color: var(--color-text-muted); font-size: 14px">
          Xem lịch sử hợp đồng lao động và thực hiện ký kết trực tuyến các bản nháp hợp đồng mới.
        </p>
      </div>
      <a-button type="primary" @click="loadContracts" :loading="loading">
        <template #icon><ReloadOutlined /></template>
        Làm mới
      </a-button>
    </div>

    <!-- Summary cards -->
    <a-row :gutter="[20, 20]" style="margin-bottom: 24px">
      <a-col :xs="24" :sm="12" :lg="8">
        <div class="stat-card">
          <div class="stat-icon" style="background: linear-gradient(135deg, #00b14f, #00d46a)">
            <CheckCircleOutlined style="font-size: 20px; color: #fff" />
          </div>
          <div class="stat-body">
            <div class="stat-value">
              <span v-if="loading"><a-spin size="small" /></span>
              <span v-else>{{ activeCount }}</span>
            </div>
            <div class="stat-label">Hợp đồng hiệu lực</div>
          </div>
        </div>
      </a-col>

      <a-col :xs="24" :sm="12" :lg="8">
        <div class="stat-card">
          <div class="stat-icon" style="background: linear-gradient(135deg, #ffb020, #ff8c00)">
            <FileTextOutlined style="font-size: 20px; color: #fff" />
          </div>
          <div class="stat-body">
            <div class="stat-value">
              <span v-if="loading"><a-spin size="small" /></span>
              <span v-else>{{ draftCount }}</span>
            </div>
            <div class="stat-label">Bản nháp chờ ký</div>
          </div>
        </div>
      </a-col>

      <a-col :xs="24" :sm="12" :lg="8">
        <div class="stat-card">
          <div class="stat-icon" style="background: linear-gradient(135deg, #00b4d8, #7c5cfc)">
            <DollarOutlined style="font-size: 20px; color: #fff" />
          </div>
          <div class="stat-body">
            <div class="stat-value">
              <span v-if="loading"><a-spin size="small" /></span>
              <span v-else>{{ formatCurrency(activeSalary) }}</span>
            </div>
            <div class="stat-label">Lương cơ bản hiện tại</div>
          </div>
        </div>
      </a-col>
    </a-row>

    <!-- Contracts Table Card -->
    <a-card title="Danh sách hợp đồng lao động" :bordered="false">
      <a-table
        :data-source="contracts"
        :columns="columns"
        :loading="loading"
        row-key="id"
        :pagination="{ pageSize: 10 }"
        size="middle"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'contractNumber'">
            <span style="font-weight: 600; color: var(--color-text)">{{
              record.contractNumber
            }}</span>
          </template>

          <template v-else-if="column.key === 'contractTypeName'">
            <span>{{ record.contractTypeName }}</span>
          </template>

          <template v-else-if="column.key === 'duration'">
            <span>
              {{ dayjs(record.startDate).format('DD/MM/YYYY') }}
              -
              {{ record.endDate ? dayjs(record.endDate).format('DD/MM/YYYY') : 'Không thời hạn' }}
            </span>
          </template>

          <template v-else-if="column.key === 'basicSalary'">
            <span style="font-weight: 600">{{ formatCurrency(record.basicSalary) }}</span>
            <div style="font-size: 11px; color: var(--color-text-muted)">
              Hệ số lương: {{ record.salaryRatio }}
            </div>
          </template>

          <template v-else-if="column.key === 'status'">
            <a-tag :color="getContractStatusColor(record.status)">
              {{ getContractStatusText(record.status) }}
            </a-tag>
          </template>

          <template v-else-if="column.key === 'actions'">
            <a-space>
              <a-button
                class="contract-detail-btn"
                type="primary"
                size="small"
                @click="viewContractDetail(record)"
              >
                <template #icon><EyeOutlined /></template>
                Chi tiết
              </a-button>
              <a-button
                v-if="record.status === 'Draft'"
                type="primary"
                size="small"
                @click="openSignModal(record)"
              >
                <template #icon><SafetyCertificateOutlined /></template>
                Ký hợp đồng
              </a-button>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <!-- Modal: Xem chi tiết & Ký hợp đồng -->
    <a-modal
      v-model:open="detailModalOpen"
      title="Chi tiết hợp đồng lao động"
      :width="800"
      :footer="null"
      destroy-on-close
    >
      <div v-if="selectedContract" class="contract-doc-container">
        <!-- Contract Header simulation -->
        <div class="contract-doc-header">
          <div class="nation-title">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</div>
          <div class="nation-subtitle">Độc lập - Tự do - Hạnh phúc</div>
          <div class="divider-line"></div>
          <h2 class="doc-title">HỢP ĐỒNG LAO ĐỘNG</h2>
          <div class="doc-number">Số: {{ selectedContract.contractNumber }}</div>
        </div>

        <!-- Contract content body -->
        <div class="contract-doc-body">
          <p>
            <em
              >Hôm nay, ngày
              {{ dayjs(selectedContract.signDate || selectedContract.createdAt).format('DD') }}
              tháng
              {{ dayjs(selectedContract.signDate || selectedContract.createdAt).format('MM') }} năm
              {{ dayjs(selectedContract.signDate || selectedContract.createdAt).format('YYYY') }},
              chúng tôi gồm các bên dưới đây:</em
            >
          </p>

          <h4>Bên A: NGƯỜI SỬ DỤNG LAO ĐỘNG</h4>
          <p><strong>Công ty Cổ phần Giải pháp Công nghệ Antigravity</strong></p>
          <p>Đại diện bởi: Ban Giám đốc nhân sự</p>
          <p>Địa chỉ: Tòa nhà Antigravity, Hà Nội, Việt Nam</p>

          <h4>Bên B: NGƯỜI LAO ĐỘNG</h4>
          <p>
            Họ và tên: <strong>{{ selectedContract.employeeName || auth.userName }}</strong>
          </p>
          <p>
            Mã nhân viên: <strong>{{ auth.user?.employeeCode || '—' }}</strong>
          </p>

          <h4>ĐIỀU KHOẢN HỢP ĐỒNG</h4>
          <p><strong>Điều 1: Công việc và thời hạn hợp đồng</strong></p>
          <ul>
            <li>Loại hợp đồng: {{ selectedContract.contractTypeName }}</li>
            <li>
              Thời gian làm việc: Từ ngày
              {{ dayjs(selectedContract.startDate).format('DD/MM/YYYY') }} đến ngày
              {{
                selectedContract.endDate
                  ? dayjs(selectedContract.endDate).format('DD/MM/YYYY')
                  : 'không xác định thời hạn'
              }}.
            </li>
          </ul>

          <p><strong>Điều 2: Lương và các khoản phụ cấp</strong></p>
          <ul>
            <li>
              Mức lương cơ bản: <strong>{{ formatCurrency(selectedContract.basicSalary) }}</strong>
            </li>
            <li>
              Hệ số tính lương: <strong>{{ selectedContract.salaryRatio }}</strong>
            </li>
            <li>Hình thức trả lương: Chuyển khoản qua ngân hàng.</li>
          </ul>

          <p><strong>Điều 3: Nghĩa vụ và quyền lợi của người lao động</strong></p>
          <p>
            Người lao động cam kết thực hiện đầy đủ các quy định về thời gian làm việc, bảo mật
            thông tin doanh nghiệp, và hoàn thành các công việc được giao.
          </p>

          <div v-if="selectedContract.notes" class="contract-notes">
            <strong>Ghi chú bổ sung từ HR:</strong> {{ selectedContract.notes }}
          </div>
        </div>

        <!-- Digital Signature Area -->
        <div class="signature-section">
          <a-row :gutter="24">
            <a-col :span="12" class="sig-col">
              <div class="sig-title">ĐẠI DIỆN BÊN A (HR/ADMIN)</div>
              <div class="sig-box signed">
                <CheckCircleOutlined class="verified-icon" />
                <span class="sig-status">Đã ký điện tử</span>
                <span class="sig-date">{{
                  dayjs(selectedContract.createdAt).format('DD/MM/YYYY')
                }}</span>
              </div>
            </a-col>
            <a-col :span="12" class="sig-col">
              <div class="sig-title">BÊN B (NGƯỜI LAO ĐỘNG)</div>
              <div v-if="selectedContract.status === 'Draft'" class="sig-box unsigned">
                <p style="font-size: 12px; color: var(--color-text-sec); margin-bottom: 8px">
                  Hợp đồng đang chờ bạn ký duyệt.
                </p>
                <a-button type="primary" size="middle" @click="openSignModal(selectedContract)">
                  <template #icon><SafetyCertificateOutlined /></template>
                  Ký điện tử ngay
                </a-button>
              </div>
              <div v-else class="sig-box signed">
                <CheckCircleOutlined class="verified-icon" />
                <span class="sig-status">Đã ký điện tử</span>
                <span class="sig-date">{{
                  dayjs(selectedContract.signDate).format('DD/MM/YYYY HH:mm')
                }}</span>
              </div>
            </a-col>
          </a-row>
        </div>
      </div>
    </a-modal>

    <!-- Modal: Confirm Sign Contract -->
    <a-modal
      v-model:open="signModalOpen"
      title="Xác nhận ký hợp đồng lao động"
      ok-text="Đồng ý ký kết"
      cancel-text="Hủy"
      :confirm-loading="signing"
      @ok="handleSign"
      destroy-on-close
    >
      <div v-if="contractToSign" style="padding: 10px 0">
        <div style="text-align: center; margin-bottom: 16px">
          <SafetyCertificateOutlined style="font-size: 48px; color: var(--color-primary)" />
          <h3 style="margin-top: 12px; font-weight: 700">Chữ ký số điện tử cá nhân</h3>
        </div>
        <p style="font-size: 14px; line-height: 1.5; color: var(--color-text)">
          Bạn đang thực hiện ký kết hợp đồng số
          <strong>{{ contractToSign.contractNumber }}</strong> ({{
            contractToSign.contractTypeName
          }}).
        </p>
        <div class="terms-box">
          <h5 style="margin-top: 0; margin-bottom: 6px; font-weight: 700">
            ĐIỀU KHOẢN VÀ XÁC NHẬN
          </h5>
          <p style="margin: 0; font-size: 12px; color: var(--color-text-sec)">
            Bằng cách nhấp vào "Đồng ý ký kết" dưới đây, tôi xác nhận rằng:
          </p>
          <ul
            style="
              margin: 6px 0 0;
              padding-left: 18px;
              font-size: 11px;
              color: var(--color-text-sec);
            "
          >
            <li>Tôi đã đọc, hiểu và đồng ý hoàn toàn với các điều khoản nêu trong hợp đồng.</li>
            <li>
              Hành động bấm nút này có giá trị pháp lý tương đương chữ ký tay của tôi trên bản hợp
              đồng giấy.
            </li>
            <li>
              Trạng thái hợp đồng của tôi sẽ lập tức chuyển sang "Có hiệu lực" sau khi ký kết thành
              công.
            </li>
          </ul>
        </div>
        <div style="margin-top: 16px">
          <a-checkbox v-model:checked="consentChecked"
            >Tôi đã đọc kĩ và hoàn toàn đồng ý với các điều khoản của hợp đồng.</a-checkbox
          >
        </div>
      </div>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { contractApi } from '@/api'
import { message } from 'ant-design-vue'
import dayjs from 'dayjs'
import {
  AuditOutlined,
  ReloadOutlined,
  CheckCircleOutlined,
  FileTextOutlined,
  DollarOutlined,
  EyeOutlined,
  SafetyCertificateOutlined,
} from '@ant-design/icons-vue'

const auth = useAuthStore()
const loading = ref(false)
const signing = ref(false)
const contracts = ref([])

// Modals
const detailModalOpen = ref(false)
const selectedContract = ref(null)
const signModalOpen = ref(false)
const contractToSign = ref(null)
const consentChecked = ref(false)

// Columns definition
const columns = [
  { title: 'Số hợp đồng', key: 'contractNumber', dataIndex: 'contractNumber' },
  { title: 'Loại hợp đồng', key: 'contractTypeName', dataIndex: 'contractTypeName' },
  { title: 'Thời hạn', key: 'duration', dataIndex: 'startDate' },
  { title: 'Lương cơ bản', key: 'basicSalary', dataIndex: 'basicSalary', align: 'right' },
  { title: 'Trạng thái', key: 'status', dataIndex: 'status', width: 120 },
  { title: 'Thao tác', key: 'actions', width: 220, align: 'center' },
]

// KPI computeds
const activeCount = computed(() => {
  return contracts.value.filter((c) => c.status === 'Active').length
})
const draftCount = computed(() => {
  return contracts.value.filter((c) => c.status === 'Draft').length
})
const activeSalary = computed(() => {
  const activeContract = contracts.value.find((c) => c.status === 'Active')
  return activeContract ? activeContract.basicSalary : 0
})

// Helpers
const formatCurrency = (v) => (v ? new Intl.NumberFormat('vi-VN').format(v) + ' ₫' : '—')

function getContractStatusColor(status) {
  switch (status) {
    case 'Active':
      return 'success'
    case 'Expired':
      return 'warning'
    case 'Terminated':
      return 'error'
    default:
      return 'default'
  }
}

function getContractStatusText(status) {
  switch (status) {
    case 'Active':
      return 'Hiệu lực'
    case 'Expired':
      return 'Hết hạn'
    case 'Terminated':
      return 'Chấm dứt'
    case 'Draft':
      return 'Bản nháp (Chờ ký)'
    default:
      return status
  }
}

// Load contracts for logged-in user
async function loadContracts() {
  if (!auth.userId) return
  loading.value = true
  try {
    const res = await contractApi.getAll({ employeeId: auth.userId })
    const data = res.data
    contracts.value = Array.isArray(data) ? data : data.data || data.items || data.contracts || []
  } catch (e) {
    message.error('Không tải được danh sách hợp đồng cá nhân')
  } finally {
    loading.value = false
  }
}

function viewContractDetail(record) {
  selectedContract.value = record
  detailModalOpen.value = true
}

function openSignModal(record) {
  contractToSign.value = record
  consentChecked.value = false
  signModalOpen.value = true
}

async function handleSign() {
  if (!consentChecked.value) {
    message.warning('Vui lòng tích chọn đồng ý điều khoản hợp đồng trước khi thực hiện ký kết.')
    return
  }
  if (!contractToSign.value) return

  signing.value = true
  try {
    await contractApi.sign(contractToSign.value.id)
    message.success('Ký hợp đồng thành công! Trạng thái hợp đồng đã có hiệu lực.')

    // Close modals
    signModalOpen.value = false
    detailModalOpen.value = false

    // Reload
    await loadContracts()
  } catch (e) {
    message.error(e.response?.data?.message || 'Ký hợp đồng thất bại')
  } finally {
    signing.value = false
  }
}

onMounted(() => {
  loadContracts()
})
</script>

<style scoped>
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 24px;
}

.contract-detail-btn {
  color: #fff !important;
}
.contract-detail-btn :deep(.anticon) {
  color: #fff !important;
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

/* Contract Doc styling */
.contract-doc-container {
  padding: 16px;
  max-height: 60vh;
  overflow-y: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: #fafafa;
}

.contract-doc-header {
  text-align: center;
  margin-bottom: 24px;
}
.nation-title {
  font-weight: 700;
  font-size: 14px;
}
.nation-subtitle {
  font-size: 13px;
  margin-top: 2px;
}
.divider-line {
  width: 120px;
  height: 1px;
  background: #000;
  margin: 8px auto;
}
.doc-title {
  font-family: var(--font-display);
  font-weight: 700;
  font-size: 20px;
  margin-top: 16px;
  margin-bottom: 4px;
}
.doc-number {
  font-size: 13px;
  color: var(--color-text-sec);
}

.contract-doc-body {
  font-size: 13.5px;
  line-height: 1.6;
  color: #333;
}
.contract-doc-body h4 {
  font-weight: 700;
  margin-top: 18px;
  margin-bottom: 6px;
  border-bottom: 1px dashed var(--color-border);
  padding-bottom: 4px;
}
.contract-doc-body ul {
  padding-left: 20px;
  margin: 6px 0;
}
.contract-notes {
  background: #fff3cd;
  border: 1px solid #ffeeba;
  color: #856404;
  padding: 10px 14px;
  border-radius: var(--radius-md);
  margin-top: 16px;
}

.signature-section {
  margin-top: 28px;
  padding-top: 18px;
  border-top: 1px solid var(--color-border);
}
.sig-col {
  text-align: center;
}
.sig-title {
  font-weight: 700;
  font-size: 12px;
  color: var(--color-text-sec);
  margin-bottom: 10px;
}
.sig-box {
  border: 2px dashed var(--color-border);
  border-radius: var(--radius-md);
  padding: 16px;
  min-height: 100px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: #fff;
  transition: all var(--transition);
}
.sig-box.signed {
  border-color: var(--color-primary);
  background: rgba(0, 177, 79, 0.03);
}
.sig-box.unsigned {
  border-color: var(--color-warning);
  background: rgba(255, 176, 32, 0.03);
}
.verified-icon {
  font-size: 28px;
  color: var(--color-primary);
  margin-bottom: 4px;
}
.sig-status {
  font-weight: 700;
  font-size: 13px;
  color: var(--color-primary);
}
.sig-date {
  font-size: 11px;
  color: var(--color-text-muted);
  margin-top: 2px;
}

/* Terms box */
.terms-box {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 12px;
  margin-top: 12px;
}

@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
    gap: 12px;
  }
  .sig-col {
    margin-bottom: 16px;
  }
}
</style>
