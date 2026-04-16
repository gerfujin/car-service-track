<template>
  <div class="row justify-content-center">
    <div class="col-md-5">
      <div class="card shadow-sm">
        <div class="card-body p-4">
          <h3 class="card-title text-center mb-4">{{ t('auth.login') }}</h3>

          <div v-if="error" class="alert alert-danger">{{ error }}</div>

          <form @submit.prevent="handleLogin">
            <div class="mb-3">
              <label class="form-label">{{ t('auth.email') }}</label>
              <input v-model="form.email" type="email" class="form-control" required autocomplete="email" />
            </div>
            <div class="mb-3">
              <label class="form-label">{{ t('auth.password') }}</label>
              <input v-model="form.password" type="password" class="form-control" required autocomplete="current-password" />
            </div>
            <button type="submit" class="btn btn-primary w-100" :disabled="loading">
              <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
              {{ t('auth.loginBtn') }}
            </button>
          </form>

          <hr />
          <p class="text-center mb-0">
            {{ t('auth.noAccount') }}
            <router-link to="/register">{{ t('nav.register') }}</router-link>
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const form = ref({ email: '', password: '' })
const error = ref('')
const loading = ref(false)

async function handleLogin() {
  error.value = ''
  loading.value = true
  try {
    await authStore.login(form.value)
    const redirect = route.query.redirect as string || '/'
    router.push(redirect)
  } catch (e: unknown) {
    error.value = t('auth.loginError')
  } finally {
    loading.value = false
  }
}
</script>
