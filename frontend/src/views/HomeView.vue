<script setup lang="ts">
import HelloWorld from '../components/HelloWorld.vue'
import { useAuth } from '../composables/useAuth'

const { logout } = useAuth()
</script>

<template>
  <main>
    <div class="toolbar">
      <button @click="logout">Logout</button>
    </div>
    <HelloWorld />
  </main>
</template>

<style scoped>
.toolbar {
  display: flex;
  justify-content: flex-end;
  padding: 1rem;
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
