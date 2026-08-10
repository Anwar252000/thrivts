import { createBrowserRouter } from 'react-router-dom'
import { PublicLayout } from '@/layouts/PublicLayout'
import { BuyerLayout } from '@/layouts/BuyerLayout'
import { SellerLayout } from '@/layouts/SellerLayout'
import { AdminLayout } from '@/layouts/AdminLayout'
import { Landing } from '@/pages/Landing'
import { Login } from '@/pages/auth/Login'
import { BuyerDashboard } from '@/pages/buyer/Dashboard'
import { SellerDashboard } from '@/pages/seller/Dashboard'
import { AdminDashboard } from '@/pages/admin/Dashboard'
import { Placeholder } from '@/components/dashboard/Placeholder'
import { ProtectedRoute } from '@/components/auth/ProtectedRoute'

export const router = createBrowserRouter([
  // The marketing homepage brings its own nav/footer (MarketingNav/MarketingFooter) —
  // it must NOT be nested in PublicLayout, which renders a second, conflicting header.
  { path: '/', element: <Landing /> },
  {
    element: <PublicLayout />,
    children: [{ path: '/login', element: <Login /> }],
  },
  {
    path: '/buyer',
    element: (
      <ProtectedRoute role="buyer">
        <BuyerLayout />
      </ProtectedRoute>
    ),
    children: [
      { index: true, element: <BuyerDashboard /> },
      { path: 'requirements', element: <Placeholder title="My Requirements" /> },
      { path: 'deals', element: <Placeholder title="My Deals" /> },
    ],
  },
  {
    path: '/seller',
    element: (
      <ProtectedRoute role="seller">
        <SellerLayout />
      </ProtectedRoute>
    ),
    children: [
      { index: true, element: <SellerDashboard /> },
      { path: 'requirements', element: <Placeholder title="Open Requirements" /> },
      { path: 'quotes', element: <Placeholder title="My Quotes" /> },
      { path: 'deals', element: <Placeholder title="My Deals" /> },
    ],
  },
  {
    path: '/admin',
    element: (
      <ProtectedRoute role="admin">
        <AdminLayout />
      </ProtectedRoute>
    ),
    children: [
      { index: true, element: <AdminDashboard /> },
      { path: 'buyers', element: <Placeholder title="Buyers" /> },
      { path: 'sellers', element: <Placeholder title="Sellers" /> },
      { path: 'deals', element: <Placeholder title="Deals" /> },
      { path: 'settings', element: <Placeholder title="Settings" /> },
    ],
  },
])
