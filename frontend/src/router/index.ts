import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import LoginView from '../views/LoginView.vue'
import AdminExercisesView from '../views/AdminExercisesView.vue'
import { parseTokenPayload } from '../composables/useAuth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomeView },
    { path: '/login', component: LoginView },
    { path: '/admin/exercises', component: AdminExercisesView },
  ],
})

router.beforeEach((to) => {
  const token = localStorage.getItem('auth_token')
  if (!token && to.path !== '/login') {
    return '/login'
  }

  if (to.path.startsWith('/admin')) {
    if (!token) return '/login'
    const payload = parseTokenPayload(token)
    const roles = payload?.['role']
    const isAdmin = Array.isArray(roles) ? roles.includes('Admin') : roles === 'Admin'
    if (!isAdmin) return '/'
  }
})

export default router
