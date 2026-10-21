import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Proxy API traffic during local development; avoids needing backend CORS changes.
export default defineConfig({
  plugins: [react()],
  server: { proxy: { '/api': 'http://localhost:5080', '/health': 'http://localhost:5080' } },
});
