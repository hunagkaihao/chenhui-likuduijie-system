import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import legacy from '@vitejs/plugin-legacy'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    legacy({
      targets: ['defaults', 'not IE 11'],
      additionalLegacyPolyfills: ['regenerator-runtime/runtime'],
      modernPolyfills: true,
      renderLegacyChunks: true
    })
  ],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',// 原复位器端口：5000
        changeOrigin: true,
        secure: false
      },
      '/pda-api': {
        target: 'http://192.168.68.6:8022',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/pda-api/, '')
      }
    }
  }
})
