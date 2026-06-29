import './assets/styles/tokens.css'
import './assets/styles/base.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { useAuthStore } from './stores/auth'
import { configureHttp } from './api/http'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

// Wire the HTTP layer to the auth store: attach the bearer token to authenticated requests, and
// on a 401 clear the session and return to login. Done after Pinia is active so the store exists.
const auth = useAuthStore(pinia)
configureHttp({
  token: () => auth.token,
  onUnauthorized: () => {
    auth.logout()
    if (router.currentRoute.value.name !== 'login') {
      void router.push({ name: 'login' })
    }
  },
})

app.mount('#app')
