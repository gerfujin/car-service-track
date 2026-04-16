<template>
  <div>
    <div class="py-5 text-center">
      <h1 class="display-5 fw-bold">🔧 CarServiceTrack</h1>
      <p class="lead text-muted">Car Service Transparency System</p>
    </div>

    <div v-if="!authStore.isAuthenticated" class="row justify-content-center">
      <div class="col-md-8">
        <div class="card shadow-sm">
          <div class="card-body text-center py-5">
            <h3>Welcome to CarServiceTrack</h3>
            <p class="text-muted">Track your vehicle service history, orders, and payments in one place.</p>
            <div class="d-flex gap-3 justify-content-center mt-4">
              <router-link to="/login" class="btn btn-primary btn-lg">Login</router-link>
              <router-link to="/register" class="btn btn-outline-primary btn-lg">Register</router-link>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div v-else class="row g-4">
      <div class="col-md-4">
        <div class="card shadow-sm h-100">
          <div class="card-body text-center">
            <div class="display-4 mb-3">🚗</div>
            <h5>My Vehicles</h5>
            <p class="text-muted">Manage your registered vehicles</p>
            <router-link to="/vehicles" class="btn btn-primary">View Vehicles</router-link>
          </div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="card shadow-sm h-100">
          <div class="card-body text-center">
            <div class="display-4 mb-3">📋</div>
            <h5>Service Orders</h5>
            <p class="text-muted">Track your service orders and progress</p>
            <router-link to="/orders" class="btn btn-primary">View Orders</router-link>
          </div>
        </div>
      </div>
      <div class="col-md-4">
        <div class="card shadow-sm h-100">
          <div class="card-body text-center">
            <div class="display-4 mb-3">💰</div>
            <h5>Payments</h5>
            <p class="text-muted">View your payment history</p>
            <router-link to="/payments" class="btn btn-primary">View Payments</router-link>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
const authStore = useAuthStore()
</script>
