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
            <tr v-for="order in orders" :key="order.id">
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
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { orderService } from '@/services/orderService'
import { useAuthStore } from '@/stores/auth'
import type { ServiceOrderDto } from '@/types'

const { t } = useI18n()
const authStore = useAuthStore()
const orders = ref<ServiceOrderDto[]>([])
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    orders.value = await orderService.getAll()
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
})

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
