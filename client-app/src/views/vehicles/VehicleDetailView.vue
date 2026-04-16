<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>🚗 Vehicle Details</h2>
      <router-link to="/vehicles" class="btn btn-outline-secondary">← {{ t('common.back') }}</router-link>
    </div>

    <div v-if="loading" class="text-center py-5"><div class="spinner-border"></div></div>
    <div v-else-if="error" class="alert alert-danger">{{ error }}</div>
    <div v-else-if="vehicle">
      <div class="card shadow-sm mb-4">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="mb-0">{{ vehicle.make }} {{ vehicle.model }} ({{ vehicle.year }})</h5>
          <div class="d-flex gap-2">
            <router-link :to="`/vehicles/${vehicle.id}/edit`" class="btn btn-sm btn-outline-primary">{{ t('vehicles.edit') }}</router-link>
          </div>
        </div>
        <div class="card-body">
          <dl class="row">
            <dt class="col-sm-3">{{ t('vehicles.licensePlate') }}</dt>
            <dd class="col-sm-9"><span class="badge bg-dark fs-6">{{ vehicle.licensePlate }}</span></dd>
            <dt class="col-sm-3">{{ t('vehicles.year') }}</dt>
            <dd class="col-sm-9">{{ vehicle.year }}</dd>
            <dt v-if="vehicle.color" class="col-sm-3">{{ t('vehicles.color') }}</dt>
            <dd v-if="vehicle.color" class="col-sm-9">{{ vehicle.color }}</dd>
            <dt v-if="vehicle.mileage" class="col-sm-3">{{ t('vehicles.mileage') }}</dt>
            <dd v-if="vehicle.mileage" class="col-sm-9">{{ vehicle.mileage.toLocaleString() }} km</dd>
            <dt v-if="vehicle.vin" class="col-sm-3">{{ t('vehicles.vin') }}</dt>
            <dd v-if="vehicle.vin" class="col-sm-9"><code>{{ vehicle.vin }}</code></dd>
          </dl>
        </div>
      </div>

      <div class="d-flex gap-2">
        <router-link to="/orders/create" class="btn btn-primary">📋 Create Service Order</router-link>
        <router-link to="/orders" class="btn btn-outline-secondary">View All Orders</router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { vehicleService } from '@/services/vehicleService'
import type { VehicleDto } from '@/types'

const { t } = useI18n()
const route = useRoute()
const vehicle = ref<VehicleDto | null>(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    vehicle.value = await vehicleService.getById(route.params.id as string)
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
})
</script>
