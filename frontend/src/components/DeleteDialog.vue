<script setup lang="ts">
import { onMounted, ref } from 'vue'
import ModalDialog from '@/components/ModalDialog.vue'

// Confirmation before a permanent delete. The parent owns the delete request and passes `deleting`
// (in-flight) and `error` (failed) so the dialog stays open and visible on failure.
defineProps<{ title: string; deleting: boolean; error: string | null }>()
const emit = defineEmits<{ confirm: []; close: [] }>()

// Focus Cancel by default — the safer choice for a destructive dialog.
const cancelButton = ref<HTMLButtonElement | null>(null)
onMounted(() => cancelButton.value?.focus())
</script>

<template>
  <ModalDialog labelledby="delete-title" :max-width="400" @close="emit('close')">
    <div class="confirm">
      <div class="confirm__head">
        <div class="confirm__icon" aria-hidden="true">!</div>
        <div>
          <h2 id="delete-title" class="confirm__title">Delete task?</h2>
          <p class="confirm__text">
            “{{ title }}” will be permanently deleted. This action can’t be undone.
          </p>
        </div>
      </div>

      <p v-if="error" class="confirm__error" role="alert">{{ error }}</p>

      <div class="confirm__footer">
        <button
          ref="cancelButton"
          type="button"
          class="btn-cancel"
          :disabled="deleting"
          @click="emit('close')"
        >
          Cancel
        </button>
        <button type="button" class="btn-danger" :disabled="deleting" @click="emit('confirm')">
          <span v-if="deleting" class="btn-spinner" aria-hidden="true"></span>
          {{ deleting ? 'Deleting…' : 'Delete task' }}
        </button>
      </div>
    </div>
  </ModalDialog>
</template>

<style scoped>
.confirm {
  padding: var(--sp-5);
}

.confirm__head {
  display: flex;
  gap: var(--sp-4);
  align-items: flex-start;
}

.confirm__icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  min-width: 40px;
  border-radius: 10px;
  background: var(--c-danger-bg);
  color: var(--c-danger);
  font-size: 20px;
  font-weight: 700;
}

.confirm__title {
  font-size: 17px;
  font-weight: 700;
}

.confirm__text {
  margin-top: 7px;
  color: var(--c-text-muted);
  font-size: 14px;
  line-height: 1.5;
}

.confirm__error {
  margin-top: var(--sp-4);
  padding: 10px 12px;
  background: var(--c-overdue-bg);
  border-radius: var(--r-sm);
  color: var(--c-overdue);
  font-size: 13px;
  font-weight: 500;
}

.confirm__footer {
  display: flex;
  gap: 10px;
  justify-content: flex-end;
  margin-top: var(--sp-5);
}

.btn-cancel {
  padding: 10px 18px;
  background: transparent;
  border: 1px solid var(--c-border-strong);
  border-radius: var(--r-sm);
  color: var(--c-text);
  font-size: 14px;
  font-weight: 600;
}

.btn-cancel:hover:not(:disabled) {
  background: var(--c-surface-2);
}

.btn-danger {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 18px;
  background: var(--c-danger);
  border: none;
  border-radius: var(--r-sm);
  color: #fff;
  font-size: 14px;
  font-weight: 600;
}

.btn-danger:hover:not(:disabled) {
  background: var(--c-danger-hover);
}

.btn-cancel:disabled,
.btn-danger:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-spinner {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.45);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}
</style>
