import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('hrm_token') || '')
  const user = ref(JSON.parse(localStorage.getItem('hrm_user') || 'null'))

  const isLoggedIn = computed(() => !!token.value)
  const userRole = computed(() => user.value?.role || '')
  const userName = computed(() => user.value?.fullName || user.value?.username || 'User')
  const userId = computed(() => user.value?.id || user.value?.employeeId || null)
  const departmentId = computed(() => user.value?.departmentId || null)

  // Role shortcuts
  const isAdmin = computed(() => userRole.value === 'Admin')
  const isHR = computed(() => userRole.value === 'HR')
  const isManager = computed(() => userRole.value === 'Manager')
  const isEmployee = computed(() => userRole.value === 'Employee')
  const isAccountant = computed(() => userRole.value === 'KT')

  async function login(username, password) {
    const res = await authApi.login({ username, password })
    const data = res.data

    // Support various response shapes
    const t = data.token || data.accessToken || data.data?.token || data.data?.accessToken
    const u = data.user || data.data?.user || { username, role: data.role || data.data?.role }

    token.value = t
    user.value = u
    localStorage.setItem('hrm_token', t)
    localStorage.setItem('hrm_user', JSON.stringify(u))
    return { token: t, user: u }
  }

  function logout() {
    token.value = ''
    user.value = null
    localStorage.removeItem('hrm_token')
    localStorage.removeItem('hrm_user')
  }

  return {
    token,
    user,
    isLoggedIn,
    userRole,
    userName,
    userId,
    departmentId,
    isAdmin,
    isHR,
    isManager,
    isEmployee,
    isAccountant,
    login,
    logout,
  }
})
