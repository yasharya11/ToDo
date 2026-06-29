<script setup lang="ts">
// Full-width primary submit button with a built-in loading state (spinner + alternate label,
// disabled while in flight), matching the kit's "Saving…" treatment. Reused by the auth and
// task forms.
defineProps<{
  loading: boolean
  label: string
  loadingLabel: string
}>()
</script>

<template>
  <button class="submit" type="submit" :disabled="loading">
    <span v-if="loading" class="submit__spinner" aria-hidden="true"></span>
    {{ loading ? loadingLabel : label }}
  </button>
</template>

<style scoped>
.submit {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 9px;
  width: 100%;
  padding: 11px;
  background: var(--c-primary);
  border: none;
  border-radius: var(--r-sm);
  color: #fff;
  font-size: 15px;
  font-weight: 600;
}

.submit:hover:not(:disabled) {
  background: var(--c-primary-hover);
}

.submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.submit__spinner {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.45);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}
</style>
