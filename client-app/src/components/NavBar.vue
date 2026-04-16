<template>
  <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
    <div class="container">
      <router-link class="navbar-brand fw-bold" to="/">
        🔧 CarServiceTrack
      </router-link>
      <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
        <span class="navbar-toggler-icon"></span>
      </button>
      <div class="collapse navbar-collapse" id="navbarNav">
        <!-- Left nav links (authenticated) -->
        <ul class="navbar-nav me-auto">
          <li class="nav-item">
            <router-link class="nav-link" to="/">{{ t('nav.home') }}</router-link>
          </li>
          <template v-if="authStore.isAuthenticated">
            <!-- Vehicles: all roles -->
            <li class="nav-item">
              <router-link class="nav-link" to="/vehicles">🚗 {{ t('nav.vehicles') }}</router-link>
            </li>
            <!-- Orders: all roles -->
            <li class="nav-item">
              <router-link class="nav-link" to="/orders">📋 {{ t('nav.orders') }}</router-link>
            </li>
            <!-- Payments: all roles -->
            <li class="nav-item">
              <router-link class="nav-link" to="/payments">💰 {{ t('nav.payments') }}</router-link>
            </li>
            <!-- Role badge -->
            <li class="nav-item d-flex align-items-center ms-2">
              <span v-if="authStore.isAdmin" class="badge bg-warning text-dark">⚙️ Admin</span>
              <span v-else-if="authStore.isMechanic" class="badge bg-info text-dark">👨‍🔧 Mechanic</span>
              <span v-else-if="authStore.isClient" class="badge bg-secondary">👤 Client</span>
            </li>
          </template>
        </ul>

        <!-- Right nav: auth area -->
        <ul class="navbar-nav ms-auto align-items-center">
          <!-- Language switcher -->
          <li class="nav-item me-2">
            <select class="form-select form-select-sm bg-dark text-white border-secondary" v-model="currentLocale" @change="changeLocale">
              <option value="en">EN</option>
              <option value="et">ET</option>
            </select>
          </li>

          <!-- Guest: show Login + Register -->
          <template v-if="!authStore.isAuthenticated">
            <li class="nav-item">
              <router-link class="nav-link" to="/login">{{ t('nav.login') }}</router-link>
            </li>
            <li class="nav-item">
              <router-link class="btn btn-outline-light btn-sm ms-2" to="/register">{{ t('nav.register') }}</router-link>
            </li>
          </template>

          <!-- Authenticated: show user email + logout -->
          <template v-else>
            <li class="nav-item">
              <span class="navbar-text text-light me-3">
                👤 {{ authStore.displayName }}
              </span>
            </li>
            <li class="nav-item">
              <button class="btn btn-outline-danger btn-sm" @click="handleLogout">
                {{ t('nav.logout') }}
              </button>
            </li>
          </template>
        </ul>
      </div>
    </div>
  </nav>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const { t, locale } = useI18n()
const router = useRouter()
const authStore = useAuthStore()

const currentLocale = ref(locale.value)

function changeLocale() {
  locale.value = currentLocale.value
  localStorage.setItem('cst_locale', currentLocale.value)
}

async function handleLogout() {
  await authStore.logout()
  router.push('/login')
}
</script>
