import { createContext, useContext } from 'react'

export type ThemeMode = 'light' | 'dark'

/** Curated accents that still read as "Thrivts" — swap the CSS var, nothing else changes. */
export const ACCENT_PRESETS = [
  { name: 'Copper', value: '#c5874b' },
  { name: 'Sage', value: '#5c6a62' },
  { name: 'Terracotta', value: '#b5651d' },
  { name: 'Slate Blue', value: '#2d4f6b' },
] as const

export interface ThemeContextValue {
  mode: ThemeMode
  accent: string
  setMode: (mode: ThemeMode) => void
  toggleMode: () => void
  setAccent: (hex: string) => void
}

export const ThemeContext = createContext<ThemeContextValue | null>(null)

export function useTheme() {
  const ctx = useContext(ThemeContext)
  if (!ctx) throw new Error('useTheme must be used within a ThemeProvider')
  return ctx
}
