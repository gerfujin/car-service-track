<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>📋 {{ t('orders.title') }}</h2>
      <!-- Only admin and client can create orders -->
      <router-link v-if="!authStore.isMechanic" to="/orders/create" class="btn btn-primary">
        + {{ t('orders.create') }}
      </router-link>
    </div>

    <div v-if="loading" class="text-center py-5"><div class="spinner-border"></div></div>
    <div v-else-if="error" class="alert alert-danger">{{ error }}</div>
    <div v-else-if="orders.length === 0" class="alert alert-info">{{ t('orders.noOrders') }}</div>
    <div v-else>
      <div class="bg-light rounded p-3 mb-3">
        <div class="row g-2 align-items-end">
          <div class="col-md-3">
            <label class="form-label">{{ t('orders.filterByStatus') }}</label>
            <select v-model="filterStatus" class="form-select">
              <option value="">{{ t('orders.allStatuses') }}</option>
              <option value="Pending">Pending</option>
              <option value="InProgress">InProgress</option>
              <option value="Completed">Completed</option>
              <option value="Cancelled">Cancelled</option>
            </select>
          </div>

          <div class="col-md-3">
            <label class="form-label">{{ t('orders.filterByWorkshop') }}</label>
            <select v-model="filterWorkshop" class="form-select">
              <option value="">{{ t('orders.allWorkshops') }}</option>
              <option v-for="workshop in workshops" :key="workshop.id" :value="workshop.id">
                {{ workshop.name }}
              </option>
            </select>
          </div>

          <div class="col-md-3">
            <label class="form-label">{{ t('orders.filterByLicensePlate') }}</label>
            <input
              v-model="filterLicensePlate"
              type="text"
              class="form-control"
              :placeholder="t('orders.licensePlatePlaceholder')"
            />
          </div>

          <div class="col-md-3">
            <button class="btn btn-outline-secondary w-100" @click="clearFilters">
              {{ t('orders.clearFilters') }}
            </button>
          </div>
        </div>
      </div>

      <div v-if="filteredOrders.length === 0" class="alert alert-info">{{ t('orders.noOrdersMatchFilter') }}</div>

      <div v-else class="card shadow-sm">
        <div class="card-body p-0">
          <table class="table table-hover mb-0">
          <thead class="table-dark">
            <tr>
              <th>{{ t('orders.vehicle') }}</th>
              <th>{{ t('orders.workshop') }}</th>
              <th>{{ t('orders.status') }}</th>
              <th>{{ t('orders.orderDate') }}</th>
              <th>{{ t('orders.total') }}</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="order in filteredOrders" :key="order.id">
              <td>{{ order.vehicleDisplay }}</td>
              <td>{{ order.workshopName }}</td>
              <td>
                <span :class="statusBadge(order.status)" class="badge">{{ order.status }}</span>
              </td>
              <td>{{ formatDate(order.orderDate) }}</td>
              <td>€{{ order.totalAmount.toFixed(2) }}</td>
              <td class="text-end">
                <router-link :to="`/orders/${order.id}`" class="btn btn-sm btn-outline-primary me-1">Details</router-link>
                <router-link :to="`/orders/${order.id}/progress`" class="btn btn-sm btn-outline-secondary me-1">Progress</router-link>
                <!-- Update status: admin and mechanic only -->
                <router-link
                  v-if="authStore.isAdmin || authStore.isMechanic"
                  :to="`/orders/${order.id}/update-status`"
                  class="btn btn-sm btn-outline-warning"
                >
                  🔄 Status
                </router-link>
              </td>
            </tr>
          </tbody>
        </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { orderService } from '@/services/orderService'
import { workshopService } from '@/services/workshopService'
import { useAuthStore } from '@/stores/auth'
import type { ServiceOrderDto, WorkshopDto } from '@/types'

const { t } = useI18n()
const authStore = useAuthStore()
const orders = ref<ServiceOrderDto[]>([])
const workshops = ref<WorkshopDto[]>([])
const loading = ref(true)
const error = ref('')
const filterStatus = ref('')
const filterWorkshop = ref('')
const filterLicensePlate = ref('')

onMounted(async () => {
  try {
    orders.value = await orderService.getAll()
    workshops.value = await workshopService.getAll()
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
})

const filteredOrders = computed(() => {
  return orders.value.filter(order => {
    if (filterStatus.value && order.status !== filterStatus.value) return false
    if (filterWorkshop.value && order.workshopId !== filterWorkshop.value) return false
    if (filterLicensePlate.value.trim()) {
      const search = filterLicensePlate.value.trim().toLowerCase()
      const vehicleDisplay = (order.vehicleDisplay ?? '').toLowerCase()
      if (!vehicleDisplay.includes(search)) return false
    }

    return true
  })
})

function clearFilters() {
  filterStatus.value = ''
  filterWorkshop.value = ''
  filterLicensePlate.value = ''
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

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString()
}
</script>
