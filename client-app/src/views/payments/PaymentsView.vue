<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>💰 {{ t('payments.title') }}</h2>
    </div>

    <div v-if="loading" class="text-center py-5">
      <div class="spinner-border"></div>
    </div>

    <div v-else-if="error" class="alert alert-danger">{{ error }}</div>

    <div v-else-if="payments.length === 0" class="alert alert-info">
      {{ t('payments.noPayments') }}
    </div>

    <div v-else class="row g-3">
      <div v-for="p in payments" :key="p.id" class="col-md-6 col-lg-4">
        <div class="card shadow-sm h-100 border">
          <div class="card-body p-4">
            <div class="d-flex justify-content-between align-items-start mb-3">
              <span :class="statusBadge(p.status)" class="badge fs-6">
                {{ t(`payments.status${p.status}`) }}
              </span>
              <small class="text-muted">
                #{{ p.serviceOrderId.substring(p.serviceOrderId.length - 8) }}
              </small>
            </div>

            <div class="text-center mb-3">
              <strong class="fs-2 text-primary">€{{ p.amount.toFixed(2) }}</strong>
            </div>

            <div v-if="p.vehicleInfo" class="text-muted small mb-1">
              🚗 {{ p.vehicleInfo }}
            </div>
            <div v-if="p.workshopName" class="text-muted small mb-1">
              🔧 {{ p.workshopName }}
            </div>

            <hr class="my-2" />

            <div class="small text-muted">
              <div>{{ t('payments.created') }}: {{ formatDate(p.createdAt) }}</div>
              <div v-if="p.paidAt">{{ t('payments.paidAt') }}: {{ formatDate(p.paidAt) }}</div>
            </div>
          </div>

          <div class="card-footer d-flex gap-2 flex-wrap">
            <router-link
              :to="`/orders/${p.serviceOrderId}`"
              class="btn btn-sm btn-outline-primary"
            >
              📋 {{ t('payments.viewOrder') }}
            </router-link>

            <button
              v-if="!authStore.isAdmin && !authStore.isMechanic && p.status === 'Pending'"
              @click="payInvoice(p)"
              class="btn btn-sm btn-success ms-auto"
              :disabled="paying === p.id"
            >
              <span v-if="paying === p.id" class="spinner-border spinner-border-sm me-1"></span>
              💰 {{ t('payments.pay') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { paymentService } from '@/services/paymentService'
import { useAuthStore } from '@/stores/auth'
import type { PaymentDto } from '@/types'

const { t } = useI18n()
const authStore = useAuthStore()
const payments = ref<PaymentDto[]>([])
const loading = ref(true)
const error = ref('')
const paying = ref<string | null>(null)

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
    Pending: 'bg-warning text-dark',
    Paid: 'bg-success',
    PartiallyPaid: 'bg-info',
    Refunded: 'bg-secondary',
    Cancelled: 'bg-danger'
  }
  return map[status] || 'bg-secondary'
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString()
}

async function payInvoice(p: PaymentDto) {
  if (!confirm(t('payments.confirmPay', { amount: p.amount.toFixed(2) }))) return
  paying.value = p.id
  try {
    await paymentService.pay(p.id)
    // Update local state
    const idx = payments.value.findIndex(x => x.id === p.id)
    if (idx !== -1) {
      payments.value[idx] = {
        ...payments.value[idx],
        status: 'Paid',
        statusName: 'Paid',
        paidAt: new Date().toISOString()
      }
    }
  } catch {
    alert(t('common.error'))
  } finally {
    paying.value = null
  }
}
</script>
