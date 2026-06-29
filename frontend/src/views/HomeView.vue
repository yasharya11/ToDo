<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { apiUrl } from '@/api/client'

// Transitional landing view for Phase 6. It renders inside the new app shell to
// demonstrate the tokens, and it keeps the Phase 1 frontend↔backend smoke test
// alive via the API health probe below. The task list (#22) replaces this view.
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
    // Network failure or a non-2xx both surface here — never a silent console error.
    state.value = 'error'
    detail.value = error instanceof Error ? error.message : 'Unknown error'
  }
}

onMounted(checkHealth)
</script>

<template>
  <section class="home">
    <div class="home__card">
      <h1 class="home__title">Foundation ready</h1>
      <p class="home__lead">
        Design tokens and the app shell are in place. Authentication, the task list, and the task
        forms are built on top of this foundation.
      </p>

      <div class="home__status" :class="`home__status--${state}`" role="status" aria-live="polite">
        <span v-if="state === 'loading'">Checking the API…</span>
        <span v-else-if="state === 'ok'">
          API reachable — health: <strong>{{ detail }}</strong>
        </span>
        <template v-else>
          <span>Can’t reach the API. {{ detail }}</span>
          <button type="button" class="home__retry" @click="checkHealth">Retry</button>
        </template>
      </div>
    </div>
  </section>
</template>

<style scoped>
.home {
  padding: var(--sp-6) var(--sp-5) var(--sp-7);
}

.home__card {
  max-width: 600px;
  margin: 0 auto;
  padding: var(--sp-6);
  background: var(--c-surface);
  border: 1px solid var(--c-border);
  border-radius: var(--r-lg);
  box-shadow: var(--shadow-sm);
}

.home__title {
  font-size: var(--fs-lg);
}

.home__lead {
  margin-top: var(--sp-3);
  color: var(--c-text-muted);
}

.home__status {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--sp-3);
  margin-top: var(--sp-5);
  padding: var(--sp-3) var(--sp-4);
  border-radius: var(--r-sm);
  font-size: var(--fs-sm);
  font-weight: 500;
}

.home__status--loading {
  color: var(--c-text-muted);
  background: var(--c-surface-2);
}

.home__status--ok {
  color: var(--c-completed);
  background: var(--c-completed-bg);
}

.home__status--error {
  color: var(--c-overdue);
  background: var(--c-overdue-bg);
}

.home__retry {
  margin-left: auto;
  padding: var(--sp-1) var(--sp-3);
  background: var(--c-surface);
  border: 1px solid var(--c-border-strong);
  border-radius: var(--r-sm);
  color: var(--c-text);
  font-size: var(--fs-sm);
  font-weight: 600;
}

.home__retry:hover {
  background: var(--c-surface-2);
}
</style>
