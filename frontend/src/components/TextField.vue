<script setup lang="ts">
// A labeled text input with an inline validation message and an optional hint, styled per the
// kit. Used for the auth fields (email/password) and reusable by later forms. Two-way bound via
// v-model; the error border and aria wiring turn on when `error` is set.
defineProps<{
  id: string
  label: string
  modelValue: string
  type?: string
  error?: string | null
  hint?: string | null
  placeholder?: string
  autocomplete?: string
}>()

defineEmits<{
  'update:modelValue': [value: string]
}>()
</script>

<template>
  <div class="field">
    <label class="field__label" :for="id">{{ label }}</label>
    <input
      :id="id"
      class="field__input"
      :class="{ 'field__input--error': error }"
      :type="type ?? 'text'"
      :value="modelValue"
      :placeholder="placeholder"
      :autocomplete="autocomplete"
      :aria-invalid="error ? 'true' : undefined"
      :aria-describedby="error ? `${id}-error` : hint ? `${id}-hint` : undefined"
      @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
    <p v-if="error" :id="`${id}-error`" class="field__error">
      <span class="field__error-icon" aria-hidden="true">!</span>{{ error }}
    </p>
    <p v-else-if="hint" :id="`${id}-hint`" class="field__hint">{{ hint }}</p>
  </div>
</template>

<style scoped>
.field {
  margin-bottom: var(--sp-4);
}

.field__label {
  display: block;
  margin-bottom: 6px;
  font-size: 13px;
  font-weight: 600;
}

.field__input {
  width: 100%;
  padding: 10px 12px;
  background: #fff;
  border: 1px solid var(--c-border-strong);
  border-radius: var(--r-sm);
  font-size: 15px;
  color: var(--c-text);
}

/* The kit's focus treatment: primary border + soft ring. This is the visible focus indicator,
   so the default outline is intentionally removed (the ring is fully accessible). */
.field__input:focus {
  outline: none;
  border-color: var(--c-primary);
  box-shadow: 0 0 0 3px var(--c-primary-weak);
}

.field__input--error {
  border-width: 1.5px;
  border-color: var(--c-overdue);
}

.field__input--error:focus {
  box-shadow: 0 0 0 3px var(--c-overdue-bg);
}

.field__error {
  display: flex;
  align-items: center;
  gap: 5px;
  margin-top: 6px;
  color: var(--c-overdue);
  font-size: 12px;
  font-weight: 500;
}

.field__error-icon {
  font-weight: 700;
}

.field__hint {
  margin-top: 6px;
  color: var(--c-text-subtle);
  font-size: 12px;
}
</style>
