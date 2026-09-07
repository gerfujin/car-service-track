<template>
  <nav class="navbar navbar-expand-xl navbar-dark bg-dark app-navbar">
    <div class="container-fluid px-3 px-xl-4 navbar-inner">
      <router-link class="navbar-brand fw-bold" to="/">
        🔧 CarServiceTrack
      </router-link>
      <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
        <span class="navbar-toggler-icon"></span>
      </button>
      <div class="collapse navbar-collapse" id="navbarNav">
        <!-- Left nav links (authenticated) -->
        <ul class="navbar-nav me-auto align-items-xl-center app-nav-left">
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
            <li class="nav-item">
              <router-link class="nav-link" to="/services">🔩 {{ t('nav.services') }}</router-link>
            </li>
            <!-- Payments: all roles -->
            <li class="nav-item">
              <router-link class="nav-link" to="/payments">💰 {{ t('nav.payments') }}</router-link>
            </li>
            <li v-if="authStore.isAdmin" class="nav-item">
              <router-link class="nav-link" to="/admin/spare-parts">🧩 {{ t('nav.spareParts') }}</router-link>
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
        <ul class="navbar-nav ms-auto align-items-xl-center app-nav-right">
          <!-- Language switcher -->
            <li class="nav-item me-xl-1">
            <select class="form-select form-select-sm bg-dark text-white border-secondary language-select" v-model="currentLocale" @change="changeLocale">
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

          <!-- Authenticated: show user name (link to profile) + logout -->
          <template v-else>
            <li class="nav-item">
              <router-link class="nav-link text-light me-xl-2 user-name" to="/profile">
                👤 {{ authStore.displayName }}
              </router-link>
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

<style scoped>
.app-navbar {
  padding-top: 0.6rem;
  padding-bottom: 0.6rem;
}

.app-navbar .navbar-inner {
  width: 100%;
  max-width: 1600px;
  margin: 0 auto;
}

.app-navbar .navbar-nav {
  flex-wrap: nowrap;
}

.app-navbar .nav-link,
.app-navbar .navbar-brand,
.app-navbar .btn,
.app-navbar .user-name,
.app-navbar .language-select {
  white-space: nowrap;
}

.app-navbar .navbar-brand {
  font-size: 1.15rem;
  margin-right: 1rem;
}

.app-navbar .nav-link {
  font-size: 1.02rem;
  padding-left: 0.5rem;
  padding-right: 0.5rem;
}

.app-navbar .app-nav-left,
.app-navbar .app-nav-right {
  gap: 0.25rem;
}

.app-navbar .language-select {
  width: auto;
  min-width: 64px;
}

@media (max-width: 1199.98px) {
  .app-navbar .navbar-nav {
    flex-wrap: wrap;
  }

  .app-navbar .app-nav-left,
  .app-navbar .app-nav-right {
    gap: 0;
  }

  .app-navbar .nav-link {
    font-size: 1rem;
  }
}
</style>
