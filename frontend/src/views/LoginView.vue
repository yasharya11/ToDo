<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ApiError, fieldErrors } from '@/api/http'
import { isValidEmail } from '@/utils/validation'
import AuthCard from '@/components/AuthCard.vue'
import TextField from '@/components/TextField.vue'
import SubmitButton from '@/components/SubmitButton.vue'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const emailError = ref<string | null>(null)
const passwordError = ref<string | null>(null)
const serverError = ref<string | null>(null)
const submitting = ref(false)

// Client-side checks for instant feedback before a round trip. The format check is purely local,
// so it catches a malformed email (e.g. "yash") without leaking whether an account exists — the
// API still answers every wrong login with the same generic 401.
function validate(): boolean {
  const trimmedEmail = email.value.trim()
  if (!trimmedEmail) {
    emailError.value = 'Email is required.'
  } else if (!isValidEmail(trimmedEmail)) {
    emailError.value = 'A valid email address is required.'
  } else {
    emailError.value = null
  }

  passwordError.value = password.value ? null : 'Password is required.'
  return !emailError.value && !passwordError.value
}

async function onSubmit(): Promise<void> {
  serverError.value = null
  if (!validate()) {
    return
  }

  submitting.value = true
  try {
    await auth.login(email.value, password.value)
    // Return to wherever the guard sent us from, or the task list by default.
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    await router.push(redirect)
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      serverError.value = 'Invalid email or password. Please try again.'
    } else if (error instanceof ApiError && error.status === 400) {
      const fields = fieldErrors(error.problem)
      emailError.value = fields.email ?? null
      passwordError.value = fields.password ?? null
      if (!fields.email && !fields.password) {
        serverError.value = error.message
      }
    } else if (error instanceof ApiError) {
      serverError.value = error.message
    } else {
      serverError.value = 'Something went wrong. Please try again.'
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <AuthCard title="Welcome back" subtitle="Sign in to your tasks" :error="serverError">
    <form novalidate @submit.prevent="onSubmit">
      <TextField
        id="login-email"
        v-model="email"
        label="Email"
        type="email"
        autocomplete="email"
        :error="emailError"
      />
      <TextField
        id="login-password"
        v-model="password"
        label="Password"
        type="password"
        autocomplete="current-password"
        :error="passwordError"
      />
      <SubmitButton :loading="submitting" label="Sign in" loading-label="Signing in…" />
    </form>

    <template #footer>
      Don't have an account?
      <RouterLink :to="{ name: 'register' }">Create one</RouterLink>
    </template>
  </AuthCard>
</template>
