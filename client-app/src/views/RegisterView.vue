<template>
  <div class="row justify-content-center">
    <div class="col-md-5">
      <div class="card shadow-sm">
        <div class="card-body p-4">
          <h3 class="card-title text-center mb-4">{{ t('auth.register') }}</h3>

          <!-- Backend error(s) -->
          <div v-if="error" class="alert alert-danger">{{ error }}</div>

          <!-- Backend validation errors (field-level) -->
          <div v-if="validationErrors.length" class="alert alert-danger">
            <ul class="mb-0 ps-3">
              <li v-for="(msg, i) in validationErrors" :key="i">{{ msg }}</li>
            </ul>
          </div>

          <form @submit.prevent="handleRegister">
            <div class="mb-3">
              <label class="form-label">First Name *</label>
              <input
                v-model="form.firstname"
                type="text"
                class="form-control"
                required
                autocomplete="given-name"
                placeholder="e.g. John"
              />
            </div>

            <div class="mb-3">
              <label class="form-label">Last Name *</label>
              <input
                v-model="form.lastname"
                type="text"
                class="form-control"
                required
                autocomplete="family-name"
                placeholder="e.g. Smith"
              />
            </div>

            <div class="mb-3">
              <label class="form-label">{{ t('auth.email') }} *</label>
              <input
                v-model="form.email"
                type="email"
                class="form-control"
                required
                autocomplete="email"
              />
            </div>

            <div class="mb-3">
              <label class="form-label">{{ t('auth.password') }} *</label>
              <input
                v-model="form.password"
                type="password"
                class="form-control"
                required
                autocomplete="new-password"
                minlength="6"
                @input="validatePasswords"
              />
              <div class="form-text">Minimum 6 characters, must include uppercase, lowercase, digit, and special character.</div>
            </div>

            <div class="mb-3">
              <label class="form-label">Confirm Password *</label>
              <input
                v-model="confirmPassword"
                type="password"
                :class="['form-control', confirmPasswordError ? 'is-invalid' : (confirmPassword ? 'is-valid' : '')]"
                required
                autocomplete="new-password"
                minlength="6"
                @input="validatePasswords"
              />
              <div v-if="confirmPasswordError" class="invalid-feedback">
                {{ confirmPasswordError }}
              </div>
            </div>

            <button
              type="submit"
              class="btn btn-primary w-100"
              :disabled="loading || !!confirmPasswordError || !confirmPassword"
            >
              <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
              {{ t('auth.registerBtn') }}
            </button>
          </form>

          <hr />
          <p class="text-center mb-0">
            {{ t('auth.hasAccount') }}
            <router-link to="/login">{{ t('nav.login') }}</router-link>
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import type { RegisterInfo } from '@/types'

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()

const form = ref<RegisterInfo>({
  firstname: '',
  lastname: '',
  email: '',
  password: ''
})
const confirmPassword = ref('')
const confirmPasswordError = ref('')
const error = ref('')
const validationErrors = ref<string[]>([])
const loading = ref(false)

function validatePasswords() {
  if (confirmPassword.value && form.value.password !== confirmPassword.value) {
    confirmPasswordError.value = 'Passwords do not match'
  } else {
    confirmPasswordError.value = ''
  }
}

async function handleRegister() {
  // Client-side password match check
  if (form.value.password !== confirmPassword.value) {
    confirmPasswordError.value = 'Passwords do not match'
    return
  }

  error.value = ''
  validationErrors.value = []
  loading.value = true

  try {
    await authStore.register(form.value)
    router.push('/')
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
      error.value = t('auth.registerError')
    }
  } finally {
    loading.value = false
  }
}
</script>
