<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>📋 Order Details</h2>
      <router-link to="/orders" class="btn btn-outline-secondary">← {{ t('common.back') }}</router-link>
    </div>

    <div v-if="loading" class="text-center py-5"><div class="spinner-border"></div></div>
    <div v-else-if="error" class="alert alert-danger">{{ error }}</div>
    <div v-else-if="order">
      <!-- Order Info Card -->
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

      <!-- Action buttons -->
      <div class="d-flex gap-2 mb-4 flex-wrap">
        <router-link :to="`/orders/${order.id}/progress`" class="btn btn-outline-primary">
          📊 {{ t('orders.statusHistory') }}
        </router-link>
        <!-- Admin: Create Invoice button -->
        <button
          v-if="authStore.isAdmin"
          @click="showInvoiceModal = true"
          class="btn btn-primary"
        >
          💰 {{ t('payments.createInvoice') }}
        </button>
        <!-- Admin/Mechanic: Add Photo button (hidden when limit reached) -->
        <button
          v-if="canUpload && photos.length < 20"
          @click="showUploadModal = true"
          class="btn btn-primary"
        >
          📷 {{ t('photos.addPhoto') }}
        </button>
      </div>

      <!-- Payments section -->
      <div class="card shadow-sm mb-4">
        <div class="card-header">
          <h5 class="mb-0">💰 {{ t('payments.invoices') }}</h5>
        </div>
        <div class="card-body">
          <div v-if="paymentsLoading" class="text-center py-3">
            <div class="spinner-border spinner-border-sm"></div>
          </div>
          <div v-else-if="payments.length === 0" class="text-muted">
            {{ t('payments.noPayments') }}
          </div>
          <div v-else class="row g-3">
            <div v-for="p in payments" :key="p.id" class="col-md-6">
              <div class="card border h-100">
                <div class="card-body p-3">
                  <div class="d-flex justify-content-between align-items-center mb-2">
                    <strong class="fs-5 text-primary">€{{ p.amount.toFixed(2) }}</strong>
                    <span :class="statusBadge2(p.status)" class="badge">
                      {{ t(`payments.status${p.status}`) }}
                    </span>
                  </div>
                  <div class="small text-muted">
                    {{ t('payments.created') }}: {{ formatDate(p.createdAt) }}
                  </div>
                  <div v-if="p.paidAt" class="small text-muted">
                    {{ t('payments.paidAt') }}: {{ formatDate(p.paidAt) }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Photos section -->
      <div class="card shadow-sm">
        <div class="card-header d-flex justify-content-between align-items-center">
          <h5 class="mb-0">📷 {{ t('photos.title') }}</h5>
          <span class="badge bg-secondary">{{ t('photos.photoCount', { count: photos?.length ?? 0 }) }}</span>
        </div>
        <div class="card-body">
          <div v-if="photosLoading" class="text-center py-3">
            <div class="spinner-border spinner-border-sm"></div>
          </div>
          <div v-else-if="photos.length === 0" class="text-muted">
            {{ t('photos.noPhotos') }}
          </div>
          <div v-else class="row g-2">
            <div
              v-for="photo in photos"
              :key="photo.id"
              class="col-6 col-sm-4 col-md-3"
            >
              <div class="position-relative photo-thumb-wrapper rounded-3 overflow-hidden" style="aspect-ratio: 1/1;">
                <img
                  :src="getPhotoUrl(photo.photoUrl)"
                  :alt="photo.description || 'Repair photo'"
                  class="w-100 h-100 photo-thumb"
                  style="object-fit: cover; cursor: pointer;"
                  @click="openViewer(photo)"
                />
                <!-- Delete overlay for admin/mechanic -->
                <div v-if="canUpload" class="photo-delete-overlay position-absolute top-0 end-0 p-1">
                  <button
                    class="btn btn-danger btn-sm rounded-circle"
                    style="width: 28px; height: 28px; padding: 0; font-size: 12px;"
                    :title="t('photos.deletePhoto')"
                    @click.stop="confirmDelete(photo.id)"
                  >🗑️</button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Create Invoice Modal -->
    <div v-if="showInvoiceModal" class="modal d-block" tabindex="-1" style="background: rgba(0,0,0,0.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">💰 {{ t('payments.createInvoice') }}</h5>
            <button type="button" class="btn-close" @click="closeInvoiceModal"></button>
          </div>
          <div class="modal-body">
            <div v-if="invoiceError" class="alert alert-danger">{{ invoiceError }}</div>
            <div class="mb-3">
              <label class="form-label">{{ t('payments.amount') }} (€)</label>
              <input
                v-model.number="invoiceAmount"
                type="number"
                min="0.01"
                step="0.01"
                class="form-control"
                :placeholder="t('payments.amountPlaceholder')"
              />
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-secondary" @click="closeInvoiceModal">{{ t('common.cancel') }}</button>
            <button
              class="btn btn-primary"
              @click="createInvoice"
              :disabled="creatingInvoice || !invoiceAmount || invoiceAmount <= 0"
            >
              <span v-if="creatingInvoice" class="spinner-border spinner-border-sm me-1"></span>
              💰 {{ t('payments.createInvoice') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Upload Photo Modal -->
    <div v-if="showUploadModal" class="modal d-block" tabindex="-1" style="background: rgba(0,0,0,0.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">📷 {{ t('photos.addPhoto') }}</h5>
            <button type="button" class="btn-close" @click="closeUploadModal"></button>
          </div>
          <div class="modal-body">
            <div v-if="uploadError" class="alert alert-danger">{{ uploadError }}</div>
            <div class="mb-3">
              <label class="form-label">{{ t('photos.selectFile') }}</label>
              <input
                type="file"
                accept="image/jpeg,image/png"
                class="form-control"
                @change="onFileSelected"
                ref="fileInputRef"
              />
              <div class="form-text">{{ t('photos.maxFileSize') }} · {{ t('photos.allowedFormats') }}</div>
            </div>
            <!-- Preview -->
            <div v-if="previewUrl" class="mb-3 text-center">
              <img
                :src="previewUrl"
                alt="Preview"
                class="img-fluid rounded-3"
                style="max-height: 200px; object-fit: contain;"
              />
            </div>
            <div class="mb-3">
              <label class="form-label">{{ t('photos.photoDescription') }}</label>
              <textarea
                v-model="uploadDescription"
                class="form-control"
                rows="2"
                :placeholder="t('photos.photoDescription')"
              ></textarea>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-secondary" @click="closeUploadModal">{{ t('common.cancel') }}</button>
            <button
              class="btn btn-primary"
              @click="uploadPhoto"
              :disabled="uploading || !selectedFile"
            >
              <span v-if="uploading" class="spinner-border spinner-border-sm me-1"></span>
              📷 {{ uploading ? t('photos.uploadingPhoto') : t('photos.addPhoto') }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Photo Viewer Modal -->
    <div v-if="viewerPhoto" class="modal d-block" tabindex="-1" style="background: rgba(0,0,0,0.85);" @click.self="viewerPhoto = null">
      <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content bg-dark text-white">
          <div class="modal-header border-secondary">
            <h6 class="modal-title">{{ viewerPhoto.description || '📷 Photo' }}</h6>
            <button type="button" class="btn-close btn-close-white" @click="viewerPhoto = null"></button>
          </div>
          <div class="modal-body text-center p-2">
            <img
              :src="getPhotoUrl(viewerPhoto.photoUrl)"
              :alt="viewerPhoto.description || 'Repair photo'"
              class="img-fluid rounded-3"
              style="max-height: 70vh;"
            />
            <div v-if="viewerPhoto.description" class="mt-2 small text-muted">
              {{ viewerPhoto.description }}
            </div>
            <div class="mt-1 small text-muted">
              {{ formatDate(viewerPhoto.uploadedAt) }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { orderService } from '@/services/orderService'
import { paymentService } from '@/services/paymentService'
import { repairPhotoService } from '@/services/repairPhotoService'
import { useAuthStore } from '@/stores/auth'
import type { ServiceOrderDto, PaymentDto, RepairPhotoDto } from '@/types'

const { t } = useI18n()
const route = useRoute()
const authStore = useAuthStore()

const order = ref<ServiceOrderDto | null>(null)
const loading = ref(true)
const error = ref('')

const payments = ref<PaymentDto[]>([])
const paymentsLoading = ref(true)

const photos = ref<RepairPhotoDto[]>([])
const photosLoading = ref(true)

// Invoice modal state
const showInvoiceModal = ref(false)
const invoiceAmount = ref<number | null>(null)
const invoiceError = ref('')
const creatingInvoice = ref(false)

// Upload photo modal state
const showUploadModal = ref(false)
const selectedFile = ref<File | null>(null)
const previewUrl = ref<string | null>(null)
const uploadDescription = ref('')
const uploading = ref(false)
const uploadError = ref('')
const fileInputRef = ref<HTMLInputElement | null>(null)

// Viewer state
const viewerPhoto = ref<RepairPhotoDto | null>(null)

const orderId = route.params.id as string

// Can current user upload/delete photos?
const canUpload = computed(() => authStore.isAdmin || authStore.isMechanic)

onMounted(async () => {
  try {
    order.value = await orderService.getById(orderId)
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }

  await Promise.all([loadPayments(), loadPhotos()])
})

async function loadPayments() {
  paymentsLoading.value = true
  try {
    payments.value = await paymentService.getAll({ serviceOrderId: orderId })
  } catch {
    // silently ignore — not critical
  } finally {
    paymentsLoading.value = false
  }
}

async function loadPhotos() {
  photosLoading.value = true
  try {
    photos.value = await repairPhotoService.getByOrder(orderId)
  } catch {
    // silently ignore — owner of other order will get 200 empty array; only network errors land here
  } finally {
    photosLoading.value = false
  }
}

// ---- Invoice modal ----
function closeInvoiceModal() {
  showInvoiceModal.value = false
  invoiceAmount.value = null
  invoiceError.value = ''
}

async function createInvoice() {
  if (!invoiceAmount.value || invoiceAmount.value <= 0) return
  creatingInvoice.value = true
  invoiceError.value = ''
  try {
    const created = await paymentService.create({
      serviceOrderId: orderId,
      amount: invoiceAmount.value
    })
    payments.value.push(created)
    closeInvoiceModal()
  } catch (err: any) {
    const msg = err?.response?.data?.error || t('common.error')
    invoiceError.value = msg
  } finally {
    creatingInvoice.value = false
  }
}

// ---- Upload modal ----
function onFileSelected(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0] ?? null
  selectedFile.value = null
  previewUrl.value = null
  uploadError.value = ''

  if (!file) return

  // Client-side validation
  if (file.size > 15 * 1024 * 1024) {
    uploadError.value = t('photos.photoTooLarge')
    return
  }
  if (!['image/jpeg', 'image/png'].includes(file.type)) {
    uploadError.value = t('photos.invalidFormat')
    return
  }

  selectedFile.value = file
  previewUrl.value = URL.createObjectURL(file)
}

function closeUploadModal() {
  showUploadModal.value = false
  selectedFile.value = null
  if (previewUrl.value) {
    URL.revokeObjectURL(previewUrl.value)
    previewUrl.value = null
  }
  uploadDescription.value = ''
  uploadError.value = ''
  if (fileInputRef.value) fileInputRef.value.value = ''
}

async function uploadPhoto() {
  if (!selectedFile.value) return

  // Check limit again before submit
  if (photos.value.length >= 20) {
    uploadError.value = t('photos.maxPhotosReached')
    return
  }

  uploading.value = true
  uploadError.value = ''
  try {
    const formData = new FormData()
    formData.append('ServiceOrderId', orderId)
    formData.append('File', selectedFile.value)
    formData.append('Description', uploadDescription.value?.trim() ?? '')

    const created = await repairPhotoService.upload(formData)
    photos.value.push(created)
    closeUploadModal()
  } catch (err: any) {
    const msg = err?.response?.data?.error || t('common.error')
    uploadError.value = msg
  } finally {
    uploading.value = false
  }
}

// ---- Viewer ----
function openViewer(photo: RepairPhotoDto) {
  viewerPhoto.value = photo
}

// ---- Delete ----
async function confirmDelete(photoId: string) {
  if (!confirm(t('photos.confirmDeletePhoto'))) return
  try {
    await repairPhotoService.delete(photoId)
    photos.value = photos.value.filter(p => p.id !== photoId)
  } catch {
    alert(t('common.error'))
  }
}

// ---- Helpers ----
function getPhotoUrl(photoUrl: string | undefined): string {
  if (!photoUrl) return ''
  return repairPhotoService.getFullUrl(photoUrl)
}

function statusBadge(status: string): string {
  const map: Record<string, string> = {
    Pending: 'bg-secondary',
    InProgress: 'bg-primary',
    Completed: 'bg-success',
    Cancelled: 'bg-danger'
  }
  return map[status] || 'bg-secondary'
}

function statusBadge2(status: string): string {
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
</script>

<style scoped>
.photo-thumb-wrapper {
  background: #f8f9fa;
  border: 1px solid #dee2e6;
}

.photo-thumb-wrapper:hover .photo-delete-overlay {
  opacity: 1;
}

.photo-delete-overlay {
  opacity: 0;
  transition: opacity 0.2s ease;
}
</style>
