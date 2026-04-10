<script setup lang="ts">
import '../assets/app-shell.css'
import { ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useAuth } from '../composables/useAuth'

const { isAdmin, logout } = useAuth()
const route = useRoute()

const menuOpen = ref(false)

function closeMenu() {
  menuOpen.value = false
}
</script>

<template>
  <!-- Mobile top bar -->
  <header class="topbar">
    <button
      class="menu-toggle btn btn-ghost btn-icon"
      :class="{ active: menuOpen }"
      :aria-expanded="menuOpen"
      aria-label="Toggle navigation"
      @click="menuOpen = !menuOpen"
    >
      <span class="hamburger-icon">
        <span></span>
        <span></span>
        <span></span>
      </span>
    </button>

    <RouterLink to="/" class="topbar-logo" @click="closeMenu">
      <span class="logo-icon">◆</span>
      <span class="logo-text">ToroFit</span>
    </RouterLink>
  </header>

  <!-- Overlay (mobile) -->
  <div
    class="nav-overlay"
    :class="{ open: menuOpen }"
    aria-hidden="true"
    @click="closeMenu"
  ></div>

  <!-- Sidebar nav -->
  <nav class="sidebar" :class="{ open: menuOpen }" aria-label="Main navigation">
    <!-- Desktop logo -->
    <RouterLink to="/" class="sidebar-logo" @click="closeMenu">
      <span class="logo-icon">◆</span>
      <span class="logo-text">ToroFit</span>
    </RouterLink>

    <ul class="nav-links">
      <li>
        <RouterLink to="/" exact-active-class="nav-link-active" class="nav-link" @click="closeMenu">
          <svg class="nav-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path d="M10.707 2.293a1 1 0 00-1.414 0l-7 7A1 1 0 003 11h1v6a1 1 0 001 1h4v-4h2v4h4a1 1 0 001-1v-6h1a1 1 0 00.707-1.707l-7-7z"/>
          </svg>
          Home
        </RouterLink>
      </li>
      <li>
        <RouterLink to="/workouts" active-class="nav-link-active" class="nav-link" @click="closeMenu">
          <svg class="nav-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M6 3.75A2.75 2.75 0 018.75 1h2.5A2.75 2.75 0 0114 3.75v.443c.572.055 1.14.122 1.706.2C17.053 4.582 18 5.75 18 7.07v3.469c0 1.126-.694 2.191-1.83 2.54-1.952.599-4.024.921-6.17.921s-4.219-.322-6.17-.921C2.694 12.73 2 11.665 2 10.539V7.07c0-1.32.947-2.489 2.294-2.676A41.047 41.047 0 016 4.193V3.75zm6.5 0v.325a41.122 41.122 0 00-5 0V3.75c0-.69.56-1.25 1.25-1.25h2.5c.69 0 1.25.56 1.25 1.25zM10 10a1 1 0 00-1 1v.01a1 1 0 001 1h.01a1 1 0 001-1V11a1 1 0 00-1-1H10z" clip-rule="evenodd"/>
            <path d="M3 15.055v-.684c.278.097.562.186.851.268C5.793 15.175 7.86 15.5 10 15.5s4.207-.325 6.149-.861c.289-.082.573-.17.851-.268v.684c0 1.329-.956 2.512-2.318 2.692a41.202 41.202 0 01-8.364 0C3.956 17.567 3 16.384 3 15.055z"/>
          </svg>
          Workouts
        </RouterLink>
      </li>
      <li v-if="isAdmin()">
        <RouterLink to="/admin/exercises" active-class="nav-link-active" class="nav-link" @click="closeMenu">
          <svg class="nav-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path fill-rule="evenodd" d="M14.5 10a4.5 4.5 0 004.284-5.882c-.105-.324-.51-.391-.752-.15L15.34 6.66a.454.454 0 01-.493.11 3.01 3.01 0 01-1.618-1.616.455.455 0 01.11-.494l2.694-2.692c.24-.241.174-.647-.15-.752a4.5 4.5 0 00-5.873 4.575c.055.873-.128 1.808-.8 2.368l-7.23 6.024a2.724 2.724 0 103.837 3.837l6.024-7.23c.56-.672 1.495-.855 2.368-.8.096.007.193.01.291.01zM5 16a1 1 0 11-2 0 1 1 0 012 0z" clip-rule="evenodd"/>
          </svg>
          Admin
        </RouterLink>
      </li>
    </ul>

    <div class="sidebar-footer">
      <button class="nav-link logout-btn" @click="logout">
        <svg class="nav-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
          <path fill-rule="evenodd" d="M3 4.25A2.25 2.25 0 015.25 2h5.5A2.25 2.25 0 0113 4.25v2a.75.75 0 01-1.5 0v-2a.75.75 0 00-.75-.75h-5.5a.75.75 0 00-.75.75v11.5c0 .414.336.75.75.75h5.5a.75.75 0 00.75-.75v-2a.75.75 0 011.5 0v2A2.25 2.25 0 0110.75 18h-5.5A2.25 2.25 0 013 15.75V4.25z" clip-rule="evenodd"/>
          <path fill-rule="evenodd" d="M19 10a.75.75 0 00-.75-.75H8.704l1.048-1.07a.75.75 0 10-1.004-1.115l-2.5 2.5a.75.75 0 000 1.07l2.5 2.5a.75.75 0 101.004-1.114l-1.048-1.071h9.546A.75.75 0 0019 10z" clip-rule="evenodd"/>
        </svg>
        Sign out
      </button>
    </div>
  </nav>

  <!-- Main content area -->
  <main class="shell-content">
    <slot />
  </main>
</template>


