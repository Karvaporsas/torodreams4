<script setup lang="ts">
import HelloWorld from '../components/HelloWorld.vue'
import { useAuth } from '../composables/useAuth'

const { logout, isAdmin } = useAuth()
</script>

<template>
  <main>
    <div class="toolbar">
      <router-link to="/workouts" class="nav-link">Workouts</router-link>
      <router-link v-if="isAdmin()" to="/admin/exercises" class="nav-link">Admin</router-link>
      <button @click="logout">Logout</button>
    </div>
    <HelloWorld />
  </main>
</template>

<style scoped>
.toolbar {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem;
}

.nav-link {
  padding: 0.4rem 1rem;
  background: #2c3e50;
  color: white;
  border-radius: 4px;
  text-decoration: none;
  font-size: 0.9rem;
}

.nav-link:hover {
  background: #3d5166;
}

button {
  padding: 0.4rem 1rem;
  background: #42b883;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
</style>
