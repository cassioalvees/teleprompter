import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { readFileSync } from 'node:fs';

const pkg = JSON.parse(readFileSync(new URL('./package.json', import.meta.url), 'utf8'));

export default defineConfig({
  plugins: [react()],
  // Caminhos relativos: a mesma build abre por file:// no Electron e em qualquer subpasta na web.
  base: './',
  define: { __APP_VERSION__: JSON.stringify(pkg.version) },
  build: { outDir: 'dist', chunkSizeWarningLimit: 2000 },
  server: { port: 5173, strictPort: true }
});
