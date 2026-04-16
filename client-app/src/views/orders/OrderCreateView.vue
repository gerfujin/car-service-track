<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>📋 {{ t('orders.create') }}</h2>
      <router-link to="/orders" class="btn btn-outline-secondary">← {{ t('common.back') }}</router-link>
    </div>

    <div v-if="loadingData" class="text-center py-5"><div class="spinner-border"></div></div>
    <div v-else class="card shadow-sm" style="max-width: 600px;">
      <div class="card-body">
        <div v-if="error" class="alert alert-danger">{{ error }}</div>

        <div v-if="vehicles.length === 0" class="alert alert-warning">
          You need to add a vehicle first.
          <router-link to="/vehicles/create" class="alert-link">Add Vehicle</router-link>
        </div>

        <form v-else @submit.prevent="handleCreate">
          <div class="mb-3">
            <label class="form-label">{{ t('orders.vehicle') }} *</label>
            <select v-model="form.vehicleId" class="form-select" required>
              <option value="">Select a vehicle...</option>
              <option v-for="v in vehicles" :key="v.id" :value="v.id">
                {{ v.make }} {{ v.model }} ({{ v.licensePlate }})
              </option>
            </select>
          </div>
          <div class="mb-3">
            <label class="form-label">{{ t('orders.workshop') }} *</label>
            <select v-model="form.workshopId" class="form-select" required>
              <option value="">Select a workshop...</option>
              <option v-for="w in workshops" :key="w.id" :value="w.id">
                {{ w.name }}
              </option>
            </select>
          </div>
          <div class="mb-3">
            <label class="form-label">Description</label>
            <textarea v-model="form.description" class="form-control" rows="3" placeholder="Describe the issue or service needed..."></textarea>
          </div>
          <div class="d-flex gap-2">
            <button type="submit" class="btn btn-primary" :disabled="loading">
              <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
              {{ t('common.save') }}
            </button>
            <router-link to="/orders" class="btn btn-outline-secondary">{{ t('common.cancel') }}</router-link>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { orderService } from '@/services/orderService'
import { vehicleService } from '@/services/vehicleService'
import { workshopService } from '@/services/workshopService'
import type { VehicleDto, WorkshopDto, ServiceOrderCreateDto } from '@/types'

const { t } = useI18n()
const router = useRouter()

const vehicles = ref<VehicleDto[]>([])
const workshops = ref<WorkshopDto[]>([])
const form = ref<ServiceOrderCreateDto>({ vehicleId: '', workshopId: '', description: '' })
const loadingData = ref(true)
const loading = ref(false)
const error = ref('')

onMounted(async () => {
  try {
    const [v, w] = await Promise.all([vehicleService.getAll(), workshopService.getAll()])
    vehicles.value = v
    workshops.value = w
  } catch {
    error.value = t('common.error')
  } finally {
    loadingData.value = false
  }
})

async function handleCreate() {
  error.value = ''
  loading.value = true
  try {
    const order = await orderService.create(form.value)
    router.push(`/orders/${order.id}`)
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
}
</script>
