import { createBrowserRouter } from 'react-router-dom'
import { ProtectedRoute } from '@/features/auth/components/ProtectedRoute'
import { Login } from '@/features/auth/pages/Login'
import { Home } from '@/pages/Home'
import { NotFound } from '@/pages/NotFound'

export const router = createBrowserRouter([
  { path: '/login', element: <Login /> },
  {
    path: '/',
    element: (
      <ProtectedRoute>
        <Home />
      </ProtectedRoute>
    ),
  },
  { path: '*', element: <NotFound /> },
])
