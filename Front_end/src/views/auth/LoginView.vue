<template>
  <div class="login-page">
    <!-- Left panel -->
    <div class="login-left">
      <router-link to="/" class="back-home">
        <LeftOutlined style="font-size: 14px" />
        <span>Về trang chủ</span>
      </router-link>

      <div class="brand">
        <div class="brand-icon">
          <TeamOutlined style="font-size: 36px; color: #fff" />
        </div>
        <h1 class="brand-name">HRM System</h1>
        <p class="brand-tagline">Hệ thống Quản lý Nhân sự & Chấm công</p>
      </div>

      <div class="feature-list">
        <div v-for="f in features" :key="f.label" class="feature-item">
          <div class="feature-dot"></div>
          <div>
            <div class="feature-title">{{ f.label }}</div>
            <div class="feature-desc">{{ f.desc }}</div>
          </div>
        </div>
      </div>

      <div class="left-footer">
        <span style="font-size: 14px; color: rgba(255, 255, 255, 0.5)"
          >© 2024 HRM System — Đề tài 03</span
        >
      </div>
    </div>

    <!-- Right panel: form -->
    <div class="login-right">
      <div class="login-form-wrap">
        <div class="form-header">
          <h2 class="form-title">Đăng nhập</h2>
          <p class="form-subtitle">Nhập thông tin tài khoản để tiếp tục vào hệ thống</p>
        </div>

        <a-form :model="form" layout="vertical" @finish="onLogin" autocomplete="off" size="large">
          <a-form-item
            name="username"
            label="Tên đăng nhập"
            :rules="[{ required: true, message: 'Vui lòng nhập tên đăng nhập' }]"
          >
            <a-input
              v-model:value="form.username"
              placeholder="Nhập tên đăng nhập"
              :prefix="h(UserOutlined)"
            />
          </a-form-item>

          <a-form-item
            name="password"
            label="Mật khẩu"
            :rules="[{ required: true, message: 'Vui lòng nhập mật khẩu' }]"
          >
            <a-input-password
              v-model:value="form.password"
              placeholder="Nhập mật khẩu"
              :prefix="h(LockOutlined)"
            />
          </a-form-item>

          <a-form-item style="margin-top: 12px">
            <a-button
              type="primary"
              html-type="submit"
              :loading="loading"
              block
              style="height: 52px; font-size: 16px; font-weight: 600; border-radius: 12px"
            >
              {{ loading ? 'Đang đăng nhập...' : 'Đăng nhập' }}
            </a-button>
          </a-form-item>
        </a-form>

        <a-alert
          v-if="errorMsg"
          :message="errorMsg"
          type="error"
          show-icon
          closable
          @close="errorMsg = ''"
          style="margin-top: 8px; border-radius: 10px; font-size: 14px"
        />

        <div class="login-hint">
          <InfoCircleOutlined
            style="color: var(--color-text-muted); margin-right: 8px; font-size: 16px"
          />
          <span style="font-size: 14px"
            >HR Service: <code style="font-size: 13px">https://localhost:7084</code></span
          >
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { h, ref, reactive } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { message } from 'ant-design-vue'
import {
  UserOutlined,
  LockOutlined,
  TeamOutlined,
  InfoCircleOutlined,
  ScheduleOutlined,
  DollarOutlined,
  CheckCircleOutlined,
  LeftOutlined,
} from '@ant-design/icons-vue'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const loading = ref(false)
const errorMsg = ref('')
const form = reactive({ username: '', password: '' })

const features = [
  { label: 'Quản lý Nhân viên & Phòng ban', desc: 'Hồ sơ nhân viên, cơ cấu tổ chức dạng cây' },
  { label: 'Chấm công & Nghỉ phép', desc: 'Check-in/out, lịch sử, duyệt nghỉ phép' },
  { label: 'Tính lương & Báo cáo', desc: 'Tự động tính lương, xuất báo cáo tháng' },
]

async function onLogin() {
  loading.value = true
  errorMsg.value = ''
  try {
    await auth.login(form.username, form.password)
    message.success('Đăng nhập thành công!')
    const redirect = route.query.redirect || '/app/dashboard'
    router.push(redirect)
  } catch (err) {
    const msg =
      err.response?.data?.message ||
      err.response?.data?.title ||
      err.response?.data ||
      'Sai tên đăng nhập hoặc mật khẩu'
    errorMsg.value = typeof msg === 'string' ? msg : 'Đăng nhập thất bại'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  display: flex;
  min-height: 100vh;
  font-family: var(--font-body);
}

/* ── Left ── */
.login-left {
  width: 420px;
  flex-shrink: 0;
  background: linear-gradient(160deg, #0d1b2a 0%, #1a2f1e 60%, #0d2a18 100%);
  padding: 48px 40px;
  display: flex;
  flex-direction: column;
  position: relative;
  overflow: hidden;
}
.login-left::before {
  content: '';
  position: absolute;
  top: -100px;
  right: -100px;
  width: 300px;
  height: 300px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(0, 177, 79, 0.18) 0%, transparent 70%);
}
.login-left::after {
  content: '';
  position: absolute;
  bottom: -80px;
  left: -80px;
  width: 250px;
  height: 250px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(0, 180, 216, 0.12) 0%, transparent 70%);
}

.brand {
  margin-bottom: 64px;
}
.back-home {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  color: rgba(255, 255, 255, 0.75);
  text-decoration: none;
  font-size: 13px;
  font-weight: 600;
  padding: 8px 12px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.1);
  width: fit-content;
  margin-bottom: 20px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  transition:
    transform 0.2s,
    background 0.2s,
    color 0.2s;
}
.back-home:hover {
  color: #fff;
  background: rgba(255, 255, 255, 0.2);
  transform: translateX(-2px);
}
.brand-icon {
  width: 64px;
  height: 64px;
  border-radius: 20px;
  background: linear-gradient(135deg, var(--color-primary), #00d46a);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 22px;
  box-shadow: 0 10px 32px rgba(0, 177, 79, 0.4);
}
.brand-icon :deep(.anticon) {
  font-size: 32px;
  color: #fff;
}
.brand-name {
  font-family: var(--font-display);
  font-size: 30px;
  font-weight: 700;
  color: #fff;
  margin: 0 0 10px;
}
.brand-tagline {
  font-size: 16px;
  color: rgba(255, 255, 255, 0.65);
  margin: 0;
  line-height: 1.6;
}

.feature-list {
  display: flex;
  flex-direction: column;
  gap: 24px;
  flex: 1;
}
.feature-item {
  display: flex;
  gap: 16px;
  align-items: flex-start;
}
.feature-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: var(--color-primary);
  margin-top: 7px;
  flex-shrink: 0;
  box-shadow: 0 0 10px rgba(0, 177, 79, 0.6);
}
.feature-title {
  font-size: 16px;
  font-weight: 600;
  color: #fff;
  margin-bottom: 4px;
}
.feature-desc {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.55);
  line-height: 1.6;
}

.left-footer {
  margin-top: auto;
  padding-top: 32px;
}

/* ── Right ── */
.login-right {
  flex: 1;
  background: linear-gradient(160deg, #f0faf5 0%, #e6f7f5 40%, #eef6fb 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 56px 40px;
}
.login-form-wrap {
  width: 100%;
  max-width: 440px;
  background: #fff;
  border-radius: var(--radius-xl);
  border: 1px solid var(--color-border);
  box-shadow: var(--shadow-card);
  padding: 44px;
}
.form-header {
  margin-bottom: 32px;
}
.form-title {
  font-family: var(--font-display);
  font-size: 26px;
  font-weight: 700;
  color: var(--color-text);
  margin: 0 0 10px;
}
.form-subtitle {
  font-size: 16px;
  color: var(--color-text-muted);
  margin: 0;
  line-height: 1.6;
}

.login-hint {
  margin-top: 24px;
  padding: 14px 16px;
  background: var(--color-surface);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  font-size: 14px;
  color: var(--color-text-sec);
  display: flex;
  align-items: center;
}
.login-hint code {
  font-size: 13px;
  background: var(--color-border);
  padding: 3px 8px;
  border-radius: 6px;
  margin-left: 4px;
}

@media (max-width: 768px) {
  .login-left {
    display: none;
  }
  .login-form-wrap {
    box-shadow: none;
    border: none;
    padding: 24px 16px;
  }
}
</style>
