import './assets/design-system.css'
import './assets/base.css'
import './assets/components.css'
import './assets/transitions.css'

import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

createApp(App).use(router).mount('#app')
