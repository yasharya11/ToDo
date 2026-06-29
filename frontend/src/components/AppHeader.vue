<script setup lang="ts">
// The app-shell header: persistent chrome shown above authenticated content.
// It is purely presentational — the brand sits on the left, and the right-hand
// `actions` slot is where later tickets drop the user email + Log out (#21) and
// the Create task button (#22/#23). The shell owns no auth or task state itself.
</script>

<template>
  <header class="app-header">
    <div class="app-header__brand">
      <span class="app-header__logo" aria-hidden="true">T</span>
      <span class="app-header__wordmark">Tasker</span>
    </div>
    <div class="app-header__actions">
      <slot name="actions" />
    </div>
  </header>
</template>

<style scoped>
.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--sp-4);
  padding: var(--sp-3) var(--sp-5);
  background: var(--c-surface);
  border-bottom: 1px solid var(--c-border);
}

.app-header__brand {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
}

.app-header__logo {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border-radius: 7px;
  background: var(--c-primary);
  color: #fff;
  font-weight: 700;
  font-size: var(--fs-base);
}

.app-header__wordmark {
  font-weight: 700;
  font-size: var(--fs-md);
  letter-spacing: -0.01em;
}

.app-header__actions {
  display: flex;
  align-items: center;
  gap: var(--sp-4);
}

/* Match the kit's tighter mobile header. */
@media (max-width: 640px) {
  .app-header {
    padding: var(--sp-3) var(--sp-4);
  }

  .app-header__logo {
    width: 28px;
    height: 28px;
  }

  .app-header__wordmark {
    font-size: 16px;
  }
}
</style>
