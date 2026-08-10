import { useEffect, useState, type ReactNode } from 'react'
import { ACCENT_PRESETS, ThemeContext, type ThemeMode } from './theme-context'

const STORAGE_KEY_MODE = 'thrivts:theme-mode'
const STORAGE_KEY_ACCENT = 'thrivts:theme-accent'

function getInitialMode(): ThemeMode {
  if (typeof window === 'undefined') return 'light'
  const stored = window.localStorage.getItem(STORAGE_KEY_MODE)
  if (stored === 'light' || stored === 'dark') return stored
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function getInitialAccent(): string {
  if (typeof window === 'undefined') return ACCENT_PRESETS[0].value
  return window.localStorage.getItem(STORAGE_KEY_ACCENT) ?? ACCENT_PRESETS[0].value
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [mode, setModeState] = useState<ThemeMode>(getInitialMode)
  const [accent, setAccentState] = useState<string>(getInitialAccent)

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', mode)
    window.localStorage.setItem(STORAGE_KEY_MODE, mode)
  }, [mode])

  useEffect(() => {
    document.documentElement.style.setProperty('--color-accent', accent)
    window.localStorage.setItem(STORAGE_KEY_ACCENT, accent)
  }, [accent])

  const setMode = (next: ThemeMode) => setModeState(next)
  const toggleMode = () => setModeState((prev) => (prev === 'light' ? 'dark' : 'light'))
  const setAccent = (hex: string) => setAccentState(hex)

  return (
    <ThemeContext.Provider value={{ mode, accent, setMode, toggleMode, setAccent }}>
      {children}
    </ThemeContext.Provider>
  )
}
