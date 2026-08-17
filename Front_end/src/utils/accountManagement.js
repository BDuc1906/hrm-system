const ROLE_LABELS = {
  Admin: 'Quan tri vien',
  HR: 'Nhan su',
  Manager: 'Quan ly',
  Employee: 'Nhan vien',
}

const ACCOUNT_STATUS_LABELS = {
  active: 'Hoat dong',
  inactive: 'Khoa',
  locked: 'Khoa',
  disabled: 'Vo hieu hoa',
}

function pickFirst(...values) {
  return values.find((value) => value !== undefined && value !== null && value !== '')
}

function normalizeStatus(rawStatus, employeeStatus) {
  if (employeeStatus === 'Inactive') {
    return { value: 'locked', label: 'Khoa theo trang thai nhan vien' }
  }

  const raw = String(rawStatus || '')
    .trim()
    .toLowerCase()
  if (!raw) return { value: 'active', label: ACCOUNT_STATUS_LABELS.active }

  if (['inactive', 'locked', 'disabled', 'disable', 'blocked'].includes(raw)) {
    return {
      value: raw === 'disabled' || raw === 'disable' ? 'disabled' : 'locked',
      label: ACCOUNT_STATUS_LABELS[raw] || 'Khoa',
    }
  }

  return { value: 'active', label: ACCOUNT_STATUS_LABELS.active }
}

export function getAssignableRoles(currentUserRole) {
  return currentUserRole === 'Admin'
    ? ['Employee', 'Manager', 'HR', 'Admin']
    : ['Employee', 'Manager', 'HR']
}

export function getRoleLabel(role) {
  return ROLE_LABELS[role] || role || 'Chua ro'
}

export function normalizeAccountRecords(rawAccounts = [], employees = []) {
  const employeeMap = new Map(
    employees.map((employee) => [
      String(pickFirst(employee.id, employee.employeeId, employee.userId)),
      employee,
    ]),
  )

  return rawAccounts.map((raw) => {
    const employeeId = pickFirst(
      raw.employeeId,
      raw.id,
      raw.userId,
      raw.employee?.id,
      raw.employee?.employeeId,
    )
    const employee = employeeMap.get(String(employeeId)) || {}
    const employeeStatus = pickFirst(
      raw.employeeStatus,
      raw.statusEmployee,
      employee.status,
      'Active',
    )
    const accountStatus = normalizeStatus(
      pickFirst(
        raw.accountStatus,
        raw.status,
        raw.userStatus,
        raw.isLocked === true ? 'Locked' : undefined,
      ),
      employeeStatus,
    )

    return {
      accountId: pickFirst(raw.accountId, raw.userId, raw.id, employeeId),
      employeeId,
      employeeCode: pickFirst(
        raw.employeeCode,
        raw.code,
        raw.employee?.employeeCode,
        employee.employeeCode,
        employee.code,
        '—',
      ),
      fullName: pickFirst(
        raw.fullName,
        raw.employeeName,
        raw.employee?.fullName,
        employee.fullName,
        raw.username,
        'Chua co ten',
      ),
      departmentName: pickFirst(
        raw.departmentName,
        raw.employee?.departmentName,
        employee.departmentName,
        '—',
      ),
      email: pickFirst(raw.email, raw.employee?.email, employee.email, ''),
      username: pickFirst(raw.username, raw.userName, raw.loginName, '—'),
      role: pickFirst(raw.role, raw.userRole, 'Employee'),
      employeeStatus,
      accountStatus: accountStatus.value,
      accountStatusLabel: accountStatus.label,
      canResetPassword: Boolean(employeeId),
      raw,
    }
  })
}
