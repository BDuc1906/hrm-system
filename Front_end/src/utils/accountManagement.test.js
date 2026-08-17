import test from 'node:test'
import assert from 'node:assert/strict'

import { getAssignableRoles, getRoleLabel, normalizeAccountRecords } from './accountManagement.js'

test('HR khong duoc gan role Admin', () => {
  assert.deepEqual(getAssignableRoles('HR'), ['Employee', 'Manager', 'HR'])
})

test('Admin duoc gan day du role', () => {
  assert.deepEqual(getAssignableRoles('Admin'), ['Employee', 'Manager', 'HR', 'Admin'])
})

test('normalizeAccountRecords ghep account voi employee va khoa theo Inactive', () => {
  const rows = normalizeAccountRecords(
    [{ employeeId: 7, username: 'an.nguyen', role: 'Manager', status: 'Active' }],
    [
      {
        id: 7,
        employeeCode: 'E007',
        fullName: 'Nguyen Van An',
        departmentName: 'IT',
        status: 'Inactive',
      },
    ],
  )

  assert.equal(rows[0].employeeCode, 'E007')
  assert.equal(rows[0].fullName, 'Nguyen Van An')
  assert.equal(rows[0].departmentName, 'IT')
  assert.equal(rows[0].accountStatus, 'locked')
  assert.equal(rows[0].accountStatusLabel, 'Khoa theo trang thai nhan vien')
})

test('normalizeAccountRecords chap nhan field userName va role label fallback', () => {
  const rows = normalizeAccountRecords([
    { id: 9, userName: 'hr.user', userRole: 'HR', accountStatus: 'Disabled' },
  ])

  assert.equal(rows[0].username, 'hr.user')
  assert.equal(rows[0].role, 'HR')
  assert.equal(rows[0].accountStatus, 'disabled')
  assert.equal(getRoleLabel(rows[0].role), 'Nhan su')
})
