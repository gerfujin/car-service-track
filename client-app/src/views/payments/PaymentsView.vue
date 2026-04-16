<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>💰 {{ t('payments.title') }}</h2>
    </div>

    <div v-if="loading" class="text-center py-5"><div class="spinner-border"></div></div>
    <div v-else-if="error" class="alert alert-danger">{{ error }}</div>
    <div v-else-if="payments.length === 0" class="alert alert-info">{{ t('payments.noPayments') }}</div>
    <div v-else class="card shadow-sm">
      <div class="card-body p-0">
        <table class="table table-hover mb-0">
          <thead class="table-dark">
            <tr>
              <th>{{ t('payments.amount') }}</th>
              <th>{{ t('payments.status') }}</th>
              <th>{{ t('payments.method') }}</th>
              <th>{{ t('payments.paidAt') }}</th>
              <th>Created</th>
              <th class="text-end">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in payments" :key="p.id">
              <td><strong>€{{ p.amount.toFixed(2) }}</strong></td>
              <td>
                <span :class="statusBadge(p.status)" class="badge">{{ p.status }}</span>
              </td>
              <td>{{ p.paymentMethod || '—' }}</td>
              <td>{{ p.paidAt ? formatDate(p.paidAt) : '—' }}</td>
              <td>{{ formatDate(p.createdAt) }}</td>
              <td class="text-end">
                <router-link :to="`/payments/${p.id}`" class="btn btn-sm btn-outline-primary">Details</router-link>
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
import { paymentService } from '@/services/paymentService'
import type { PaymentDto } from '@/types'

const { t } = useI18n()
const payments = ref<PaymentDto[]>([])
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    payments.value = await paymentService.getAll()
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
  return new Date(dateStr).toLocaleDateString()
}
</script>
