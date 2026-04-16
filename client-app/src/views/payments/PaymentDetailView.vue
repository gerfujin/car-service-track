<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>💰 Payment Details</h2>
      <router-link to="/payments" class="btn btn-outline-secondary">← {{ t('common.back') }}</router-link>
    </div>

    <div v-if="loading" class="text-center py-5"><div class="spinner-border"></div></div>
    <div v-else-if="error" class="alert alert-danger">{{ error }}</div>
    <div v-else-if="payment">
      <div class="card shadow-sm">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="mb-0">Payment #{{ payment.id.substring(0, 8) }}...</h5>
          <span :class="statusBadge(payment.status)" class="badge fs-6">{{ payment.status }}</span>
        </div>
        <div class="card-body">
          <dl class="row">
            <dt class="col-sm-3">{{ t('payments.amount') }}</dt>
            <dd class="col-sm-9"><strong class="fs-5">€{{ payment.amount.toFixed(2) }}</strong></dd>
            <dt class="col-sm-3">{{ t('payments.status') }}</dt>
            <dd class="col-sm-9">{{ payment.status }}</dd>
            <dt v-if="payment.paymentMethod" class="col-sm-3">{{ t('payments.method') }}</dt>
            <dd v-if="payment.paymentMethod" class="col-sm-9">{{ payment.paymentMethod }}</dd>
            <dt v-if="payment.paidAt" class="col-sm-3">{{ t('payments.paidAt') }}</dt>
            <dd v-if="payment.paidAt" class="col-sm-9">{{ formatDate(payment.paidAt) }}</dd>
            <dt class="col-sm-3">Created</dt>
            <dd class="col-sm-9">{{ formatDate(payment.createdAt) }}</dd>
            <dt v-if="payment.notes" class="col-sm-3">Notes</dt>
            <dd v-if="payment.notes" class="col-sm-9">{{ payment.notes }}</dd>
          </dl>
        </div>
        <div class="card-footer">
          <router-link :to="`/orders/${payment.serviceOrderId}`" class="btn btn-outline-primary btn-sm">
            📋 View Related Order
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { paymentService } from '@/services/paymentService'
import type { PaymentDto } from '@/types'

const { t } = useI18n()
const route = useRoute()
const payment = ref<PaymentDto | null>(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    payment.value = await paymentService.getById(route.params.id as string)
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
})

function statusBadge(status: string): string {
  const map: Record<string, string> = {
    'Pending': 'bg-warning text-dark',
    'Paid': 'bg-success',
    'Failed': 'bg-danger',
    'Refunded': 'bg-info'
  }
  return map[status] || 'bg-secondary'
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString()
}
</script>
