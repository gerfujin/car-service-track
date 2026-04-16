<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>🚗 {{ authStore.isMechanic ? 'All Vehicles (Read-only)' : t('vehicles.title') }}</h2>
      <!-- Only admin and client can create vehicles -->
      <router-link v-if="!authStore.isMechanic" to="/vehicles/create" class="btn btn-primary">
        + {{ t('vehicles.add') }}
      </router-link>
    </div>

    <div v-if="loading" class="text-center py-5">
      <div class="spinner-border"></div>
    </div>

    <div v-else-if="error" class="alert alert-danger">{{ error }}</div>

    <div v-else-if="vehicles.length === 0" class="alert alert-info">
      {{ t('vehicles.noVehicles') }}
    </div>

    <div v-else class="row g-3">
      <div v-for="v in vehicles" :key="v.id" class="col-md-6 col-lg-4">
        <div class="card shadow-sm h-100">
          <div class="card-body">
            <h5 class="card-title">{{ v.make }} {{ v.model }}</h5>
            <p class="card-text text-muted mb-1">
              <strong>{{ t('vehicles.year') }}:</strong> {{ v.year }}
            </p>
            <p class="card-text text-muted mb-1">
              <strong>{{ t('vehicles.licensePlate') }}:</strong>
              <span class="badge bg-dark ms-1">{{ v.licensePlate }}</span>
            </p>
            <p v-if="v.color" class="card-text text-muted mb-1">
              <strong>{{ t('vehicles.color') }}:</strong> {{ v.color }}
            </p>
            <p v-if="v.mileage" class="card-text text-muted mb-1">
              <strong>{{ t('vehicles.mileage') }}:</strong> {{ v.mileage.toLocaleString() }} km
            </p>
          </div>
          <div class="card-footer d-flex gap-2">
            <router-link :to="`/vehicles/${v.id}`" class="btn btn-sm btn-outline-primary">Details</router-link>
            <!-- Edit/Delete only for admin and client -->
            <template v-if="!authStore.isMechanic">
              <router-link :to="`/vehicles/${v.id}/edit`" class="btn btn-sm btn-outline-secondary">{{ t('vehicles.edit') }}</router-link>
              <button @click="deleteVehicle(v.id)" class="btn btn-sm btn-outline-danger ms-auto">{{ t('vehicles.delete') }}</button>
            </template>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { vehicleService } from '@/services/vehicleService'
import { useAuthStore } from '@/stores/auth'
import type { VehicleDto } from '@/types'

const { t } = useI18n()
const authStore = useAuthStore()
const vehicles = ref<VehicleDto[]>([])
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    vehicles.value = await vehicleService.getAll()
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
})

async function deleteVehicle(id: string) {
  if (!confirm(t('vehicles.confirmDelete'))) return
  try {
    await vehicleService.delete(id)
    vehicles.value = vehicles.value.filter(v => v.id !== id)
  } catch {
    alert(t('common.error'))
  }
}
</script>
