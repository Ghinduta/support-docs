import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Proxy /api to the .NET API so the browser never makes a cross-origin call (no CORS setup needed).
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: process.env.API_URL ?? 'http://localhost:5000',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
})
