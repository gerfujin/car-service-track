<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>📊 {{ t('orders.statusHistory') }}</h2>
      <router-link :to="`/orders/${route.params.id}`" class="btn btn-outline-secondary">← {{ t('common.back') }}</router-link>
    </div>

    <div v-if="loading" class="text-center py-5"><div class="spinner-border"></div></div>
    <div v-else-if="error" class="alert alert-danger">{{ error }}</div>
    <div v-else>
      <div v-if="history.length === 0" class="alert alert-info">No status history available.</div>
      <div v-else class="card shadow-sm">
        <div class="card-body">
          <div class="timeline">
            <div v-for="(entry, index) in history" :key="entry.id" class="d-flex mb-4">
              <div class="me-3 text-center" style="min-width: 80px;">
                <span :class="statusBadge(entry.status)" class="badge mb-1">{{ entry.status }}</span>
                <div class="text-muted small">{{ formatDate(entry.changedAt) }}</div>
              </div>
              <div class="flex-grow-1">
                <div class="card border-0 bg-light">
                  <div class="card-body py-2">
                    <p class="mb-0">{{ entry.notes || 'Status updated' }}</p>
                  </div>
                </div>
                <div v-if="index < history.length - 1" class="border-start ms-3 mt-1" style="height: 20px;"></div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { orderService } from '@/services/orderService'
import type { StatusHistoryEntry } from '@/types'

const { t } = useI18n()
const route = useRoute()
const history = ref<StatusHistoryEntry[]>([])
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    history.value = await orderService.getStatusHistory(route.params.id as string)
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
  return new Date(dateStr).toLocaleString()
}
</script>
