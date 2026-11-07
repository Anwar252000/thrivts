import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['**/dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
  },
  {
    // Shared providers export their hooks alongside them, and the tab/page registries export
    // component tables — neither is a hot-reloaded component module.
    files: ['shared/src/**/*.{ts,tsx}', 'apps/*/src/shell/{tabs,pages}.tsx'],
    rules: { 'react-refresh/only-export-components': 'off' },
  },
])
