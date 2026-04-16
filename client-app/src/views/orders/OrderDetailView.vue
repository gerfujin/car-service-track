<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>📋 Order Details</h2>
      <router-link to="/orders" class="btn btn-outline-secondary">← {{ t('common.back') }}</router-link>
    </div>

    <div v-if="loading" class="text-center py-5"><div class="spinner-border"></div></div>
    <div v-else-if="error" class="alert alert-danger">{{ error }}</div>
    <div v-else-if="order">
      <div class="card shadow-sm mb-4">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="mb-0">Order #{{ order.id.substring(0, 8) }}...</h5>
          <span :class="statusBadge(order.status)" class="badge fs-6">{{ order.status }}</span>
        </div>
        <div class="card-body">
          <dl class="row">
            <dt class="col-sm-3">{{ t('orders.vehicle') }}</dt>
            <dd class="col-sm-9">{{ order.vehicleDisplay }}</dd>
            <dt class="col-sm-3">{{ t('orders.workshop') }}</dt>
            <dd class="col-sm-9">{{ order.workshopName }}</dd>
            <dt v-if="order.mechanicName" class="col-sm-3">{{ t('orders.mechanic') }}</dt>
            <dd v-if="order.mechanicName" class="col-sm-9">{{ order.mechanicName }}</dd>
            <dt class="col-sm-3">{{ t('orders.orderDate') }}</dt>
            <dd class="col-sm-9">{{ formatDate(order.orderDate) }}</dd>
            <dt v-if="order.completedDate" class="col-sm-3">Completed</dt>
            <dd v-if="order.completedDate" class="col-sm-9">{{ formatDate(order.completedDate) }}</dd>
            <dt v-if="order.description" class="col-sm-3">Description</dt>
            <dd v-if="order.description" class="col-sm-9">{{ order.description }}</dd>
            <dt class="col-sm-3">{{ t('orders.total') }}</dt>
            <dd class="col-sm-9"><strong>€{{ order.totalAmount.toFixed(2) }}</strong></dd>
          </dl>
        </div>
      </div>

      <div class="d-flex gap-2">
        <router-link :to="`/orders/${order.id}/progress`" class="btn btn-outline-primary">
          📊 {{ t('orders.statusHistory') }}
        </router-link>
        <router-link to="/payments" class="btn btn-outline-secondary">
          💰 View Payments
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { orderService } from '@/services/orderService'
import type { ServiceOrderDto } from '@/types'

const { t } = useI18n()
const route = useRoute()
const order = ref<ServiceOrderDto | null>(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    order.value = await orderService.getById(route.params.id as string)
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
