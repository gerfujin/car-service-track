<template>
  <div class="row justify-content-center">
    <div class="col-md-6">
      <div class="card shadow-sm">
        <div class="card-header bg-primary text-white">
          <h5 class="mb-0">🔄 Update Order Status</h5>
        </div>
        <div class="card-body">
          <div v-if="loading" class="text-center py-3"><div class="spinner-border"></div></div>
          <div v-else-if="error" class="alert alert-danger">{{ error }}</div>
          <div v-else-if="order">
            <dl class="row mb-3">
              <dt class="col-sm-4">Vehicle</dt>
              <dd class="col-sm-8">{{ order.vehicleDisplay }}</dd>
              <dt class="col-sm-4">Workshop</dt>
              <dd class="col-sm-8">{{ order.workshopName }}</dd>
              <dt class="col-sm-4">Current Status</dt>
              <dd class="col-sm-8">
                <span :class="statusBadge(order.status)" class="badge">{{ order.status }}</span>
              </dd>
            </dl>

            <div class="mb-3">
              <label class="form-label fw-bold">New Status</label>
              <select v-model="newStatus" class="form-select">
                <option value="Pending">Pending</option>
                <option value="InProgress">In Progress</option>
                <option value="Completed">Completed</option>
                <option value="Cancelled">Cancelled</option>
              </select>
            </div>

            <div class="mb-3">
              <label class="form-label">Notes (optional)</label>
              <textarea v-model="notes" class="form-control" rows="3"
                        placeholder="Describe what was done or why the status changed..."></textarea>
            </div>

            <div v-if="submitError" class="alert alert-danger">{{ submitError }}</div>

            <div class="d-flex gap-2">
              <button @click="handleSubmit" :disabled="submitting" class="btn btn-primary">
                <span v-if="submitting" class="spinner-border spinner-border-sm me-2"></span>
                ✅ Update Status
              </button>
              <router-link to="/orders" class="btn btn-outline-secondary">Cancel</router-link>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { orderService } from '@/services/orderService'
import type { ServiceOrderDto } from '@/types'

const route = useRoute()
const router = useRouter()

const order = ref<ServiceOrderDto | null>(null)
const loading = ref(true)
const error = ref('')
const submitting = ref(false)
const submitError = ref('')
const newStatus = ref('InProgress')
const notes = ref('')

onMounted(async () => {
  try {
    order.value = await orderService.getById(route.params.id as string)
    newStatus.value = order.value.status
  } catch {
    error.value = 'Failed to load order.'
  } finally {
    loading.value = false
  }
})

async function handleSubmit() {
  submitError.value = ''
  submitting.value = true
  try {
    await orderService.updateStatus(route.params.id as string, newStatus.value, notes.value || undefined)
    router.push('/orders')
  } catch {
    submitError.value = 'Failed to update status. Please try again.'
  } finally {
    submitting.value = false
  }
}

function statusBadge(status: string): string {
  const map: Record<string, string> = {
    'Pending': 'bg-secondary',
    'InProgress': 'bg-primary',
    'Completed': 'bg-success',
    'Cancelled': 'bg-danger'
  }
  return map[status] || 'bg-secondary'
}
</script>
