<script setup lang="ts">
import { ref } from 'vue'

// The brand mark. Renders /logo.png (drop a file in frontend/public/) and falls back to the "T"
// lettermark until that file exists, so the app never shows a broken image. `size` is the square
// dimension in px; the parent decides placement and spacing.
withDefaults(defineProps<{ size?: number }>(), { size: 30 })

// Bound (not a static `src`) so Vite leaves it as a runtime public-asset path instead of trying
// to resolve /logo.png at build time. Drop the actual file at frontend/public/logo.png.
const logoSrc = '/logo.png'
const failed = ref(false)
</script>

<template>
  <span class="brand-logo" :style="{ width: `${size}px`, height: `${size}px` }">
    <img v-if="!failed" class="brand-logo__img" :src="logoSrc" alt="" @error="failed = true" />
    <span
      v-else
      class="brand-logo__fallback"
      :style="{ fontSize: `${Math.round(size * 0.5)}px` }"
      aria-hidden="true"
      >T</span
    >
  </span>
</template>

<style scoped>
.brand-logo {
  display: inline-flex;
  flex: none;
  align-items: center;
  justify-content: center;
}

.brand-logo__img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.brand-logo__fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  border-radius: 22%;
  background: var(--c-primary);
  color: #fff;
  font-weight: 700;
}
</style>
