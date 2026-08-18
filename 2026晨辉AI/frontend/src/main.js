import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import axios from 'axios'

// 配置axios默认值
axios.defaults.baseURL = '/api'

const app = createApp(App)
app.use(router)
app.mount('#app')
