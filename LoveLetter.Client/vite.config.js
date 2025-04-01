import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
    plugins: [react()],
    server: {
        proxy: {
            // Only proxy requests that start with /lobbies
            '/lobbies': {
                target: 'http://localhost:5062', // Your backend URL
                changeOrigin: true, // Important for virtual hosted sites (and often needed)
                secure: false,      // Keep false if backend target is HTTP or uses self-signed HTTPS cert
            },
           
        },
    },
})