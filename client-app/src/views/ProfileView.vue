<template>
  <div class="row justify-content-center">
    <div class="col-md-6">
      <h2 class="mb-4">👤 {{ t('profile.title') }}</h2>

      <div v-if="message" class="alert alert-success">{{ message }}</div>
      <div v-if="error" class="alert alert-danger">{{ error }}</div>
      <div v-if="validationErrors.length" class="alert alert-danger">
        <ul class="mb-0 ps-3">
          <li v-for="(msg, i) in validationErrors" :key="i">{{ msg }}</li>
        </ul>
      </div>

      <div v-if="profileStore.loading" class="text-center py-5">
        <div class="spinner-border"></div>
      </div>

      <div v-else class="card shadow-sm">
        <div class="card-body">
          <form @submit.prevent="handleSave">
            <div class="mb-3">
              <label class="form-label">{{ t('profile.email') }}</label>
              <input :value="profileStore.profile?.email" type="email" class="form-control" disabled />
            </div>
            <div class="mb-3">
              <label class="form-label">{{ t('profile.firstName') }} *</label>
              <input v-model="form.firstName" class="form-control" required autocomplete="given-name" />
            </div>
            <div class="mb-3">
              <label class="form-label">{{ t('profile.lastName') }} *</label>
              <input v-model="form.lastName" class="form-control" required autocomplete="family-name" />
            </div>
            <div class="mb-3">
              <label class="form-label">{{ t('profile.address') }}</label>
              <input v-model="form.address" class="form-control" autocomplete="street-address" />
            </div>
            <div class="mb-3">
              <label class="form-label">{{ t('profile.phone') }}</label>
              <input v-model="form.phone" class="form-control" autocomplete="tel" />
            </div>
            <button type="submit" class="btn btn-primary" :disabled="profileStore.saving">
              <span v-if="profileStore.saving" class="spinner-border spinner-border-sm me-2"></span>
              {{ t('profile.save') }}
            </button>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useProfileStore } from '@/stores/profile'
import type { ProfileUpdateDto } from '@/types'

const { t } = useI18n()
const profileStore = useProfileStore()

const form = ref<ProfileUpdateDto>({ firstName: '', lastName: '', address: '', phone: '' })
const error = ref('')
const message = ref('')
const validationErrors = ref<string[]>([])

onMounted(async () => {
  try {
    await profileStore.loadProfile()
    const p = profileStore.profile
    if (p) {
      form.value = { firstName: p.firstName, lastName: p.lastName, address: p.address, phone: p.phone }
    }
  } catch {
    error.value = t('profile.errorLoad')
  }
})

async function handleSave() {
  error.value = ''
  message.value = ''
  validationErrors.value = []

  try {
    await profileStore.saveProfile(form.value)
    message.value = t('profile.updateSuccess')
  } catch (e: unknown) {
    const err = e as {
      response?: {
        data?: {
          error?: string
          errors?: Record<string, string[]>
        }
      }
    }

    const responseData = err?.response?.data

    if (responseData?.errors) {
      // Parse field-level validation errors from ASP.NET Core
      const msgs: string[] = []
      for (const [field, messages] of Object.entries(responseData.errors)) {
        for (const msg of messages) {
          msgs.push(`${field}: ${msg}`)
        }
      }
      validationErrors.value = msgs
    } else if (responseData?.error) {
      error.value = responseData.error
    } else {
      error.value = t('profile.errorSave')
    }
  }
}
</script>
