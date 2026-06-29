<script setup lang="ts">
// Shared chrome for the login and register screens: the centered page, the brand mark, a
// title/subtitle, an optional server-error banner, the form (default slot), and a footer link
// (footer slot). Keeping it here means both auth views stay focused on their fields and logic.
import BrandLogo from '@/components/BrandLogo.vue'

defineProps<{
  title: string
  subtitle: string
  error?: string | null
}>()
</script>

<template>
  <div class="auth-page">
    <div class="auth-card">
      <div class="auth-card__head">
        <BrandLogo :size="38" class="auth-card__logo" />
        <h1 class="auth-card__title">{{ title }}</h1>
        <p class="auth-card__subtitle">{{ subtitle }}</p>
      </div>

      <div v-if="error" class="auth-card__error" role="alert">
        <span class="auth-card__error-icon" aria-hidden="true">!</span>
        <span>{{ error }}</span>
      </div>

      <slot />

      <p class="auth-card__footer">
        <slot name="footer" />
      </p>
    </div>
  </div>
</template>

<style scoped>
.auth-page {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  padding: var(--sp-5);
}

.auth-card {
  width: 100%;
  max-width: 420px;
  padding: var(--sp-6);
  background: var(--c-surface);
  border: 1px solid var(--c-border);
  border-radius: var(--r-lg);
  box-shadow: var(--shadow-sm);
}

.auth-card__head {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: var(--sp-5);
}

.auth-card__logo {
  margin-bottom: var(--sp-3);
}

.auth-card__title {
  font-size: 22px;
  font-weight: 700;
  letter-spacing: -0.01em;
}

.auth-card__subtitle {
  margin-top: 6px;
  color: var(--c-text-muted);
  font-size: 14px;
}

.auth-card__error {
  display: flex;
  gap: 10px;
  align-items: flex-start;
  margin-bottom: var(--sp-4);
  padding: 11px 12px;
  /* The banner border has no token in the kit; keep the literal tint it specifies. */
  background: var(--c-overdue-bg);
  border: 1px solid #f3c9c4;
  border-radius: var(--r-sm);
  color: var(--c-overdue);
  font-size: 13px;
  font-weight: 500;
}

.auth-card__error-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  min-width: 18px;
  margin-top: 1px;
  border-radius: 50%;
  background: var(--c-overdue);
  color: #fff;
  font-size: 12px;
  font-weight: 700;
}

.auth-card__footer {
  margin-top: var(--sp-5);
  text-align: center;
  color: var(--c-text-muted);
  font-size: 14px;
}

/* Style the slotted footer link (the "Create one" / "Sign in" RouterLink). */
.auth-card__footer :slotted(a) {
  color: var(--c-primary);
  font-weight: 600;
  text-decoration: none;
}

.auth-card__footer :slotted(a:hover) {
  text-decoration: underline;
}
</style>
