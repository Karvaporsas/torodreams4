<script setup lang="ts">
import '../assets/login.css'
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const router = useRouter()

async function handleLogin() {
  error.value = ''
  loading.value = true
  try {
    const res = await fetch('http://localhost:5000/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: username.value, password: password.value }),
    })
    if (!res.ok) {
      error.value = 'Invalid username or password.'
      return
    }
    const data = await res.json()
    localStorage.setItem('auth_token', data.token)
    router.push('/')
  } catch {
    error.value = 'Could not connect to server.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <div class="login-container">
      <div class="login-logo">
        <span class="login-logo-icon">◆</span>
        <span class="login-logo-text">ToroFit</span>
      </div>
      <div class="login-card card">
        <h1>Sign in</h1>
        <form class="login-form" @submit.prevent="handleLogin">
          <div class="form-group">
            <label class="form-label" for="username">Username</label>
            <input id="username" v-model="username" type="text" class="input" autocomplete="username" required />
          </div>
          <div class="form-group">
            <label class="form-label" for="password">Password</label>
            <input id="password" v-model="password" type="password" class="input" autocomplete="current-password" required />
          </div>
          <p v-if="error" class="login-error">{{ error }}</p>
          <button type="submit" class="btn btn-primary login-submit" :disabled="loading">
            <span v-if="loading" class="spinner"></span>
            {{ loading ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>
      </div>
    </div>
  </div>
</template>


