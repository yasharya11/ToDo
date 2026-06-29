<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { apiUrl } from './api/client'

// The smoke test is always in exactly one of these three states.
type HealthState = 'loading' | 'ok' | 'error'

const state = ref<HealthState>('loading')
const detail = ref<string>('')

async function checkHealth(): Promise<void> {
  state.value = 'loading'
  detail.value = ''
  try {
    const response = await fetch(apiUrl('/api/health'))
    if (!response.ok) {
      throw new Error(`API responded ${response.status} ${response.statusText}`)
    }
    const body = (await response.json()) as { status: string }
    state.value = 'ok'
    detail.value = body.status
  } catch (error) {
    // A network failure (API down) or a non-2xx response both land here,
    // so the user always sees something — never a silent console-only error.
    state.value = 'error'
    detail.value = error instanceof Error ? error.message : 'Unknown error'
  }
}

// Run the check once, after the component is mounted on the page.
onMounted(checkHealth)
</script>

<template>
  <main class="health">
    <h1>ToDo</h1>

    <p v-if="state === 'loading'" class="status">Checking the API…</p>

    <p v-else-if="state === 'ok'" class="status status--ok">
      ✓ API reachable — health: <strong>{{ detail }}</strong>
    </p>

    <div v-else class="status status--error">
      <p>✕ Can’t reach the API.</p>
      <p class="detail">{{ detail }}</p>
      <button type="button" @click="checkHealth">Retry</button>
    </div>
  </main>
</template>

<style scoped>
.health {
  max-width: 32rem;
  margin: 4rem auto;
  padding: 0 1rem;
  font-family: system-ui, sans-serif;
  text-align: center;
}
.status {
  font-size: 1.1rem;
}
.status--ok {
  color: #15803d;
}
.status--error {
  color: #b91c1c;
}
.detail {
  font-size: 0.9rem;
  opacity: 0.75;
}
button {
  margin-top: 0.75rem;
  padding: 0.4rem 1rem;
  font: inherit;
  cursor: pointer;
}
</style>
