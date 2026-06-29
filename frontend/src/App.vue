<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import AppHeader from '@/components/AppHeader.vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

// Login/register are full-page centered cards with no app chrome (matches the kit).
const showHeader = computed(() => route.meta.public !== true)

async function logout(): Promise<void> {
  auth.logout()
  await router.push({ name: 'login' })
}
</script>

<template>
  <div class="app">
    <AppHeader v-if="showHeader">
      <template #actions>
        <span class="app__email">{{ auth.email }}</span>
        <span class="app__divider" aria-hidden="true"></span>
        <button class="app__logout" type="button" @click="logout">Log out</button>
      </template>
    </AppHeader>
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

.app__email {
  color: var(--c-text-muted);
  font-size: 14px;
}

.app__divider {
  width: 1px;
  height: 20px;
  background: var(--c-border);
}

.app__logout {
  padding: 8px 4px;
  background: transparent;
  border: none;
  color: var(--c-text-muted);
  font-size: 14px;
  font-weight: 600;
}

.app__logout:hover {
  color: var(--c-text);
}

/* The kit's mobile header drops the email, keeping just the brand and Log out. */
@media (max-width: 640px) {
  .app__email,
  .app__divider {
    display: none;
  }
}
</style>
