<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ApiError, fieldErrors } from '@/api/http'
import { isValidEmail } from '@/utils/validation'
import AuthCard from '@/components/AuthCard.vue'
import TextField from '@/components/TextField.vue'
import SubmitButton from '@/components/SubmitButton.vue'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const emailError = ref<string | null>(null)
const passwordError = ref<string | null>(null)
const serverError = ref<string | null>(null)
const submitting = ref(false)

// A light client-side check (the API is the real validator). Mirrors the API's rules: a valid
// email and a password of at least 8 characters.
function validate(): boolean {
  const trimmedEmail = email.value.trim()
  if (!trimmedEmail) {
    emailError.value = 'Email is required.'
  } else if (!isValidEmail(trimmedEmail)) {
    emailError.value = 'A valid email address is required.'
  } else {
    emailError.value = null
  }

  if (!password.value) {
    passwordError.value = 'Password is required.'
  } else if (password.value.length < 8) {
    passwordError.value = 'Password must be at least 8 characters.'
  } else {
    passwordError.value = null
  }

  return !emailError.value && !passwordError.value
}

async function onSubmit(): Promise<void> {
  serverError.value = null
  if (!validate()) {
    return
  }

  submitting.value = true
  try {
    // register() also logs in on success, so the user lands authenticated on the task list.
    await auth.register(email.value, password.value)
    await router.push('/')
  } catch (error) {
    if (error instanceof ApiError && error.status === 409) {
      serverError.value = 'That email is already registered. Try signing in instead.'
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
  <AuthCard title="Create your account" subtitle="Start tracking your tasks" :error="serverError">
    <form novalidate @submit.prevent="onSubmit">
      <TextField
        id="register-email"
        v-model="email"
        label="Email"
        type="email"
        placeholder="you@example.com"
        autocomplete="email"
        :error="emailError"
      />
      <TextField
        id="register-password"
        v-model="password"
        label="Password"
        type="password"
        placeholder="At least 8 characters"
        autocomplete="new-password"
        :error="passwordError"
        :hint="passwordError ? null : 'Use 8 or more characters.'"
      />
      <SubmitButton
        :loading="submitting"
        label="Create account"
        loading-label="Creating account…"
      />
    </form>

    <template #footer>
      Already have an account?
      <RouterLink :to="{ name: 'login' }">Sign in</RouterLink>
    </template>
  </AuthCard>
</template>
