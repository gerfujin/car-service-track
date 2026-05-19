<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>📋 {{ t('orders.create') }}</h2>
      <router-link to="/orders" class="btn btn-outline-secondary">← {{ t('common.back') }}</router-link>
    </div>

    <div v-if="loadingData" class="text-center py-5"><div class="spinner-border"></div></div>
    <div v-else class="card shadow-sm" style="max-width: 800px;">
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

          <!-- Services Selection Section -->
          <div class="mb-4">
            <label class="form-label fw-bold">{{ t('orders.selectServices') }}</label>
            <div v-if="services.length === 0" class="text-muted">
              {{ t('services.noServices') }}
            </div>
            <div v-else class="border rounded">
              <!-- Scrollable service list -->
              <div style="max-height: 320px; overflow-y: auto;">
                <div 
                  v-for="s in services" 
                  :key="s.id"
                  class="d-flex align-items-center px-3 py-2 border-bottom service-row"
                  :class="{ 'bg-primary bg-opacity-10': selectedServiceIds.includes(s.id) }"
                  @click="toggleService(s.id)"
                  style="cursor: pointer;"
                >
                  <div class="form-check me-3">
                    <input 
                      type="checkbox" 
                      class="form-check-input" 
                      :id="'service-' + s.id"
                      :checked="selectedServiceIds.includes(s.id)"
                      @click.stop="toggleService(s.id)"
                    >
                  </div>
                  <div class="flex-grow-1">
                    <span class="fw-medium">{{ s.name }}</span>
                  </div>
                  <div class="text-end text-nowrap ms-3">
                    <span class="fw-semibold">€{{ s.basePrice.toFixed(2) }}</span>
                  </div>
                  <div class="text-end text-nowrap ms-3 text-muted small" style="min-width: 80px;">
                    {{ formatTime(s.estimatedTimeMinutes) }}
                  </div>
                </div>
              </div>

              <!-- Other Option -->
              <div 
                class="d-flex align-items-center px-3 py-2 border-top-2 service-row"
                :class="{ 'bg-primary bg-opacity-10': otherSelected }"
                @click="toggleOther"
                style="cursor: pointer; border-top-width: 2px !important;"
              >
                <div class="form-check me-3">
                  <input 
                    type="checkbox" 
                    class="form-check-input" 
                    id="service-other"
                    v-model="otherSelected"
                    @click.stop="toggleOther"
                  >
                </div>
                <div class="flex-grow-1">
                  <span class="fw-medium">{{ t('orders.other') }}</span>
                  <span class="text-muted small ms-2">(Custom service or issue not listed above)</span>
                </div>
              </div>
              <div v-if="otherSelected" class="px-3 py-2 bg-light border-top small text-muted">
                ℹ️ Please describe your issue in the Description field below
              </div>
            </div>
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
import { serviceService } from '@/services/serviceService'
import type { VehicleDto, WorkshopDto, ServiceDto, ServiceOrderCreateDto } from '@/types'

const { t } = useI18n()
const router = useRouter()

const vehicles = ref<VehicleDto[]>([])
const workshops = ref<WorkshopDto[]>([])
const services = ref<ServiceDto[]>([])
const selectedServiceIds = ref<string[]>([])
const otherSelected = ref(false)
const form = ref<ServiceOrderCreateDto>({ vehicleId: '', workshopId: '', description: '' })
const loadingData = ref(true)
const loading = ref(false)
const error = ref('')

onMounted(async () => {
  try {
    const [v, w, s] = await Promise.all([
      vehicleService.getAll(), 
      workshopService.getAll(),
      serviceService.getAll()
    ])
    vehicles.value = v
    workshops.value = w
    services.value = s
  } catch {
    error.value = t('common.error')
  } finally {
    loadingData.value = false
  }
})

function toggleService(serviceId: string) {
  const index = selectedServiceIds.value.indexOf(serviceId)
  if (index > -1) {
    selectedServiceIds.value.splice(index, 1)
  } else {
    selectedServiceIds.value.push(serviceId)
  }
}

function toggleOther() {
  otherSelected.value = !otherSelected.value
}

function formatTime(minutes: number): string {
  if (minutes < 60) return `${minutes} min`
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  if (m === 0) return `${h} h`
  return `${h} h ${m} min`
}

async function handleCreate() {
  error.value = ''
  loading.value = true
  try {
    const payload: ServiceOrderCreateDto = {
      ...form.value,
      serviceIds: selectedServiceIds.value.length > 0 ? selectedServiceIds.value : undefined
    }
    const order = await orderService.create(payload)
    router.push(`/orders/${order.id}`)
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.service-row:hover {
  background-color: #f8f9fa !important;
}

.service-row.bg-primary {
  background-color: rgba(13, 110, 253, 0.1) !important;
}
</style>
