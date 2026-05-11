import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5141,
    proxy: {
      '/api': {
        target: 'http://localhost:5140',
        changeOrigin: true,
      },
    },
  },
})
