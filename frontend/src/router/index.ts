import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import LoginView from '../views/LoginView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomeView },
    { path: '/login', component: LoginView },
  ],
})

router.beforeEach((to) => {
  const token = localStorage.getItem('auth_token')
  if (!token && to.path !== '/login') {
    return '/login'
  }
})

export default router
