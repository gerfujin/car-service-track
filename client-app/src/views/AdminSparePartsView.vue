<template>
  <div class="container py-4">
    <div v-if="!authStore.isAdmin" class="alert alert-danger">Forbidden</div>
    <template v-else>
      <div class="d-flex justify-content-between align-items-center mb-3">
        <h2>{{ t('spareParts.title') }}</h2>
        <button class="btn btn-primary" @click="startCreate">+ {{ t('spareParts.create') }}</button>
      </div>

      <div v-if="error" class="alert alert-danger">{{ error }}</div>

      <div class="card shadow-sm mb-4">
        <div class="card-body">
          <div class="row g-3">
            <div class="col-md-3">
              <label class="form-label">{{ t('spareParts.name') }}</label>
              <input v-model="form.name" class="form-control" :placeholder="t('spareParts.name')" />
              <small v-if="validationErrors.name" class="text-danger">{{ validationErrors.name }}</small>
            </div>
            <div class="col-md-2">
              <label class="form-label">{{ t('spareParts.partNumber') }}</label>
              <input v-model="form.partNumber" class="form-control" :placeholder="t('spareParts.partNumber')" />
              <small v-if="validationErrors.partNumber" class="text-danger">{{ validationErrors.partNumber }}</small>
            </div>
            <div class="col-md-2">
              <label class="form-label">{{ t('spareParts.price') }}</label>
              <input v-model.number="form.price" type="number" min="0" step="0.01" class="form-control" :placeholder="t('spareParts.price')" />
              <small v-if="validationErrors.price" class="text-danger">{{ validationErrors.price }}</small>
            </div>
            <div class="col-md-3">
              <label class="form-label">{{ t('spareParts.country') }}</label>
              <input v-model="form.country" class="form-control" :placeholder="t('spareParts.country')" />
            </div>
            <div class="col-md-2">
              <label class="form-label">{{ t('spareParts.stockQuantity') }}</label>
              <input v-model.number="form.stockQuantity" type="number" min="0" class="form-control" :placeholder="t('spareParts.stockQuantity')" />
              <small v-if="validationErrors.stockQuantity" class="text-danger">{{ validationErrors.stockQuantity }}</small>
            </div>
          </div>
          <div class="mt-3 d-flex gap-2">
            <button class="btn btn-success" @click="save">{{ t('spareParts.save') }}</button>
            <button class="btn btn-outline-secondary" @click="resetForm">{{ t('spareParts.cancel') }}</button>
          </div>
        </div>
      </div>

      <div v-if="parts.length === 0" class="alert alert-info">{{ t('spareParts.errorLoad') }}</div>
      <div v-else class="row g-3">
        <div v-for="sp in parts" :key="sp.id" class="col-md-6 col-lg-4">
          <div class="card shadow-sm h-100">
            <div class="card-body">
              <h5 class="card-title">{{ sp.name }}</h5>
              <p class="mb-1"><strong>{{ t('spareParts.partNumber') }}:</strong> {{ sp.partNumber || '-' }}</p>
              <p class="mb-1"><strong>{{ t('spareParts.country') }}:</strong> {{ sp.country || '-' }}</p>
              <p class="mb-1"><strong>{{ t('spareParts.price') }}:</strong> €{{ sp.price.toFixed(2) }}</p>
              <p class="mb-0"><strong>{{ t('spareParts.stockQuantity') }}:</strong> {{ sp.stockQuantity }}</p>
            </div>
            <div class="card-footer d-flex gap-2">
              <button class="btn btn-sm btn-outline-secondary" @click="viewPart(sp)">{{ t('common.view') }}</button>
              <button class="btn btn-sm btn-outline-primary" @click="startEdit(sp)">{{ t('spareParts.edit') }}</button>
              <button class="btn btn-sm btn-outline-danger ms-auto" @click="remove(sp.id)">{{ t('spareParts.delete') }}</button>
            </div>
          </div>
        </div>
      </div>

      <div v-if="showDetails && selectedPart" class="modal d-block" tabindex="-1" style="background: rgba(0,0,0,0.5)">
        <div class="modal-dialog modal-dialog-centered">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">{{ t('spareParts.details') }}</h5>
              <button type="button" class="btn-close" @click="closeDetails"></button>
            </div>
            <div class="modal-body">
              <p><strong>{{ t('spareParts.name') }}:</strong> {{ selectedPart.name }}</p>
              <p><strong>{{ t('spareParts.partNumber') }}:</strong> {{ selectedPart.partNumber || '-' }}</p>
              <p><strong>{{ t('spareParts.country') }}:</strong> {{ selectedPart.country || '-' }}</p>
              <p><strong>{{ t('spareParts.price') }}:</strong> €{{ selectedPart.price.toFixed(2) }}</p>
              <p class="mb-0"><strong>{{ t('spareParts.stockQuantity') }}:</strong> {{ selectedPart.stockQuantity }}</p>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import { sparePartService } from '@/services/sparePartService'
import type { SparePartDto, SparePartCreateDto } from '@/types'

const { t } = useI18n()
const authStore = useAuthStore()

const parts = ref<SparePartDto[]>([])
const error = ref('')
const editId = ref<string | null>(null)
const form = ref<SparePartCreateDto>({ name: '', partNumber: '', manufacturer: '', price: 0, country: '', stockQuantity: 0 })
const selectedPart = ref<SparePartDto | null>(null)
const showDetails = ref(false)
const validationErrors = ref<Record<string, string>>({})

onMounted(load)

async function load() {
  try {
    parts.value = await sparePartService.getAll()
  } catch {
    error.value = t('spareParts.errorLoad')
  }
}

function startCreate() {
  resetForm()
}

function startEdit(sp: SparePartDto) {
  editId.value = sp.id
  form.value = { name: sp.name, partNumber: sp.partNumber, manufacturer: sp.partNumber, price: sp.price, country: sp.country, stockQuantity: sp.stockQuantity }
}

async function save() {
  validationErrors.value = {}
  if (!form.value.name.trim()) validationErrors.value.name = `${t('spareParts.name')} is required`
  if (!form.value.partNumber?.trim()) validationErrors.value.partNumber = `${t('spareParts.partNumber')} is required`
  if (form.value.price < 0) validationErrors.value.price = `${t('spareParts.price')} >= 0`
  if (form.value.stockQuantity < 0) validationErrors.value.stockQuantity = `${t('spareParts.stockQuantity')} >= 0`
  if (Object.keys(validationErrors.value).length > 0) return

  try {
    form.value.manufacturer = form.value.partNumber
    if (editId.value) {
      await sparePartService.update(editId.value, form.value)
    } else {
      await sparePartService.create(form.value)
    }
    await load()
    resetForm()
  } catch {
    error.value = t('spareParts.errorSave')
  }
}

async function remove(id: string) {
  if (!confirm(t('spareParts.confirmDelete'))) return
  try {
    await sparePartService.delete(id)
    await load()
  } catch {
    error.value = t('spareParts.errorDelete')
  }
}

function resetForm() {
  editId.value = null
  form.value = { name: '', partNumber: '', manufacturer: '', price: 0, country: '', stockQuantity: 0 }
  validationErrors.value = {}
}

function viewPart(sp: SparePartDto) {
  selectedPart.value = sp
  showDetails.value = true
}

function closeDetails() {
  showDetails.value = false
  selectedPart.value = null
}
</script>

