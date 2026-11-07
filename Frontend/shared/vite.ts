import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

/**
 * One Vite setup for both apps. Each app is its own root and its own deployment (separate
 * domains); they share `shared/` as plain source through the `@shared` alias, so there is no
 * package to build or publish. `.env` files are read from the Frontend folder for both.
 */
export function appConfig(appDir: string, port: number) {
  const frontendRoot = path.resolve(appDir, '../..')
  return defineConfig({
    root: appDir,
    envDir: frontendRoot,
    plugins: [react()],
    resolve: {
      alias: {
        '@shared': path.resolve(frontendRoot, 'shared/src'),
        '@': path.resolve(appDir, 'src'),
      },
    },
    server: { port, strictPort: true },
    preview: { port, strictPort: true },
    build: {
      outDir: path.resolve(appDir, 'dist'),
      emptyOutDir: true,
      sourcemap: true,
    },
  })
}
