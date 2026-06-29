<script setup lang="ts">
// The brand mark — the 📎 paperclip emoji, the same glyph used for the favicon. Rendered as text
// so it scales crisply with `size` (px) and needs no asset. Decorative: the adjacent "Tasker"
// wordmark / page title carries the accessible name.
withDefaults(defineProps<{ size?: number }>(), { size: 30 })
</script>

<template>
  <span class="brand-logo" :style="{ fontSize: `${size}px` }" aria-hidden="true">📎</span>
</template>

<style scoped>
.brand-logo {
  display: inline-flex;
  flex: none;
  align-items: center;
  justify-content: center;
  line-height: 1;
}
</style>
