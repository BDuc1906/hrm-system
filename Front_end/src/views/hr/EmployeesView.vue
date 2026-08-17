<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">Nhân viên</h1>
        <p style="margin: 4px 0 0; font-size: 14px; color: var(--color-text-muted)">
          Quản lý hồ sơ nhân viên toàn công ty
        </p>
      </div>
      <div style="display: flex; gap: 10px">
        <a-button @click="loadAll"><ReloadOutlined /> Làm mới</a-button>
        <a-button
          v-if="activeTab === 'list' && (auth.isAdmin || auth.isHR)"
          type="primary"
          @click="openCreate"
        >
          <PlusOutlined /> Thêm nhân viên
        </a-button>
      </div>
    </div>

    <a-card :bordered="false">
      <a-tabs v-model:activeKey="activeTab" @change="onTabChange">
        <!-- ─── TAB 1: DANH SÁCH ─────────────────────────────── -->
        <a-tab-pane key="list" tab="Danh sách">
          <!-- Filters -->
          <a-row :gutter="[16, 12]" align="middle" style="margin-bottom: 16px">
            <a-col :xs="24" :sm="12" :lg="8">
              <a-input-search
                v-model:value="search"
                placeholder="Tìm theo tên, email..."
                allow-clear
                @search="loadEmployees"
                @change="debounceSearch"
              />
            </a-col>
            <a-col :xs="24" :sm="8" :lg="6">
              <a-select
                v-model:value="filterStatus"
                placeholder="Trạng thái"
                allow-clear
                style="width: 100%"
                @change="loadEmployees"
              >
                <a-select-option value="">Tất cả</a-select-option>
                <a-select-option value="Active">Đang làm việc</a-select-option>
                <a-select-option value="Inactive">Đã nghỉ việc</a-select-option>
              </a-select>
            </a-col>
            <a-col :xs="24" :sm="4" :lg="4">
              <span style="font-size: 13px; color: var(--color-text-muted)">
                <UserOutlined /> {{ total }} nhân viên
              </span>
            </a-col>
          </a-row>

          <!-- Table -->
          <a-table
            :data-source="employees"
            :columns="columns"
            :loading="loading"
            :pagination="{
              current: page,
              pageSize: pageSize,
              total,
              showSizeChanger: true,
              showTotal: (t) => `${t} bản ghi`,
            }"
            row-key="id"
            @change="onTableChange"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'fullName'">
                <div style="display: flex; align-items: center; gap: 10px">
                  <a-avatar
                    :style="{
                      background: getColor(record.fullName),
                      fontFamily: 'var(--font-display)',
                      fontWeight: 700,
                    }"
                  >
                    {{ (record.fullName || 'N')[0] }}
                  </a-avatar>
                  <div>
                    <div style="font-weight: 600; color: var(--color-text)">
                      {{ record.fullName }}
                      <a-tag
                        v-if="isWithoutAccount(record.id)"
                        color="orange"
                        style="margin-left: 6px; font-size: 11px; line-height: 18px; padding: 0 6px"
                      >
                        Chưa có TK
                      </a-tag>
                    </div>
                    <div style="font-size: 12px; color: var(--color-text-muted)">
                      {{ record.email }}
                    </div>
                  </div>
                </div>
              </template>
              <template v-else-if="column.key === 'status'">
                <span
                  class="status-badge"
                  :class="record.status === 'Active' ? 'badge-success' : 'badge-default'"
                >
                  <span
                    class="dot"
                    :class="record.status === 'Active' ? 'online' : 'offline'"
                  ></span>
                  {{ record.status === 'Active' ? 'Đang làm' : 'Nghỉ việc' }}
                </span>
              </template>
              <template v-else-if="column.key === 'salary'">
                <span style="font-weight: 600; color: var(--color-text)">
                  {{ formatCurrency(record.baseSalary || record.salary) }}
                </span>
              </template>
              <template v-else-if="column.key === 'actions'">
                <a-space>
                  <a-tooltip title="Xem chi tiết">
                    <a-button size="small" type="text" @click="openView(record)">
                      <EyeOutlined style="color: var(--color-primary)" />
                    </a-button>
                  </a-tooltip>
                  <a-button size="small" type="text" @click="openEdit(record)">
                    <EditOutlined style="color: var(--color-accent-blue)" />
                  </a-button>
                  <a-popconfirm
                    v-if="auth.isAdmin"
                    title="Xác nhận xoá nhân viên này?"
                    ok-text="Xoá"
                    cancel-text="Huỷ"
                    ok-type="danger"
                    @confirm="deleteEmployee(record.id)"
                  >
                    <a-button size="small" type="text" danger>
                      <DeleteOutlined />
                    </a-button>
                  </a-popconfirm>
                </a-space>
              </template>
            </template>
          </a-table>
        </a-tab-pane>

        <!-- ─── TAB 2: TÀI KHOẢN (chỉ Admin / HR) ───────────── -->
        <a-tab-pane v-if="auth.isAdmin || auth.isHR" key="account" tab="Tài khoản">
          <a-alert
            type="info"
            show-icon
            style="margin-bottom: 16px; border-radius: 8px"
            message="Danh sách nhân viên chưa có tài khoản đăng nhập"
            description="Bấm 'Tạo tài khoản' để cấp username/password cho nhân viên. Sau khi tạo, nhân viên có thể đăng nhập vào hệ thống."
          />

          <a-alert
            type="warning"
            show-icon
            style="margin-bottom: 16px; border-radius: 8px"
            message="Lưu ý: Chỉ cần đổi trạng thái nhân viên sang &lsquo;Nghỉ việc&rsquo; là tài khoản bị khóa ngay — không cần xóa bản ghi."
          />

          <a-table
            :data-source="noAccountList"
            :columns="accountColumns"
            :loading="accountLoading"
            row-key="employeeId"
            :pagination="{ pageSize: 10, showTotal: (t) => `${t} nhân viên` }"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'fullName'">
                <div style="display: flex; align-items: center; gap: 10px">
                  <a-avatar
                    :style="{
                      background: getColor(record.fullName),
                      fontFamily: 'var(--font-display)',
                      fontWeight: 700,
                    }"
                  >
                    {{ (record.fullName || 'N')[0] }}
                  </a-avatar>
                  <div>
                    <div style="font-weight: 600">{{ record.fullName }}</div>
                    <div style="font-size: 12px; color: var(--color-text-muted)">
                      {{ record.email }}
                    </div>
                  </div>
                </div>
              </template>
              <template v-else-if="column.key === 'actions'">
                <a-button type="primary" size="small" @click="openCreateAccount(record)">
                  <UserAddOutlined /> Tạo tài khoản
                </a-button>
              </template>
            </template>
            <template #emptyText>
              <a-empty description="Tất cả nhân viên đều đã có tài khoản" />
            </template>
          </a-table>

          <a-divider orientation="left" style="margin-top: 24px; font-weight: 600">
            <SafetyCertificateOutlined /> Tài khoản đã có
          </a-divider>
          <a-alert
            type="info"
            show-icon
            style="margin-bottom: 12px; border-radius: 8px"
            message="Danh sách tài khoản hiện có để xem username, vai trò và trạng thái đăng nhập."
          />
          <a-table
            :data-source="existingAccounts"
            :columns="existingAccountColumns"
            :loading="existingAccountLoading"
            row-key="accountId"
            :pagination="{ pageSize: 10, showTotal: (t) => `${t} tài khoản` }"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'fullName'">
                <div style="display: flex; align-items: center; gap: 8px">
                  <a-avatar
                    :style="{
                      background: getColor(record.fullName),
                      fontFamily: 'var(--font-display)',
                      fontWeight: 700,
                    }"
                  >
                    {{ (record.fullName || 'N')[0] }}
                  </a-avatar>
                  <div>
                    <div style="font-weight: 600">{{ record.fullName }}</div>
                    <div style="font-size: 12px; color: var(--color-text-muted)">
                      {{ record.email || record.username }}
                    </div>
                  </div>
                </div>
              </template>
              <template v-else-if="column.key === 'username'">
                <span style="font-weight: 600">{{ record.username }}</span>
              </template>
              <template v-else-if="column.key === 'role'">
                <a-tag
                  :color="
                    record.role === 'Admin'
                      ? 'red'
                      : record.role === 'HR'
                        ? 'blue'
                        : record.role === 'Manager'
                          ? 'gold'
                          : 'default'
                  "
                >
                  {{ getRoleLabel(record.role) }}
                </a-tag>
              </template>
              <template v-else-if="column.key === 'accountStatus'">
                <span
                  class="status-badge"
                  :class="record.accountStatus === 'active' ? 'badge-success' : 'badge-default'"
                >
                  <span
                    class="dot"
                    :class="record.accountStatus === 'active' ? 'online' : 'offline'"
                  ></span>
                  {{ record.accountStatusLabel }}
                </span>
              </template>
              <template v-else-if="column.key === 'actions'">
                <a-space wrap>
                  <a-button
                    size="small"
                    @click="openChangeRole(record)"
                    :disabled="!canManageAccountRole(record)"
                  >
                    <SafetyCertificateOutlined /> Đổi role
                  </a-button>
                  <a-button
                    size="small"
                    @click="openResetPassword(record)"
                    :disabled="!record.canResetPassword"
                  >
                    <KeyOutlined /> Reset mật khẩu
                  </a-button>
                </a-space>
              </template>
            </template>
            <template #emptyText>
              <a-empty description="Chưa có tài khoản nào để quản lý" />
            </template>
          </a-table>

          <!-- Danh sách NV có TK nhưng đã bị khóa (Inactive) -->
          <a-divider orientation="left" style="margin-top: 24px; font-weight: 600">
            <LockOutlined /> Nhân viên đã nghỉ việc — tài khoản bị khóa
          </a-divider>
          <a-alert
            type="success"
            show-icon
            style="margin-bottom: 12px; border-radius: 8px"
            message="Nhân viên Inactive không thể đăng nhập. Bản ghi được giữ nguyên để phục vụ lịch sử lương, thuế TNCN và thanh tra lao động."
          />
          <a-table
            :data-source="employees.filter((e) => e.status === 'Inactive')"
            :columns="lockedAccountColumns"
            size="small"
            row-key="id"
            :pagination="{ pageSize: 10, showTotal: (t) => `${t} nhân viên` }"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'fullName'">
                <div style="display: flex; align-items: center; gap: 8px">
                  <a-avatar
                    :style="{
                      background: getColor(record.fullName),
                      fontFamily: 'var(--font-display)',
                      fontWeight: 700,
                    }"
                  >
                    {{ (record.fullName || 'N')[0] }}
                  </a-avatar>
                  <div>
                    <div style="font-weight: 600">{{ record.fullName }}</div>
                    <div style="font-size: 12px; color: var(--color-text-muted)">
                      {{ record.email }}
                    </div>
                  </div>
                </div>
              </template>
              <template v-else-if="column.key === 'accountStatus'">
                <span class="status-badge badge-default">
                  <span class="dot offline"></span> Tài khoản khóa
                </span>
              </template>
              <template v-else-if="column.key === 'actions'">
                <a-button
                  v-if="auth.isAdmin"
                  size="small"
                  type="text"
                  @click="openResetPassword({ ...record, employeeId: record.id })"
                >
                  <KeyOutlined /> Đặt lại mật khẩu
                </a-button>
              </template>
            </template>
          </a-table>
        </a-tab-pane>
      </a-tabs>
    </a-card>

    <!-- ─── Modal: Create / Edit Employee ──────────────────────── -->
    <a-modal
      v-model:open="modalOpen"
      :title="editingId ? 'Chỉnh sửa nhân viên' : 'Thêm nhân viên mới'"
      :confirm-loading="saving"
      ok-text="Lưu"
      cancel-text="Huỷ"
      width="680px"
      @ok="saveEmployee"
      @cancel="resetForm"
      destroy-on-close
    >
      <a-form :model="form" layout="vertical" ref="formRef" :rules="rules">
        <a-divider orientation="left" style="margin-top: 0; font-weight: 600"
          ><UserOutlined /> Thông tin cơ bản & Công việc</a-divider
        >
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item name="employeeCode" label="Mã nhân viên">
              <a-input
                v-model:value="form.employeeCode"
                placeholder="Mã NV (VD: NV001)"
                :disabled="!!editingId"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="fullName" label="Họ và tên">
              <a-input v-model:value="form.fullName" placeholder="Nguyễn Văn A" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="email" label="Email">
              <a-input v-model:value="form.email" placeholder="email@company.com" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="phone" label="Số điện thoại">
              <a-input v-model:value="form.phone" placeholder="0912 345 678" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="departmentId" label="Phòng ban">
              <a-select
                v-model:value="form.departmentId"
                placeholder="Chọn phòng ban"
                style="width: 100%"
                allow-clear
                show-search
                :filter-option="filterDept"
                :options="departmentOptions"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="position" label="Chức vụ">
              <a-input v-model:value="form.position" placeholder="Lập trình viên" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="hireDate" label="Ngày vào làm">
              <a-date-picker
                v-model:value="form.hireDate"
                format="DD/MM/YYYY"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="status" label="Trạng thái">
              <a-select v-model:value="form.status" style="width: 100%">
                <a-select-option value="Active">Đang làm việc</a-select-option>
                <a-select-option value="Inactive">Nghỉ việc</a-select-option>
              </a-select>
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="contractType" label="Loại hình công việc">
              <a-select v-model:value="form.contractType" style="width: 100%">
                <a-select-option value="Full-time">Chính thức (Full-time)</a-select-option>
                <a-select-option value="Part-time">Bán thời gian (Part-time)</a-select-option>
                <a-select-option value="Probation">Thử việc (Probation)</a-select-option>
              </a-select>
            </a-form-item>
          </a-col>
          <a-col :span="24">
            <a-form-item name="address" label="Địa chỉ">
              <a-textarea v-model:value="form.address" :rows="2" placeholder="Địa chỉ nhà..." />
            </a-form-item>
          </a-col>
        </a-row>

        <a-divider orientation="left" style="font-weight: 600"
          ><SafetyCertificateOutlined /> Thông tin cá nhân & Thuế</a-divider
        >
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item name="identityNumber" label="Số CMND/CCCD">
              <a-input v-model:value="form.identityNumber" placeholder="Số định danh công dân..." />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="taxCode" label="Mã số thuế">
              <a-input v-model:value="form.taxCode" placeholder="Mã số thuế cá nhân..." />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="dependentsCount" label="Số người phụ thuộc">
              <a-input-number
                v-model:value="form.dependentsCount"
                :min="0"
                :max="100"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="baseSalary">
              <template #label>
                Lương cơ bản (VND)
                <a-tooltip
                  v-if="auth.isAdmin"
                  title="Admin không được sửa lương cơ bản trực tiếp. Điều chỉnh thông qua hợp đồng lao động."
                >
                  <LockOutlined
                    style="color: var(--color-text-muted); margin-left: 4px; font-size: 12px"
                  />
                </a-tooltip>
              </template>
              <a-input-number
                v-model:value="form.baseSalary"
                :disabled="auth.isAdmin && !!editingId"
                :formatter="(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')"
                :parser="(v) => v.replace(/,/g, '')"
                style="width: 100%"
                placeholder="10,000,000"
              />
              <div
                v-if="auth.isAdmin && !!editingId"
                style="font-size: 11px; color: var(--color-text-muted); margin-top: 3px"
              >
                🔒 Chỉ có thể điều chỉnh qua hợp đồng lao động
              </div>
            </a-form-item>
          </a-col>
        </a-row>

        <a-divider orientation="left" style="font-weight: 600"
          ><BankOutlined /> Tài khoản ngân hàng</a-divider
        >
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item name="bankName" label="Tên ngân hàng">
              <a-input
                v-model:value="form.bankName"
                placeholder="Ví dụ: Vietcombank, Techcombank..."
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="bankAccountNumber" label="Số tài khoản">
              <a-input
                v-model:value="form.bankAccountNumber"
                placeholder="Số tài khoản ngân hàng..."
              />
            </a-form-item>
          </a-col>
          <a-col :span="24">
            <a-form-item name="bankBranch" label="Chi nhánh">
              <a-input
                v-model:value="form.bankBranch"
                placeholder="Ví dụ: Chi nhánh Hà Nội, Hoàn Kiếm..."
              />
            </a-form-item>
          </a-col>
        </a-row>
      </a-form>
    </a-modal>

    <!-- ─── Modal: Xem chi tiết nhân viên ────────────────────── -->
    <a-modal
      v-model:open="viewModalOpen"
      title="Thông tin nhân viên chi tiết"
      :footer="null"
      width="850px"
      destroy-on-close
    >
      <div v-if="viewRecord" style="padding: 4px 0">
        <!-- Avatar + tên -->
        <div
          style="
            display: flex;
            align-items: center;
            gap: 14px;
            margin-bottom: 20px;
            padding-bottom: 16px;
            border-bottom: 1px solid var(--color-border);
          "
        >
          <a-avatar
            :size="56"
            :style="{
              background: getColor(viewRecord.fullName),
              fontFamily: 'var(--font-display)',
              fontWeight: 700,
              fontSize: '22px',
            }"
          >
            {{ (viewRecord.fullName || 'N')[0] }}
          </a-avatar>
          <div style="flex: 1">
            <div style="display: flex; align-items: center; gap: 8px">
              <span style="font-size: 18px; font-weight: 700; color: var(--color-text)">{{
                viewRecord.fullName
              }}</span>
              <a-tag v-if="isWithoutAccount(viewRecord.id)" color="orange">Chưa có TK</a-tag>
            </div>
            <div style="font-size: 13px; color: var(--color-text-muted); margin-top: 2px">
              {{ viewRecord.email }}
            </div>
            <span
              class="status-badge"
              :class="viewRecord.status === 'Active' ? 'badge-success' : 'badge-default'"
              style="margin-top: 6px; display: inline-flex"
            >
              <span
                class="dot"
                :class="viewRecord.status === 'Active' ? 'online' : 'offline'"
              ></span>
              {{ viewRecord.status === 'Active' ? 'Đang làm việc' : 'Nghỉ việc' }}
            </span>
          </div>
        </div>

        <a-tabs v-model:activeKey="detailActiveTab" @change="onDetailTabChange">
          <!-- Tab 1: Thông tin chung -->
          <a-tab-pane key="general" tab="Thông tin cá nhân & Công việc">
            <a-descriptions :column="2" size="small" bordered style="margin-top: 10px">
              <a-descriptions-item label="Mã nhân viên" :span="1">{{
                viewRecord.employeeCode || '—'
              }}</a-descriptions-item>
              <a-descriptions-item label="Số CMND/CCCD" :span="1">{{
                viewRecord.identityNumber || '—'
              }}</a-descriptions-item>
              <a-descriptions-item label="Số điện thoại" :span="1">{{
                viewRecord.phone || '—'
              }}</a-descriptions-item>
              <a-descriptions-item label="Phòng ban" :span="1">{{
                departmentMap[viewRecord.departmentId] || viewRecord.departmentName || '—'
              }}</a-descriptions-item>
              <a-descriptions-item label="Chức vụ" :span="1">{{
                viewRecord.position || '—'
              }}</a-descriptions-item>
              <a-descriptions-item label="Ngày vào làm" :span="1">{{
                viewRecord.hireDate ? dayjs(viewRecord.hireDate).format('DD/MM/YYYY') : '—'
              }}</a-descriptions-item>
              <a-descriptions-item label="Địa chỉ" :span="2">{{
                viewRecord.address || '—'
              }}</a-descriptions-item>
            </a-descriptions>
          </a-tab-pane>

          <!-- Tab 2: Thuế & Tài chính -->
          <a-tab-pane key="finance" tab="Thuế, Lương & Ngân hàng">
            <a-descriptions :column="2" size="small" bordered style="margin-top: 10px">
              <a-descriptions-item label="Lương cơ bản" :span="1">
                <span style="font-weight: 600; color: var(--color-primary)">{{
                  formatCurrency(viewRecord.baseSalary || viewRecord.salary)
                }}</span>
              </a-descriptions-item>
              <a-descriptions-item label="Mã số thuế" :span="1">{{
                viewRecord.taxCode || '—'
              }}</a-descriptions-item>
              <a-descriptions-item label="Người phụ thuộc" :span="1"
                >{{ viewRecord.dependentsCount ?? 0 }} người</a-descriptions-item
              >
              <a-descriptions-item label="Loại hợp đồng hiện tại" :span="1">
                <a-tag color="blue">{{ viewRecord.contractType || 'Chưa ký HĐ' }}</a-tag>
              </a-descriptions-item>
              <a-descriptions-item label="Tên ngân hàng" :span="1">{{
                viewRecord.bankName || '—'
              }}</a-descriptions-item>
              <a-descriptions-item label="Số tài khoản" :span="1">{{
                viewRecord.bankAccountNumber || '—'
              }}</a-descriptions-item>
              <a-descriptions-item label="Chi nhánh ngân hàng" :span="2">{{
                viewRecord.bankBranch || '—'
              }}</a-descriptions-item>
            </a-descriptions>
          </a-tab-pane>

          <!-- Tab 3: Hợp đồng lao động -->
          <a-tab-pane key="contracts" tab="Hợp đồng lao động" v-if="auth.isAdmin || auth.isHR">
            <div
              style="
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: 12px;
                margin-top: 10px;
              "
            >
              <span style="font-weight: 600; color: var(--color-text)"
                ><HistoryOutlined /> Lịch sử hợp đồng</span
              >
              <a-button type="primary" size="small" @click="openCreateContract">
                <PlusOutlined /> Ký hợp đồng mới
              </a-button>
            </div>

            <a-table
              :data-source="contracts"
              :columns="contractColumns"
              :loading="contractsLoading"
              size="small"
              row-key="id"
              :pagination="{ pageSize: 5 }"
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
                    {{
                      record.endDate ? dayjs(record.endDate).format('DD/MM/YYYY') : 'Không thời hạn'
                    }}
                  </span>
                </template>
                <template v-else-if="column.key === 'basicSalary'">
                  <span style="font-weight: 600">{{ formatCurrency(record.basicSalary) }}</span>
                  <div style="font-size: 11px; color: var(--color-text-muted)">
                    Hệ số: {{ record.salaryRatio }}
                  </div>
                </template>
                <template v-else-if="column.key === 'status'">
                  <a-tag :color="getContractStatusColor(record.status)">
                    {{ getContractStatusText(record.status) }}
                  </a-tag>
                </template>
                <template v-else-if="column.key === 'actions'">
                  <a-space>
                    <a-button size="small" type="text" @click="openEditContract(record)">
                      <EditOutlined style="color: var(--color-accent-blue)" />
                    </a-button>
                    <a-popconfirm
                      title="Xác nhận xoá hợp đồng này?"
                      ok-text="Xoá"
                      cancel-text="Huỷ"
                      ok-type="danger"
                      @confirm="deleteContract(record.id)"
                    >
                      <a-button size="small" type="text" danger>
                        <DeleteOutlined />
                      </a-button>
                    </a-popconfirm>
                  </a-space>
                </template>
              </template>
            </a-table>
          </a-tab-pane>
        </a-tabs>
      </div>
    </a-modal>

    <!-- ─── Modal: Create / Edit Contract ──────────────────────── -->
    <a-modal
      v-model:open="contractFormModalOpen"
      :title="editingContractId ? 'Chỉnh sửa hợp đồng' : 'Ký hợp đồng mới'"
      :confirm-loading="savingContract"
      ok-text="Lưu"
      cancel-text="Huỷ"
      width="580px"
      @ok="saveContract"
      destroy-on-close
    >
      <a-form :model="contractForm" layout="vertical" ref="contractFormRef" :rules="contractRules">
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item name="contractNumber" label="Số hợp đồng">
              <a-input
                v-model:value="contractForm.contractNumber"
                placeholder="Ví dụ: HDLD/2026/0001"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="contractTypeId" label="Loại hợp đồng">
              <a-select
                v-model:value="contractForm.contractTypeId"
                placeholder="Chọn loại hợp đồng"
                style="width: 100%"
                @change="onContractTypeChange"
              >
                <a-select-option v-for="type in contractTypes" :key="type.id" :value="type.id">
                  {{ type.name }}
                </a-select-option>
              </a-select>
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="startDate" label="Ngày bắt đầu">
              <a-date-picker
                v-model:value="contractForm.startDate"
                format="DD/MM/YYYY"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="endDate" label="Ngày kết thúc">
              <a-date-picker
                v-model:value="contractForm.endDate"
                format="DD/MM/YYYY"
                style="width: 100%"
                placeholder="Bỏ trống nếu không thời hạn"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="basicSalary" label="Lương cơ bản trên HĐ (VND)">
              <a-input-number
                v-model:value="contractForm.basicSalary"
                :formatter="(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')"
                :parser="(v) => v.replace(/,/g, '')"
                style="width: 100%"
                placeholder="10,000,000"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="salaryRatio" label="Tỷ lệ hưởng lương">
              <a-input-number
                v-model:value="contractForm.salaryRatio"
                :min="0.1"
                :max="2.0"
                :step="0.05"
                style="width: 100%"
                placeholder="1.0"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="signDate" label="Ngày ký">
              <a-date-picker
                v-model:value="contractForm.signDate"
                format="DD/MM/YYYY"
                style="width: 100%"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="status" label="Trạng thái">
              <a-select v-model:value="contractForm.status" style="width: 100%">
                <a-select-option value="Draft">Bản nháp (Draft)</a-select-option>
                <a-select-option value="Active">Hiệu lực (Active)</a-select-option>
                <a-select-option value="Expired">Hết hạn (Expired)</a-select-option>
                <a-select-option value="Terminated">Chấm dứt (Terminated)</a-select-option>
              </a-select>
            </a-form-item>
          </a-col>
        </a-row>
        <a-form-item name="notes" label="Ghi chú">
          <a-textarea
            v-model:value="contractForm.notes"
            :rows="2"
            placeholder="Ghi chú thêm về hợp đồng..."
          />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- ─── Modal: Create Account ──────────────────────────────── -->
    <a-modal
      v-model:open="accountModalOpen"
      title="Tạo tài khoản cho nhân viên"
      :confirm-loading="creatingAccount"
      ok-text="Tạo tài khoản"
      cancel-text="Huỷ"
      width="520px"
      @ok="submitCreateAccount"
      @cancel="resetAccountForm"
      destroy-on-close
    >
      <a-alert
        v-if="accountTarget"
        type="success"
        show-icon
        style="margin-bottom: 16px; border-radius: 8px"
      >
        <template #message>
          Tạo tài khoản cho: <b>{{ accountTarget.fullName }}</b>
          <span style="color: var(--color-text-muted)"> ({{ accountTarget.employeeCode }}) </span>
        </template>
      </a-alert>

      <a-form :model="accountForm" layout="vertical" ref="accountFormRef" :rules="accountRules">
        <a-form-item name="username" label="Tên đăng nhập">
          <a-input v-model:value="accountForm.username" placeholder="vd: nguyenvana" allow-clear />
        </a-form-item>
        <a-form-item name="password" label="Mật khẩu">
          <a-input-password v-model:value="accountForm.password" placeholder="Tối thiểu 6 ký tự" />
        </a-form-item>
        <a-form-item name="role" label="Vai trò">
          <a-select v-model:value="accountForm.role" style="width: 100%">
            <a-select-option v-for="role in assignableRoles" :key="role" :value="role">
              {{ role }} — {{ getRoleLabel(role) }}
            </a-select-option>
          </a-select>
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal
      v-model:open="changeRoleModalOpen"
      title="Đổi vai trò tài khoản"
      :confirm-loading="changingRole"
      ok-text="Lưu thay đổi"
      cancel-text="Huỷ"
      width="460px"
      @ok="submitChangeRole"
      @cancel="resetChangeRoleForm"
      destroy-on-close
    >
      <a-alert
        v-if="changeRoleTarget"
        type="info"
        show-icon
        style="margin-bottom: 16px; border-radius: 8px"
      >
        <template #message>
          Đổi role cho: <b>{{ changeRoleTarget.fullName }}</b>
        </template>
        <template #description>
          Username: <b>{{ changeRoleTarget.username }}</b>
        </template>
      </a-alert>
      <a-form layout="vertical">
        <a-form-item label="Vai trò mới">
          <a-select v-model:value="changeRoleForm.role" style="width: 100%">
            <a-select-option v-for="role in assignableRoles" :key="role" :value="role">
              {{ role }} — {{ getRoleLabel(role) }}
            </a-select-option>
          </a-select>
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- ─── Modal: Đặt lại mật khẩu ─────────────────────────────── -->
    <a-modal
      v-model:open="resetPassModalOpen"
      title="Đặt lại mật khẩu"
      :confirm-loading="resettingPass"
      ok-text="Xác nhận"
      cancel-text="Huỷ"
      width="420px"
      @ok="submitResetPassword"
      @cancel="resetPassModalOpen = false"
      destroy-on-close
    >
      <a-alert
        v-if="resetPassTarget"
        type="warning"
        show-icon
        style="margin-bottom: 16px; border-radius: 8px"
      >
        <template #message>
          Đặt lại mật khẩu cho: <b>{{ resetPassTarget.fullName }}</b>
          <div style="font-size: 12px; color: var(--color-text-muted); margin-top: 2px">
            Tài khoản đang bị khóa (Nghỉ việc). Chỉ Admin mới thực hiện được thao tác này.
          </div>
        </template>
      </a-alert>
      <a-form layout="vertical">
        <a-form-item label="Mật khẩu mới">
          <a-input-password
            v-model:value="newPassword"
            placeholder="Tối thiểu 6 ký tự"
            allow-clear
          />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { employeeApi, authApi, departmentApi, contractApi } from '@/api'
import { message, Modal } from 'ant-design-vue'
import dayjs from 'dayjs'
import {
  getAssignableRoles,
  getRoleLabel,
  normalizeAccountRecords,
} from '@/utils/accountManagement'
import {
  UserOutlined,
  PlusOutlined,
  ReloadOutlined,
  EditOutlined,
  DeleteOutlined,
  UserAddOutlined,
  EyeOutlined,
  LockOutlined,
  SafetyCertificateOutlined,
  BankOutlined,
  HistoryOutlined,
  KeyOutlined,
} from '@ant-design/icons-vue'

const auth = useAuthStore()

// ─── Tab state ──────────────────────────────────────────────────
const activeTab = ref('list')

async function onTabChange(key) {
  if (key === 'account') {
    await loadNoAccountList()
    await loadExistingAccounts()
  }
}

// ─── Tab 1: Employees CRUD (giữ nguyên logic cũ) ────────────────
const loading = ref(false)
const saving = ref(false)
const employees = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(10)
const search = ref('')
const filterStatus = ref('')
const modalOpen = ref(false)
const editingId = ref(null)
const formRef = ref()

// ─── View modal ─────────────────────────────────────────────────
const viewModalOpen = ref(false)
const viewRecord = ref(null)
const detailActiveTab = ref('general')

function openView(rec) {
  viewRecord.value = rec
  detailActiveTab.value = 'general'
  viewModalOpen.value = true
}

function onDetailTabChange(key) {
  if (key === 'contracts') {
    loadContracts(viewRecord.value.id)
    loadContractTypes()
  }
}

// ─── Contract Management ──────────────────────────────────────────
const contracts = ref([])
const contractsLoading = ref(false)
const contractTypes = ref([])

const contractFormModalOpen = ref(false)
const editingContractId = ref(null)
const savingContract = ref(false)
const contractFormRef = ref()

const contractForm = reactive({
  contractNumber: '',
  contractTypeId: null,
  startDate: null,
  endDate: null,
  basicSalary: null,
  salaryRatio: 1.0,
  signDate: null,
  status: 'Draft',
  notes: '',
})

const contractRules = {
  contractNumber: [{ required: true, message: 'Số hợp đồng không được trống' }],
  contractTypeId: [{ required: true, message: 'Vui lòng chọn loại hợp đồng' }],
  startDate: [{ required: true, message: 'Vui lòng chọn ngày bắt đầu' }],
  basicSalary: [{ required: true, message: 'Vui lòng nhập lương cơ bản' }],
  salaryRatio: [{ required: true, message: 'Vui lòng nhập tỷ lệ lương' }],
  signDate: [{ required: true, message: 'Vui lòng chọn ngày ký' }],
  status: [{ required: true, message: 'Vui lòng chọn trạng thái' }],
}

const contractColumns = [
  { title: 'Số hợp đồng', key: 'contractNumber', dataIndex: 'contractNumber' },
  { title: 'Loại hợp đồng', key: 'contractTypeName', dataIndex: 'contractTypeName' },
  { title: 'Thời hạn', key: 'duration', dataIndex: 'startDate' },
  { title: 'Lương cơ bản', key: 'basicSalary', dataIndex: 'basicSalary', align: 'right' },
  { title: 'Trạng thái', key: 'status', dataIndex: 'status', width: 100 },
  { title: '', key: 'actions', width: 100, align: 'center' },
]

async function loadContracts(employeeId) {
  if (!employeeId) return
  contractsLoading.value = true
  try {
    const res = await contractApi.getAll({ employeeId })
    const data = res.data
    contracts.value = Array.isArray(data) ? data : data.data || data.items || data.contracts || []
  } catch (e) {
    message.error('Không tải được danh sách hợp đồng')
  } finally {
    contractsLoading.value = false
  }
}

async function loadContractTypes() {
  try {
    const res = await contractApi.getTypes()
    const data = res.data
    contractTypes.value = Array.isArray(data)
      ? data
      : data.data || data.items || data.contractTypes || []
  } catch (e) {
    message.error('Không tải được danh sách loại hợp đồng')
  }
}

function onContractTypeChange(typeId) {
  const selectedType = contractTypes.value.find((t) => t.id === typeId)
  if (selectedType) {
    contractForm.salaryRatio = selectedType.defaultSalaryRatio || 1.0
  }
}

function openCreateContract() {
  editingContractId.value = null
  Object.assign(contractForm, {
    contractNumber: '',
    contractTypeId: null,
    startDate: null,
    endDate: null,
    basicSalary: null,
    salaryRatio: 1.0,
    signDate: dayjs(),
    status: 'Draft',
    notes: '',
  })
  contractFormModalOpen.value = true
}

function openEditContract(record) {
  editingContractId.value = record.id
  Object.assign(contractForm, {
    contractNumber: record.contractNumber || '',
    contractTypeId: record.contractTypeId || null,
    startDate: record.startDate ? dayjs(record.startDate) : null,
    endDate: record.endDate ? dayjs(record.endDate) : null,
    basicSalary: record.basicSalary || null,
    salaryRatio: record.salaryRatio || 1.0,
    signDate: record.signDate ? dayjs(record.signDate) : null,
    status: record.status || 'Draft',
    notes: record.notes || '',
  })
  contractFormModalOpen.value = true
}

async function saveContract() {
  try {
    await contractFormRef.value.validate()
  } catch {
    return
  }
  savingContract.value = true
  const payload = {
    ...contractForm,
    employeeId: viewRecord.value.id,
    startDate: contractForm.startDate ? contractForm.startDate.toISOString() : null,
    endDate: contractForm.endDate ? contractForm.endDate.toISOString() : null,
    signDate: contractForm.signDate ? contractForm.signDate.toISOString() : null,
  }
  try {
    if (editingContractId.value) {
      await contractApi.update(editingContractId.value, payload)
      message.success('Đã cập nhật hợp đồng lao động')
    } else {
      await contractApi.create(payload)
      message.success('Đã ký hợp đồng lao động mới')
    }
    contractFormModalOpen.value = false
    loadContracts(viewRecord.value.id)
    loadEmployees()
  } catch (e) {
    message.error(e.response?.data?.message || 'Lưu hợp đồng thất bại')
  } finally {
    savingContract.value = false
  }
}

async function deleteContract(id) {
  try {
    await contractApi.remove(id)
    message.success('Đã xoá hợp đồng lao động')
    loadContracts(viewRecord.value.id)
    loadEmployees()
  } catch (e) {
    message.error(e.response?.data?.message || 'Xoá hợp đồng thất bại')
  }
}

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
      return 'Bản nháp'
    default:
      return status
  }
}

// ─── Departments ────────────────────────────────────────────────
const departments = ref([]) // flat list { id, name }

function flattenDepts(nodes, result = []) {
  for (const n of nodes) {
    if (n.id && n.name) result.push({ id: n.id, name: n.name })
    if (n.children?.length) flattenDepts(n.children, result)
  }
  return result
}

async function loadDepartments() {
  try {
    const res = await departmentApi.getTree()
    const raw = res.data
    const nodes = Array.isArray(raw) ? raw : raw.data || raw.departments || []
    departments.value = flattenDepts(nodes)
  } catch {
    /* non-critical */
  }
}

const departmentOptions = computed(() =>
  departments.value.map((d) => ({ value: d.id, label: d.name })),
)

const departmentMap = computed(() =>
  Object.fromEntries(departments.value.map((d) => [d.id, d.name])),
)

function filterDept(input, option) {
  return option.label.toLowerCase().includes(input.toLowerCase())
}

const form = reactive({
  employeeCode: '',
  fullName: '',
  email: '',
  phone: '',
  departmentId: null,
  position: '',
  baseSalary: null,
  hireDate: null,
  address: '',
  status: 'Active',
  contractType: 'Full-time',
  identityNumber: '',
  taxCode: '',
  dependentsCount: 0,
  bankName: '',
  bankAccountNumber: '',
  bankBranch: '',
})

const rules = {
  employeeCode: [{ required: true, message: 'Vui lòng nhập mã nhân viên' }],
  fullName: [{ required: true, message: 'Họ tên không được trống' }],
  email: [{ required: true, type: 'email', message: 'Email không hợp lệ' }],
  departmentId: [
    {
      validator: (_, val) => {
        if (!val) return Promise.resolve() // optional field
        if (departments.value.find((d) => d.id === val)) return Promise.resolve()
        return Promise.reject('Phòng ban không hợp lệ')
      },
      trigger: 'change',
    },
  ],
}

const columns = [
  { title: 'Nhân viên', key: 'fullName', dataIndex: 'fullName', sorter: true },
  { title: 'Phòng ban', key: 'departmentName', dataIndex: 'departmentName', ellipsis: true },
  { title: 'Chức vụ', key: 'position', dataIndex: 'position', ellipsis: true },
  { title: 'Lương CB', key: 'salary', dataIndex: 'baseSalary', width: 140 },
  { title: 'Trạng thái', key: 'status', dataIndex: 'status', width: 130 },
  { title: '', key: 'actions', width: 120, align: 'center' },
]

// ─── Tab 2: Account management ──────────────────────────────────
const accountLoading = ref(false)
const noAccountList = ref([]) // AccountDto[]
const existingAccountLoading = ref(false)
const existingAccounts = ref([])
const accountModalOpen = ref(false)
const creatingAccount = ref(false)
const accountFormRef = ref()
const accountTarget = ref(null) // { employeeId, fullName, employeeCode, ... }
const changeRoleModalOpen = ref(false)
const changingRole = ref(false)
const changeRoleTarget = ref(null)

const accountForm = reactive({
  username: '',
  password: '',
  role: 'Employee',
})
const changeRoleForm = reactive({
  role: 'Employee',
})
const accountRules = {
  username: [
    { required: true, message: 'Tên đăng nhập không được trống' },
    { max: 50, message: 'Tối đa 50 ký tự' },
  ],
  password: [
    { required: true, message: 'Mật khẩu không được trống' },
    { min: 6, message: 'Mật khẩu tối thiểu 6 ký tự' },
  ],
  role: [{ required: true, message: 'Vui lòng chọn vai trò' }],
}

const accountColumns = [
  { title: 'Nhân viên', key: 'fullName', dataIndex: 'fullName' },
  { title: 'Mã NV', key: 'employeeCode', dataIndex: 'employeeCode', width: 120 },
  { title: 'Phòng ban', key: 'departmentName', dataIndex: 'departmentName', ellipsis: true },
  { title: '', key: 'actions', width: 160, align: 'center' },
]

const existingAccountColumns = [
  { title: 'Nhân viên', key: 'fullName', dataIndex: 'fullName' },
  { title: 'Username', key: 'username', dataIndex: 'username', width: 160 },
  { title: 'Mã NV', key: 'employeeCode', dataIndex: 'employeeCode', width: 120 },
  { title: 'Vai trò', key: 'role', dataIndex: 'role', width: 130 },
  { title: 'Trạng thái', key: 'accountStatus', dataIndex: 'accountStatus', width: 180 },
  { title: '', key: 'actions', width: 220, align: 'center' },
]

const lockedAccountColumns = [
  { title: 'Nhân viên', key: 'fullName', dataIndex: 'fullName' },
  { title: 'Mã NV', key: 'employeeCode', dataIndex: 'employeeCode', width: 120 },
  { title: 'Phòng ban', key: 'departmentName', dataIndex: 'departmentName', ellipsis: true },
  { title: 'TK đăng nhập', key: 'accountStatus', width: 140 },
  { title: '', key: 'actions', width: 160, align: 'center' },
]

// ─── Helpers ─────────────────────────────────────────────────────
const colors = ['#00b14f', '#00b4d8', '#7c5cfc', '#ffb020', '#ff4757', '#1a2332']
const getColor = (name) => colors[(name || '').charCodeAt(0) % colors.length]
const formatCurrency = (v) => (v ? new Intl.NumberFormat('vi-VN').format(v) + ' ₫' : '—')
const assignableRoles = computed(() => getAssignableRoles(auth.userRole))

// Set lookup để check "Chưa có TK" nhanh O(1)
const noAccountIdSet = computed(() => new Set(noAccountList.value.map((x) => x.employeeId)))
function isWithoutAccount(empId) {
  return noAccountIdSet.value.has(empId)
}

function canManageAccountRole(record) {
  if (!record?.employeeId) return false
  if (auth.isAdmin) return true
  return auth.isHR && record.role !== 'Admin'
}

// ─── Loaders ─────────────────────────────────────────────────────
let debTimer
function debounceSearch() {
  clearTimeout(debTimer)
  debTimer = setTimeout(loadEmployees, 400)
}

async function loadEmployees() {
  loading.value = true
  try {
    const res = await employeeApi.getAll({
      page: page.value,
      pageSize: pageSize.value,
      search: search.value || undefined,
      status: filterStatus.value || undefined,
    })
    const data = res.data
    employees.value = Array.isArray(data) ? data : data.data || data.items || data.employees || []
    total.value = data.total || data.totalCount || employees.value.length
  } catch (e) {
    message.error('Không tải được danh sách nhân viên')
  } finally {
    loading.value = false
  }
}

async function loadNoAccountList() {
  accountLoading.value = true
  try {
    const res = await authApi.getEmployeesWithoutAccount()
    const data = res.data
    noAccountList.value = Array.isArray(data) ? data : data.data || []
  } catch (e) {
    message.error('Không tải được danh sách nhân viên chưa có tài khoản')
  } finally {
    accountLoading.value = false
  }
}

async function loadExistingAccounts() {
  existingAccountLoading.value = true
  try {
    const res = await authApi.listAccounts()
    const data = res.data
    const items = Array.isArray(data) ? data : data.data || data.items || data.accounts || []
    existingAccounts.value = normalizeAccountRecords(items, employees.value)
  } catch (e) {
    message.error(e.response?.data?.message || 'Không tải được danh sách tài khoản')
  } finally {
    existingAccountLoading.value = false
  }
}

async function loadAll() {
  await loadEmployees()
  if (activeTab.value === 'account') {
    await loadNoAccountList()
    await loadExistingAccounts()
  }
}

function onTableChange(pag) {
  page.value = pag.current
  pageSize.value = pag.pageSize
  loadEmployees()
}

// ─── Employee CRUD (giữ nguyên) ─────────────────────────────────
function openCreate() {
  editingId.value = null
  Object.assign(form, {
    employeeCode: '',
    fullName: '',
    email: '',
    phone: '',
    departmentId: null,
    position: '',
    baseSalary: null,
    hireDate: null,
    address: '',
    status: 'Active',
    contractType: 'Full-time',
    identityNumber: '',
    taxCode: '',
    dependentsCount: 0,
    bankName: '',
    bankAccountNumber: '',
    bankBranch: '',
  })
  modalOpen.value = true
}
function openEdit(rec) {
  editingId.value = rec.id
  Object.assign(form, {
    employeeCode: rec.employeeCode || '',
    fullName: rec.fullName || '',
    email: rec.email || '',
    phone: rec.phone || '',
    departmentId: rec.departmentId || null,
    position: rec.position || '',
    baseSalary: rec.baseSalary || rec.salary || null,
    hireDate: rec.hireDate ? dayjs(rec.hireDate) : null,
    address: rec.address || '',
    status: rec.status || 'Active',
    contractType: rec.contractType || 'Full-time',
    identityNumber: rec.identityNumber || '',
    taxCode: rec.taxCode || '',
    dependentsCount: rec.dependentsCount || 0,
    bankName: rec.bankName || '',
    bankAccountNumber: rec.bankAccountNumber || '',
    bankBranch: rec.bankBranch || '',
  })
  modalOpen.value = true
}
function resetForm() {
  modalOpen.value = false
  editingId.value = null
}

async function saveEmployee() {
  try {
    await formRef.value.validate()
  } catch {
    return
  }

  // Cảnh báo: đổi sang Inactive sẽ khóa TK đăng nhập
  const wasActive = employees.value.find((e) => e.id === editingId.value)?.status === 'Active'
  if (editingId.value && wasActive && form.status === 'Inactive') {
    const confirmed = await new Promise((resolve) => {
      Modal.confirm({
        title: 'Xác nhận khóa tài khoản',
        content:
          'Chuyển sang “Nghỉ việc” sẽ khóa tài khoản đăng nhập của nhân viên này ngay lập tức. Họ sẽ không thể đăng nhập vào hệ thống. Bản ghi được giữ nguyên để lưu lịch sử. Tiếp tục?',
        okText: 'Khóa tài khoản',
        okType: 'danger',
        cancelText: 'Huỷ',
        onOk: () => resolve(true),
        onCancel: () => resolve(false),
      })
    })
    if (!confirmed) return
  }

  saving.value = true
  const payload = {
    ...form,
    hireDate: form.hireDate ? form.hireDate.toISOString() : undefined,
    contractType: form.contractType || 'Full-time',
  }
  try {
    if (editingId.value) {
      await employeeApi.update(editingId.value, payload)
      if (form.status === 'Inactive' && wasActive)
        message.warning('Nhân viên đã được chuyển sang Nghỉ việc — tài khoản đăng nhập đã bị khóa')
      else message.success('Đã cập nhật nhân viên')
    } else {
      await employeeApi.create(payload)
      message.success('Đã thêm nhân viên')
    }
    modalOpen.value = false
    await loadEmployees()
    if (activeTab.value === 'account') {
      await loadNoAccountList()
      await loadExistingAccounts()
    }
  } catch (e) {
    message.error(e.response?.data?.message || 'Lưu thất bại')
  } finally {
    saving.value = false
  }
}

async function deleteEmployee(id) {
  try {
    await employeeApi.remove(id)
    message.success('Đã xoá nhân viên')
    loadEmployees()
  } catch {
    message.error('Xoá thất bại')
  }
}

// ─── Account creation ────────────────────────────────────────────
function openCreateAccount(rec) {
  accountTarget.value = rec
  Object.assign(accountForm, {
    username: '',
    password: '',
    role: assignableRoles.value[0] || 'Employee',
  })
  accountModalOpen.value = true
}
function resetAccountForm() {
  accountModalOpen.value = false
  accountTarget.value = null
}
async function submitCreateAccount() {
  try {
    await accountFormRef.value.validate()
  } catch {
    return
  }
  if (!accountTarget.value) return
  creatingAccount.value = true
  try {
    await authApi.createAccount({
      employeeId: accountTarget.value.employeeId,
      username: accountForm.username.trim(),
      password: accountForm.password,
      role: accountForm.role,
    })
    message.success(`Đã tạo tài khoản cho ${accountTarget.value.fullName}`)
    accountModalOpen.value = false
    accountTarget.value = null
    await loadNoAccountList()
    await loadExistingAccounts()
    loadEmployees()
  } catch (e) {
    message.error(e.response?.data?.message || 'Tạo tài khoản thất bại')
  } finally {
    creatingAccount.value = false
  }
}

function openChangeRole(rec) {
  if (!canManageAccountRole(rec)) return
  changeRoleTarget.value = rec
  changeRoleForm.role = rec.role
  changeRoleModalOpen.value = true
}

function resetChangeRoleForm() {
  changeRoleModalOpen.value = false
  changeRoleTarget.value = null
  changeRoleForm.role = assignableRoles.value[0] || 'Employee'
}

async function submitChangeRole() {
  if (!changeRoleTarget.value) return
  if (!canManageAccountRole(changeRoleTarget.value)) {
    message.error('Bạn không có quyền đổi role tài khoản này')
    return
  }
  changingRole.value = true
  try {
    await authApi.changeRole({
      employeeId: changeRoleTarget.value.employeeId,
      role: changeRoleForm.role,
      newRole: changeRoleForm.role,
    })
    message.success(`Đã đổi role cho ${changeRoleTarget.value.fullName}`)
    resetChangeRoleForm()
    await loadExistingAccounts()
  } catch (e) {
    message.error(e.response?.data?.message || 'Đổi role thất bại')
  } finally {
    changingRole.value = false
  }
}

// ─── Reset password (chờ backend HR thêm endpoint) ─────────────
const resetPassTarget = ref(null)
const resettingPass = ref(false)
const newPassword = ref('')
const resetPassModalOpen = ref(false)

function openResetPassword(rec) {
  resetPassTarget.value = rec
  newPassword.value = ''
  resetPassModalOpen.value = true
}
async function submitResetPassword() {
  if (!newPassword.value || newPassword.value.length < 6) {
    message.error('Mật khẩu tối thiểu 6 ký tự')
    return
  }
  resettingPass.value = true
  try {
    await authApi.resetPassword({
      employeeId: resetPassTarget.value.employeeId || resetPassTarget.value.id,
      newPassword: newPassword.value,
    })
    message.success(`Đã đặt lại mật khẩu cho ${resetPassTarget.value.fullName}`)
    resetPassModalOpen.value = false
  } catch (e) {
    message.error(e.response?.data?.message || 'Đặt lại mật khẩu thất bại')
  } finally {
    resettingPass.value = false
  }
}

// ─── Init ───────────────────────────────────────────────────────
onMounted(async () => {
  await loadEmployees()
  loadDepartments()
  if (auth.isAdmin || auth.isHR) {
    await loadNoAccountList()
    await loadExistingAccounts()
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
.dot {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  margin-right: 5px;
  vertical-align: middle;
}
.dot.online {
  background: var(--color-primary);
}
.dot.offline {
  background: var(--color-text-muted);
}
</style>
