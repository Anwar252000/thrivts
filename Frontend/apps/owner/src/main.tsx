import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createApi } from '@shared/api/client'
import { AppProviders } from '@shared/api/context'
import { ToastProvider } from '@shared/ui/toast'
import { App } from './App'
import './styles/legacy.css'
import './styles/app.css'

const api = createApi('Owner')

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AppProviders api={api}>
      <ToastProvider>
        <App />
      </ToastProvider>
    </AppProviders>
  </StrictMode>,
)
