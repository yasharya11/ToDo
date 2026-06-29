<script setup lang="ts">
import AppHeader from '@/components/AppHeader.vue'
</script>

<template>
  <div class="app">
    <AppHeader />
    <main class="app__main">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.app {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: var(--c-bg);
}

.app__main {
  flex: 1 1 auto;
}
</style>
