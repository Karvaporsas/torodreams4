<script setup lang="ts">
import '../assets/welcome-item.css'
</script>

<template>
  <div class="welcome-item">
    <i>
      <slot name="icon"></slot>
    </i>
    <div class="welcome-item-details">
      <h3>
        <slot name="heading"></slot>
      </h3>
      <slot></slot>
    </div>
  </div>
</template>

