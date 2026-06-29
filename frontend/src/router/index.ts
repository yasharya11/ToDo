import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/views/HomeView.vue'
import { useAuthStore } from '@/stores/auth'

// Strongly-typed route meta so the guard and App shell read these flags safely.
declare module 'vue-router' {
  interface RouteMeta {
    /** Route requires a signed-in user; the guard redirects to /login otherwise. */
    requiresAuth?: boolean
    /** Public auth screen (login/register): no app chrome, and off-limits once signed in. */
    public?: boolean
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
      meta: { requiresAuth: true },
    },
    {
      path: '/login',
      name: 'login',
      // Lazy-loaded: the auth screens aren't needed once a returning user is signed in.
      component: () => import('@/views/LoginView.vue'),
      meta: { public: true },
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/views/RegisterView.vue'),
      meta: { public: true },
    },
  ],
})

// Auth guard: gate protected routes, and keep signed-in users out of login/register.
router.beforeEach((to) => {
  const auth = useAuthStore()

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.public && auth.isAuthenticated) {
    return { name: 'home' }
  }
})

export default router
