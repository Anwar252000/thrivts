import { RouterProvider } from 'react-router-dom'
import { Provider as ReduxProvider } from 'react-redux'
import { store } from '@/app/store'
import { router } from '@/app/router'
import { ThemeProvider } from '@/lib/theme'
import { AuthBootstrapper } from '@/features/auth/components/AuthBootstrapper'

function App() {
  return (
    <ReduxProvider store={store}>
      <ThemeProvider>
        <AuthBootstrapper>
          <RouterProvider router={router} />
        </AuthBootstrapper>
      </ThemeProvider>
    </ReduxProvider>
  )
}

export default App
