<script setup lang="ts">
import { ref, onMounted } from 'vue'

const message = ref<string | null>(null)
const loading = ref(true)

onMounted(async () => {
  const res = await fetch('http://localhost:5000/api/hello')
  const data = await res.json()
  message.value = data.message
  loading.value = false
})
</script>

<template>
  <div class="greetings">
    <p v-if="loading">Loading...</p>
    <h1 v-else class="green">{{ message }}</h1>
  </div>
</template>
