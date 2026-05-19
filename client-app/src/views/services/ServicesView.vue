<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>🔩 {{ t('services.title') }}</h2>
      <button
        v-if="authStore.isAdmin"
        class="btn btn-primary"
        @click="openCreate"
      >
        + {{ t('services.create') }}
      </button>
    </div>

    <div v-if="message" class="alert alert-success">{{ message }}</div>

    <div v-if="loading" class="text-center py-5">
      <div class="spinner-border"></div>
    </div>

    <div v-else-if="error" class="alert alert-danger">{{ error }}</div>

    <div v-else-if="services.length === 0" class="alert alert-info">
      {{ t('services.noServices') }}
    </div>

    <div v-else class="row g-3">
      <div v-for="s in services" :key="s.id" class="col-md-6 col-lg-4">
        <div class="card shadow-sm h-100">
          <div class="card-body">
            <h5 class="card-title">{{ s.name }}</h5>
            <p class="card-text text-muted mb-2">{{ s.description || '-' }}</p>
            <p class="mb-1"><strong>{{ t('services.basePrice') }}:</strong> €{{ s.basePrice.toFixed(2) }}</p>
            <p class="mb-0"><strong>{{ t('services.estimatedTime') }}:</strong> {{ formatTime(s.estimatedTimeMinutes) }}</p>
          </div>
          <div v-if="authStore.isAdmin" class="card-footer d-flex gap-2">
            <button class="btn btn-sm btn-outline-primary" @click="openEdit(s)">{{ t('services.edit') }}</button>
            <button class="btn btn-sm btn-outline-danger ms-auto" @click="removeService(s.id)">{{ t('services.delete') }}</button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="authStore.isAdmin && showForm" class="modal d-block" tabindex="-1" style="background: rgba(0,0,0,0.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">
              {{ editId ? t('services.edit') : t('services.create') }}
            </h5>
            <button type="button" class="btn-close" @click="closeForm"></button>
          </div>
          <div class="modal-body">
            <div v-if="formError" class="alert alert-danger">{{ formError }}</div>

            <div class="mb-3">
              <label class="form-label">{{ t('services.name') }}</label>
              <input v-model="form.name" class="form-control" />
            </div>
            <div class="mb-3">
              <label class="form-label">{{ t('services.description') }}</label>
              <textarea v-model="form.description" class="form-control" rows="2"></textarea>
            </div>
            <div class="mb-3">
              <label class="form-label">{{ t('services.basePrice') }}</label>
              <input v-model.number="form.basePrice" type="number" min="0" step="0.01" class="form-control" />
            </div>
            <div class="mb-1">
              <label class="form-label">{{ t('services.estimatedTimeMinutes') }}</label>
              <input v-model.number="form.estimatedTimeMinutes" type="number" min="0" class="form-control" />
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline-secondary" @click="closeForm">{{ t('services.cancel') }}</button>
            <button class="btn btn-primary" :disabled="saving" @click="saveService">
              {{ t('services.save') }}
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
import { serviceService, type ServiceUpsertDto } from '@/services/serviceService'
import { useAuthStore } from '@/stores/auth'
import type { ServiceDto } from '@/types'

const { t } = useI18n()
const authStore = useAuthStore()
const services = ref<ServiceDto[]>([])
const loading = ref(true)
const error = ref('')
const message = ref('')

const showForm = ref(false)
const editId = ref<string | null>(null)
const saving = ref(false)
const formError = ref('')
const form = ref<ServiceUpsertDto>({
  name: '',
  description: '',
  basePrice: 0,
  estimatedTimeMinutes: 0
})

onMounted(async () => {
  await loadServices()
})

async function loadServices() {
  try {
    error.value = ''
    services.value = await serviceService.getAll()
  } catch {
    error.value = t('services.errorLoad')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editId.value = null
  form.value = { name: '', description: '', basePrice: 0, estimatedTimeMinutes: 0 }
  formError.value = ''
  showForm.value = true
}

function openEdit(service: ServiceDto) {
  editId.value = service.id
  form.value = {
    name: service.name,
    description: service.description ?? '',
    basePrice: service.basePrice,
    estimatedTimeMinutes: service.estimatedTimeMinutes
  }
  formError.value = ''
  showForm.value = true
}

function closeForm() {
  showForm.value = false
}

function validateForm(): string | null {
  if (!form.value.name.trim()) return t('services.name') + ' is required'
  if (form.value.basePrice < 0) return t('services.basePrice') + ' >= 0'
  if (form.value.estimatedTimeMinutes < 0) return t('services.estimatedTimeMinutes') + ' >= 0'
  return null
}

async function saveService() {
  const validationMessage = validateForm()
  if (validationMessage) {
    formError.value = validationMessage
    return
  }

  saving.value = true
  formError.value = ''
  message.value = ''

  try {
    if (editId.value) {
      await serviceService.update(editId.value, form.value)
      message.value = t('services.updated')
    } else {
      await serviceService.create(form.value)
      message.value = t('services.created')
    }
    closeForm()
    await loadServices()
  } catch (err: any) {
    formError.value = err?.response?.data?.error || t('services.errorSave')
  } finally {
    saving.value = false
  }
}

async function removeService(id: string) {
  if (!confirm(t('services.confirmDelete'))) return
  try {
    await serviceService.delete(id)
    message.value = t('services.deleted')
    await loadServices()
  } catch (err: any) {
    error.value = err?.response?.data?.error || t('services.errorDelete')
  }
}

function formatTime(minutes: number): string {
  if (minutes < 60) return `${minutes} min`
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  if (m === 0) return `${h} h`
  return `${h} h ${m} min`
}
</script>
