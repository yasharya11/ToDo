<script setup lang="ts">
import { onBeforeUnmount, onMounted } from 'vue'

// A modal shell: a scrim over the page with a centered dialog, teleported to <body> so it escapes
// any parent overflow/stacking context. Closes on Escape or a scrim click and locks background
// scroll while open. Labelling + initial focus are the caller's job (pass `labelledby`).
withDefaults(defineProps<{ labelledby: string; maxWidth?: number }>(), { maxWidth: 480 })
const emit = defineEmits<{ close: [] }>()

function onKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') {
    emit('close')
  }
}

onMounted(() => {
  document.addEventListener('keydown', onKeydown)
  document.body.style.overflow = 'hidden'
})

onBeforeUnmount(() => {
  document.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = ''
})
</script>

<template>
  <Teleport to="body">
    <div class="scrim" @click.self="emit('close')">
      <div
        class="dialog"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="labelledby"
        :style="{ maxWidth: `${maxWidth}px` }"
      >
        <slot />
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.scrim {
  position: fixed;
  inset: 0;
  z-index: 100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--sp-5);
  background: rgba(20, 28, 36, 0.45);
}

.dialog {
  width: 100%;
  max-height: calc(100vh - 2 * var(--sp-5));
  overflow-y: auto;
  background: var(--c-surface);
  border-radius: var(--r-lg);
  box-shadow: var(--shadow-md);
}
</style>
