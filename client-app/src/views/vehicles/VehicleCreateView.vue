<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h2>🚗 {{ t('vehicles.add') }}</h2>
      <router-link to="/vehicles" class="btn btn-outline-secondary">← {{ t('common.back') }}</router-link>
    </div>

    <div class="card shadow-sm" style="max-width: 600px;">
      <div class="card-body">
        <div v-if="error" class="alert alert-danger">{{ error }}</div>

        <form @submit.prevent="handleCreate">
          <div class="mb-3">
            <label class="form-label">{{ t('vehicles.make') }} *</label>
            <input v-model="form.make" class="form-control" required placeholder="e.g. Toyota" />
          </div>
          <div class="mb-3">
            <label class="form-label">{{ t('vehicles.model') }} *</label>
            <input v-model="form.model" class="form-control" required placeholder="e.g. Corolla" />
          </div>
          <div class="mb-3">
            <label class="form-label">{{ t('vehicles.year') }} *</label>
            <input v-model.number="form.year" type="number" class="form-control" required min="1900" :max="new Date().getFullYear() + 1" />
          </div>
          <div class="mb-3">
            <label class="form-label">{{ t('vehicles.licensePlate') }} *</label>
            <input v-model="form.licensePlate" class="form-control" required placeholder="e.g. 123ABC" />
          </div>

          <div class="mb-3">
            <label class="form-label">{{ t('vehicles.vin') }}</label>
            <input
              :value="form.vin"
              @input="onVinInput"
              @blur="vinTouched = true"
              class="form-control"
              :class="{ 'is-invalid': vinTouched && vinError, 'is-valid': vinTouched && !vinError && (form.vin?.length ?? 0) === 17 }"
              maxlength="17"
              pattern="[A-HJ-NPR-Z0-9]{17}"
              :title="t('vehicles.vinPatternHint')"
              placeholder="17-character VIN"
              style="text-transform: uppercase;"
              required
            />
            <div class="form-text" :class="(form.vin?.length ?? 0) === 17 ? 'text-success fw-semibold' : 'text-muted'">
              {{ form.vin?.length ?? 0 }}/17
            </div>
            <div v-if="vinTouched && vinError" class="invalid-feedback d-block">
              {{ vinError }}
            </div>
          </div>

          <div class="mb-3">
            <label class="form-label">{{ t('vehicles.mileage') }}</label>
            <input v-model.number="form.mileage" type="number" class="form-control" min="0" />
          </div>
          <div class="mb-3">
            <label class="form-label">{{ t('vehicles.color') }}</label>
            <input v-model="form.color" class="form-control" placeholder="e.g. Silver" />
          </div>
          <div class="d-flex gap-2">
            <button type="submit" class="btn btn-primary" :disabled="loading || !vinIsValid">
              <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
              {{ t('common.save') }}
            </button>
            <router-link to="/vehicles" class="btn btn-outline-secondary">{{ t('common.cancel') }}</router-link>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { vehicleService } from '@/services/vehicleService'
import type { VehicleCreateDto } from '@/types'

const { t } = useI18n()
const router = useRouter()

const VIN_PATTERN = /^[A-HJ-NPR-Z0-9]{17}$/
const VIN_BANNED  = /[IOQ]/i

const form = ref<VehicleCreateDto>({
  make: '',
  model: '',
  year: new Date().getFullYear(),
  licensePlate: '',
  vin: '',
  mileage: undefined,
  color: ''
})
const error = ref('')
const loading = ref(false)
const vinTouched = ref(false)

const vinError = computed((): string => {
  const v = (form.value.vin ?? '').toUpperCase()
  if (VIN_BANNED.test(v)) return t('vehicles.vinInvalidChars')
  if (v.length !== 17) return t('vehicles.vinLength')
  if (!VIN_PATTERN.test(v)) return t('vehicles.vinInvalidChars')
  return ''
})

const vinIsValid = computed(() => vinError.value === '')

function onVinInput(event: Event) {
  const raw = (event.target as HTMLInputElement).value
  form.value.vin = raw.toUpperCase()
}

async function handleCreate() {
  vinTouched.value = true
  if (vinError.value) return
  error.value = ''
  loading.value = true
  try {
    await vehicleService.create(form.value)
    router.push('/vehicles')
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
}
</script>
