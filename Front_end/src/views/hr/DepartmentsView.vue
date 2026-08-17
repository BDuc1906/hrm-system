<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">Phòng ban</h1>
        <p style="margin: 4px 0 0; font-size: 14px; color: var(--color-text-muted)">
          Cơ cấu tổ chức dạng cây
        </p>
      </div>
      <div style="display: flex; gap: 10px">
        <a-button @click="loadTree"><ReloadOutlined /></a-button>
        <a-button v-if="auth.isAdmin || auth.isHR" type="primary" @click="openCreate"
          ><PlusOutlined /> Thêm phòng ban</a-button
        >
      </div>
    </div>

    <a-row :gutter="[20, 20]">
      <!-- Tree panel -->
      <a-col :xs="24" :lg="10">
        <a-card title="Sơ đồ tổ chức" :bordered="false">
          <a-spin :spinning="loading">
            <a-tree
              v-if="treeData.length"
              :tree-data="treeData"
              :field-names="{ title: 'name', key: 'id', children: 'children' }"
              default-expand-all
              show-icon
            >
              <template #icon><ApartmentOutlined style="color: var(--color-primary)" /></template>
              <template #title="node">
                <div class="tree-node" @click="selectNode(node)">
                  <span :class="{ active: selectedNode?.id === node.id }">{{ node.name }}</span>
                  <span v-if="node.employeeCount" class="node-count">{{ node.employeeCount }}</span>
                </div>
              </template>
            </a-tree>
            <a-empty v-else description="Chưa có phòng ban" />
          </a-spin>
        </a-card>
      </a-col>

      <!-- Detail panel -->
      <a-col :xs="24" :lg="14">
        <a-card :bordered="false">
          <template #title>
            <span v-if="selectedNode">Chi tiết: {{ selectedNode.name }}</span>
            <span v-else>Danh sách phòng ban</span>
          </template>
          <template #extra>
            <a-space v-if="selectedNode">
              <a-button
                v-if="auth.isAdmin || auth.isHR"
                size="small"
                @click="openEdit(selectedNode)"
                ><EditOutlined /> Sửa</a-button
              >
              <a-popconfirm
                v-if="auth.isAdmin"
                title="Xác nhận xoá phòng ban?"
                ok-text="Xoá"
                cancel-text="Huỷ"
                ok-type="danger"
                @confirm="deleteDept(selectedNode.id)"
              >
                <a-button size="small" danger><DeleteOutlined /> Xoá</a-button>
              </a-popconfirm>
            </a-space>
          </template>

          <div v-if="selectedNode" class="dept-detail">
            <a-descriptions :column="2" bordered size="small">
              <a-descriptions-item label="Tên phòng ban">{{
                selectedNode.name
              }}</a-descriptions-item>
              <a-descriptions-item label="Mã phòng ban">{{
                selectedNode.code || selectedNode.id
              }}</a-descriptions-item>
              <a-descriptions-item label="Trưởng phòng">{{
                selectedNode.managerName || '—'
              }}</a-descriptions-item>
              <a-descriptions-item label="Số nhân viên">{{
                selectedNode.employeeCount || '—'
              }}</a-descriptions-item>
              <a-descriptions-item label="Mô tả" :span="2">{{
                selectedNode.description || '—'
              }}</a-descriptions-item>
            </a-descriptions>
          </div>

          <a-table
            v-else
            :data-source="flatList"
            :columns="deptColumns"
            :loading="loading"
            row-key="id"
            size="small"
            :pagination="{ pageSize: 8 }"
          >
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'actions'">
                <a-space>
                  <a-button
                    v-if="auth.isAdmin || auth.isHR"
                    type="text"
                    size="small"
                    @click="openEdit(record)"
                    ><EditOutlined style="color: var(--color-accent-blue)"
                  /></a-button>
                  <a-popconfirm
                    v-if="auth.isAdmin"
                    title="Xoá phòng ban?"
                    ok-type="danger"
                    ok-text="Xoá"
                    cancel-text="Huỷ"
                    @confirm="deleteDept(record.id)"
                  >
                    <a-button type="text" size="small" danger><DeleteOutlined /></a-button>
                  </a-popconfirm>
                </a-space>
              </template>
            </template>
          </a-table>
        </a-card>
      </a-col>
    </a-row>

    <!-- Modal -->
    <a-modal
      v-model:open="modalOpen"
      :title="editingId ? 'Sửa phòng ban' : 'Thêm phòng ban'"
      :confirm-loading="saving"
      ok-text="Lưu"
      cancel-text="Huỷ"
      @ok="saveDept"
    >
      <a-form :model="form" layout="vertical" :rules="rules" ref="formRef">
        <a-form-item name="name" label="Tên phòng ban">
          <a-input v-model:value="form.name" placeholder="Phòng Kỹ thuật" />
        </a-form-item>
        <a-form-item name="code" label="Mã phòng ban">
          <a-input v-model:value="form.code" placeholder="IT" />
        </a-form-item>
        <a-form-item name="parentId" label="Phòng ban cha">
          <a-select
            v-model:value="form.parentId"
            allow-clear
            placeholder="Không có (cấp cao nhất)"
            style="width: 100%"
          >
            <a-select-option v-for="d in flatList" :key="d.id" :value="d.id">{{
              d.name
            }}</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item name="description" label="Mô tả">
          <a-textarea v-model:value="form.description" :rows="3" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { departmentApi } from '@/api'
import { message } from 'ant-design-vue'
import {
  ApartmentOutlined,
  PlusOutlined,
  ReloadOutlined,
  EditOutlined,
  DeleteOutlined,
} from '@ant-design/icons-vue'

const auth = useAuthStore()

const loading = ref(false)
const saving = ref(false)
const treeData = ref([])
const flatList = ref([])
const selectedNode = ref(null)
const modalOpen = ref(false)
const editingId = ref(null)
const formRef = ref()
const form = reactive({ name: '', code: '', parentId: null, description: '' })
const rules = { name: [{ required: true, message: 'Tên không được trống' }] }

const deptColumns = [
  { title: 'Tên phòng ban', dataIndex: 'name', key: 'name' },
  { title: 'Mã', dataIndex: 'code', key: 'code', width: 80 },
  { title: 'Trưởng phòng', dataIndex: 'managerName', key: 'manager', ellipsis: true },
  { title: 'NV', dataIndex: 'employeeCount', key: 'count', width: 60, align: 'center' },
  { title: '', key: 'actions', width: 80, align: 'center' },
]

function flatten(nodes, result = []) {
  for (const n of nodes) {
    result.push(n)
    if (n.children?.length) flatten(n.children, result)
  }
  return result
}
function selectNode(node) {
  selectedNode.value = selectedNode.value?.id === node.id ? null : node
}

async function loadTree() {
  loading.value = true
  try {
    const res = await departmentApi.getTree()
    treeData.value = Array.isArray(res.data) ? res.data : res.data.data || []
    flatList.value = flatten(treeData.value)
  } catch {
    message.error('Không tải được danh sách phòng ban')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, { name: '', code: '', parentId: null, description: '' })
  modalOpen.value = true
}
function openEdit(rec) {
  editingId.value = rec.id
  Object.assign(form, {
    name: rec.name || '',
    code: rec.code || '',
    parentId: rec.parentId || null,
    description: rec.description || '',
  })
  modalOpen.value = true
}

async function saveDept() {
  try {
    await formRef.value.validate()
  } catch {
    return
  }
  saving.value = true
  try {
    if (editingId.value) {
      await departmentApi.update(editingId.value, form)
      message.success('Đã cập nhật phòng ban')
    } else {
      await departmentApi.create(form)
      message.success('Đã thêm phòng ban')
    }
    modalOpen.value = false
    loadTree()
  } catch (e) {
    message.error(e.response?.data?.message || 'Lưu thất bại')
  } finally {
    saving.value = false
  }
}

async function deleteDept(id) {
  try {
    await departmentApi.remove(id)
    message.success('Đã xoá')
    selectedNode.value = null
    loadTree()
  } catch {
    message.error('Xoá thất bại — có thể còn nhân viên')
  }
}

onMounted(loadTree)
</script>

<style scoped>
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 24px;
}
.tree-node {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 2px 0;
}
.tree-node span.active {
  color: var(--color-primary);
  font-weight: 600;
}
.node-count {
  font-size: 11px;
  background: var(--color-primary-light);
  color: var(--color-primary);
  border-radius: 9999px;
  padding: 1px 7px;
  font-weight: 600;
}
.dept-detail {
  padding: 4px 0;
}
</style>
