import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// https://vite.dev/config/
export default defineConfig(
  ({ mode }) => {
    const env = loadEnv(mode, process.cwd(), '')

    return {
      plugins: [react()],
      server: {
        // keep any existing server settings 
        proxy: env.LOCAL_API_URL ? {
          '/api': {
            target: env.LOCAL_API_URL,
            changeOrigin: true,
            secure: false, // accept the local .NET dev certificate
          },
        } : undefined,
      },
    }
  }
)
