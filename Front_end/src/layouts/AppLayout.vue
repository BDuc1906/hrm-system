<template>
  <a-layout style="min-height: 100vh">
    <!-- ─── Sidebar ─────────────────────────────────────────── -->
    <a-layout-sider
      v-model:collapsed="collapsed"
      :trigger="null"
      collapsible
      :width="248"
      :collapsed-width="72"
      style="
        background: #fff;
        border-right: 1px solid var(--color-border);
        position: fixed;
        height: 100vh;
        left: 0;
        top: 0;
        z-index: 100;
        overflow-y: auto;
      "
    >
      <!-- Logo -->
      <div class="sidebar-logo" :class="{ collapsed }">
        <div class="logo-icon">
          <TeamOutlined style="color: #fff; font-size: 18px" />
        </div>
        <transition name="fade">
          <div v-if="!collapsed" class="logo-text">
            <span class="logo-title">HRM</span>
            <span class="logo-sub">System</span>
          </div>
        </transition>
      </div>

      <!-- Navigation -->
      <a-menu
        v-model:selectedKeys="selectedKeys"
        v-model:openKeys="openKeys"
        mode="inline"
        :inline-collapsed="collapsed"
        @select="onMenuSelect"
        style="border: none; padding: 8px 12px"
      >
        <a-menu-item key="/app/dashboard">
          <template #icon><DashboardOutlined /></template>
          <span>Tổng quan</span>
        </a-menu-item>

        <a-menu-item key="/app/hr/my-contracts">
          <template #icon><AuditOutlined /></template>
          <span>Hợp đồng của tôi</span>
        </a-menu-item>

        <!-- Nhân sự — Admin, HR -->
        <a-menu-item-group v-if="auth.isAdmin || auth.isHR">
          <template #title>
            <span
              v-if="!collapsed"
              style="
                font-size: 10px;
                font-weight: 700;
                color: var(--color-text-muted);
                text-transform: uppercase;
                letter-spacing: 0.08em;
                padding: 0 8px;
              "
              >Nhân sự</span
            >
          </template>
          <a-menu-item key="/app/hr/employees">
            <template #icon><UserOutlined /></template>
            <span>Nhân viên</span>
          </a-menu-item>
          <a-menu-item key="/app/hr/departments">
            <template #icon><ApartmentOutlined /></template>
            <span>Phòng ban</span>
          </a-menu-item>
        </a-menu-item-group>

        <!-- Chấm công — tất cả -->
        <a-menu-item-group>
          <template #title>
            <span
              v-if="!collapsed"
              style="
                font-size: 10px;
                font-weight: 700;
                color: var(--color-text-muted);
                text-transform: uppercase;
                letter-spacing: 0.08em;
                padding: 0 8px;
              "
              >Chấm công</span
            >
          </template>
          <a-menu-item key="/app/attendance/check">
            <template #icon><ClockCircleOutlined /></template>
            <span>Chấm công</span>
          </a-menu-item>
          <a-menu-item key="/app/attendance/history">
            <template #icon><CalendarOutlined /></template>
            <span>Lịch sử</span>
          </a-menu-item>
          <a-menu-item key="/app/attendance/leave">
            <template #icon><FileTextOutlined /></template>
            <span>Nghỉ phép</span>
          </a-menu-item>
        </a-menu-item-group>

        <!--
          FIX: trước đây nhóm cha chỉ v-if="isAdmin || isHR || isEmployee" nên
          với Manager, CẢ NHÓM (kể cả mục "Báo cáo" vốn được phép hiện với
          Manager) bị ẩn hoàn toàn dù route + v-if của chính mục đó đã cho phép.
          → Manager không bao giờ thấy được nút "Báo cáo" dù có quyền.
          Giờ nhóm cha hiện nếu có ÍT NHẤT 1 mục con hợp lệ với role hiện tại,
          từng mục con tự quyết định việc hiển thị của chính nó.
        -->
        <a-menu-item-group v-if="auth.isAdmin || auth.isHR || auth.isEmployee || auth.isManager || auth.isAccountant">
          <template #title>
            <span
              v-if="!collapsed"
              style="
                font-size: 10px;
                font-weight: 700;
                color: var(--color-text-muted);
                text-transform: uppercase;
                letter-spacing: 0.08em;
                padding: 0 8px;
              "
              >Lương</span
            >
          </template>
          <!-- Bảng lương — Admin, HR, Employee (không có Manager) -->
          <a-menu-item v-if="auth.isAdmin || auth.isHR || auth.isEmployee || auth.isAccountant" key="/app/payroll/list">
            <template #icon><DollarOutlined /></template>
            <span>Bảng lương</span>
          </a-menu-item>
          <!-- Báo cáo — Admin, HR, Manager (không Employee) -->
          <a-menu-item v-if="auth.isAdmin || auth.isHR || auth.isManager || auth.isAccountant" key="/app/payroll/report">
            <template #icon><BarChartOutlined /></template>
            <span>Báo cáo</span>
          </a-menu-item>
        </a-menu-item-group>
      </a-menu>

      <!-- Bottom: collapse toggle -->
      <div class="sidebar-footer">
        <a-button
          type="text"
          @click="collapsed = !collapsed"
          style="
            width: 100%;
            justify-content: center;
            display: flex;
            align-items: center;
            color: var(--color-text-muted);
          "
        >
          <MenuFoldOutlined v-if="!collapsed" />
          <MenuUnfoldOutlined v-else />
        </a-button>
      </div>
    </a-layout-sider>

    <!-- ─── Main ───────────────────────────────────────────── -->
    <a-layout
      :style="{ marginLeft: collapsed ? '72px' : '248px', transition: 'margin-left 0.25s' }"
    >
      <!-- Header -->
      <a-layout-header class="app-header">
        <a-breadcrumb :routes="breadcrumbs">
          <template #itemRender="{ route }">
            <router-link v-if="route.path" :to="route.path">{{ route.breadcrumbName }}</router-link>
            <span v-else>{{ route.breadcrumbName }}</span>
          </template>
        </a-breadcrumb>

        <div class="header-right">
          <!-- User menu -->
          <a-dropdown placement="bottomRight" :arrow="{ pointAtCenter: true }">
            <div class="user-chip">
              <a-avatar
                :size="21"
                style="
                  background: var(--color-primary);
                  font-family: var(--font-display);
                  font-weight: 700;
                  font-size: 14px;
                  flex-shrink: 0;
                  line-height: 20px;
                "
              >
                {{ avatarLetter }}
              </a-avatar>
              <span class="user-name">{{ auth.userName }}</span>
              <DownOutlined style="font-size: 11px; color: var(--color-text-muted)" />
            </div>
            <template #overlay>
              <a-menu>
                <a-menu-item key="role" disabled>
                  <a-tag :color="roleColor" style="font-size: 11px">{{ roleLabel }}</a-tag>
                </a-menu-item>
                <a-menu-divider />
                <a-menu-item key="logout" @click="handleLogout">
                  <LogoutOutlined /> Đăng xuất
                </a-menu-item>
              </a-menu>
            </template>
          </a-dropdown>
        </div>
      </a-layout-header>

      <!-- Content -->
      <a-layout-content class="app-content">
        <router-view v-slot="{ Component }">
          <transition name="page" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </a-layout-content>
    </a-layout>
  </a-layout>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import {
  DashboardOutlined,
  UserOutlined,
  ApartmentOutlined,
  ClockCircleOutlined,
  CalendarOutlined,
  FileTextOutlined,
  DollarOutlined,
  BarChartOutlined,
  TeamOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  DownOutlined,
  LogoutOutlined,
  AuditOutlined,
} from '@ant-design/icons-vue'
import { message } from 'ant-design-vue'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const collapsed = ref(false)

const selectedKeys = ref([route.path])
const openKeys = ref([])

watch(
  () => route.path,
  (path) => {
    selectedKeys.value = [path]
  },
)

function onMenuSelect({ key }) {
  router.push(key)
}

const avatarLetter = computed(() => (auth.userName || 'U')[0].toUpperCase())

const roleColor = computed(
  () =>
    ({
      Admin: 'red',
      HR: 'blue',
      Manager: 'orange',
      Employee: 'green',
    })[auth.userRole] || 'default',
)

const roleLabel = computed(
  () =>
    ({
      Admin: 'Quản trị viên',
      HR: 'Nhân sự',
      Manager: 'Quản lý',
      Employee: 'Nhân viên',
    })[auth.userRole] ||
    auth.userRole ||
    '—',
)

const routeMap = {
  '/app/dashboard': 'Tổng quan',
  '/app/hr/employees': 'Nhân viên',
  '/app/hr/departments': 'Phòng ban',
  '/app/attendance/check': 'Chấm công',
  '/app/attendance/history': 'Lịch sử chấm công',
  '/app/attendance/leave': 'Nghỉ phép',
  '/app/payroll/list': 'Bảng lương',
  '/app/payroll/report': 'Báo cáo lương',
}
const breadcrumbs = computed(() => [
  { breadcrumbName: 'HRM', path: '/dashboard' },
  { breadcrumbName: routeMap[route.path] || route.path },
])

async function handleLogout() {
  auth.logout()
  message.success('Đã đăng xuất')
  router.push('/login')
}
</script>

<style scoped>
.sidebar-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 20px 20px 16px;
  border-bottom: 1px solid var(--color-border);
  margin-bottom: 8px;
  transition: padding var(--transition);
}
.sidebar-logo.collapsed {
  padding: 20px 14px 16px;
  justify-content: center;
}
.logo-icon {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: linear-gradient(135deg, var(--color-primary), #00d46a);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  box-shadow: 0 4px 12px rgba(0, 177, 79, 0.3);
}
.logo-text {
  display: flex;
  flex-direction: column;
  line-height: 1.2;
}
.logo-title {
  font-family: var(--font-display);
  font-size: 17px;
  font-weight: 700;
  color: var(--color-text);
}
.logo-sub {
  font-size: 11px;
  color: var(--color-text-muted);
  font-weight: 500;
}

.sidebar-footer {
  position: sticky;
  bottom: 0;
  padding: 12px;
  border-top: 1px solid var(--color-border);
  background: #fff;
}

.app-header {
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid var(--color-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 32px;
  height: 60px;
  position: sticky;
  top: 0;
  z-index: 50;
  box-shadow: 0 1px 8px rgba(13, 27, 42, 0.06);
}
.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}
.user-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px 4px 6px;
  border-radius: 10px;
  border: 1px solid var(--color-border);
  cursor: pointer;
  transition: all var(--transition);
  background: var(--color-surface);
  line-height: 1;
  height: 38px;
  box-sizing: border-box;
}
.user-chip:hover {
  border-color: var(--color-primary);
  background: var(--color-primary-light);
}
.user-name {
  font-size: 14px;
  font-weight: 500;
  color: var(--color-text);
}

.app-content {
  padding: 28px 32px;
  min-height: calc(100vh - 60px);
}

/* Transitions */
.page-enter-active,
.page-leave-active {
  transition:
    opacity 0.18s,
    transform 0.18s;
}
.page-enter-from {
  opacity: 0;
  transform: translateY(8px);
}
.page-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* Menu group title override */
:deep(.ant-menu-item-group-title) {
  padding: 12px 8px 4px !important;
}
:deep(.ant-menu-item) {
  border-radius: var(--radius-md) !important;
  margin-bottom: 2px !important;
  height: 42px !important;
  line-height: 42px !important;
  font-size: 14px;
}
</style>