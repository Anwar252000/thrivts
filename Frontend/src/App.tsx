import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router-dom'
import { Provider as ReduxProvider } from 'react-redux'
import { store } from '@/app/store'
import { ThemeProvider } from '@/lib/theme'
import { AuthBootstrapper } from '@/components/auth/AuthBootstrapper'
import { router } from '@/routes/router'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
})

function App() {
  return (
    <ReduxProvider store={store}>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider>
          <AuthBootstrapper>
            <RouterProvider router={router} />
          </AuthBootstrapper>
        </ThemeProvider>
      </QueryClientProvider>
    </ReduxProvider>
  )
}

export default App
