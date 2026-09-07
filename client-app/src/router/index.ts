import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@/views/HomeView.vue')
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { guestOnly: true }
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/views/RegisterView.vue'),
      meta: { guestOnly: true }
    },
    // Vehicles — all authenticated roles can view
    {
      path: '/vehicles',
      name: 'vehicles',
      component: () => import('@/views/vehicles/VehiclesView.vue'),
      meta: { requiresAuth: true }
    },
    // Vehicle create — admin and client only
    {
      path: '/vehicles/create',
      name: 'vehicle-create',
      component: () => import('@/views/vehicles/VehicleCreateView.vue'),
      meta: { requiresAuth: true, roles: ['admin', 'client'] }
    },
    {
      path: '/vehicles/:id',
      name: 'vehicle-detail',
      component: () => import('@/views/vehicles/VehicleDetailView.vue'),
      meta: { requiresAuth: true }
    },
    // Vehicle edit — admin and client only
    {
      path: '/vehicles/:id/edit',
      name: 'vehicle-edit',
      component: () => import('@/views/vehicles/VehicleEditView.vue'),
      meta: { requiresAuth: true, roles: ['admin', 'client'] }
    },
    // Service Orders — all authenticated roles
    {
      path: '/orders',
      name: 'orders',
      component: () => import('@/views/orders/OrdersView.vue'),
      meta: { requiresAuth: true }
    },
    // Order create — admin and client only
    {
      path: '/orders/create',
      name: 'order-create',
      component: () => import('@/views/orders/OrderCreateView.vue'),
      meta: { requiresAuth: true, roles: ['admin', 'client'] }
    },
    {
      path: '/orders/:id',
      name: 'order-detail',
      component: () => import('@/views/orders/OrderDetailView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/orders/:id/progress',
      name: 'order-progress',
      component: () => import('@/views/orders/OrderProgressView.vue'),
      meta: { requiresAuth: true }
    },
    // Order status update — admin and mechanic only
    {
      path: '/orders/:id/update-status',
      name: 'order-update-status',
      component: () => import('@/views/orders/OrderUpdateStatusView.vue'),
      meta: { requiresAuth: true, roles: ['admin', 'mechanic'] }
    },
    // Payments — all authenticated roles
    {
      path: '/services',
      name: 'services',
      component: () => import('@/views/services/ServicesView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/payments',
      name: 'payments',
      component: () => import('@/views/payments/PaymentsView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/payments/:id',
      name: 'payment-detail',
      component: () => import('@/views/payments/PaymentDetailView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin/spare-parts',
      name: 'admin-spare-parts',
      component: () => import('@/views/AdminSparePartsView.vue'),
      meta: { requiresAuth: true, roles: ['admin'] }
    },
    // Profile — the authenticated user's own profile
    {
      path: '/profile',
      name: 'profile',
      component: () => import('@/views/ProfileView.vue'),
      meta: { requiresAuth: true }
    },
    // Catch-all
    {
      path: '/:pathMatch(.*)*',
      redirect: '/'
    }
  ]
})

// Route guards
router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next({ name: 'login', query: { redirect: to.fullPath } })
    return
  }

  if (to.meta.guestOnly && authStore.isAuthenticated) {
    next({ name: 'home' })
    return
  }

  // Role-based route guard
  const requiredRoles = to.meta.roles as string[] | undefined
  if (requiredRoles && requiredRoles.length > 0 && authStore.isAuthenticated) {
    const userRoles = authStore.userRoles
    const hasRole = requiredRoles.some(r => userRoles.includes(r.toLowerCase()))
    if (!hasRole) {
      // Redirect to home with access denied
      next({ name: 'home' })
      return
    }
  }

  next()
})

export default router
