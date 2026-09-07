import { createBrowserRouter } from 'react-router-dom'
import { BuyerLayout } from '@/layouts/BuyerLayout'
import { SellerLayout } from '@/layouts/SellerLayout'
import { AdminLayout } from '@/layouts/AdminLayout'
import { Landing } from '@/pages/Landing'
import { BuyerLanding } from '@/pages/landing/BuyerLanding'
import { SellerLanding } from '@/pages/landing/SellerLanding'
import { Login } from '@/pages/auth/Login'
import { ForgotPassword } from '@/pages/auth/ForgotPassword'
import { ResetPassword } from '@/pages/auth/ResetPassword'
import { BuyerSignup } from '@/pages/auth/BuyerSignup'
import { SellerSignup } from '@/pages/auth/SellerSignup'
import { VerifyEmail } from '@/pages/auth/VerifyEmail'
import { BuyerDashboard } from '@/pages/buyer/Dashboard'
import { PostRequirement } from '@/pages/buyer/PostRequirement'
import { BuyerRequirements } from '@/pages/buyer/requirements/Requirements'
import { BuyerDeals } from '@/pages/buyer/deals/Deals'
import { BuyerMessages } from '@/pages/buyer/Messages'
import { BuyerHistory } from '@/pages/buyer/History'
import { BuyerProfile } from '@/pages/buyer/Profile'
import { SellerDashboard } from '@/pages/seller/Dashboard'
import { SellerRequirements } from '@/pages/seller/requirements/Requirements'
import { SellerQuotes } from '@/pages/seller/quotes/Quotes'
import { SellerOffers } from '@/pages/seller/offers/Offers'
import { SellerDeals } from '@/pages/seller/deals/Deals'
import { SellerProfile } from '@/pages/seller/Profile'
import { AdminDashboard } from '@/pages/admin/Dashboard'
import { AdminApprovals } from '@/pages/admin/Approvals'
import { AdminUsers } from '@/pages/admin/users/Users'
import { AdminBuyers } from '@/pages/admin/buyers/Buyers'
import { AdminSellers } from '@/pages/admin/sellers/Sellers'
import { AdminAgencies } from '@/pages/admin/agencies/Agencies'
import { AdminRequirements } from '@/pages/admin/requirements/Requirements'
import { AdminDeals } from '@/pages/admin/deals/Deals'
import { AdminCommissions } from '@/pages/admin/Commissions'
import { AdminDisputes } from '@/pages/admin/disputes/Disputes'
import { AdminMessages } from '@/pages/admin/Messages'
import { AdminSettings } from '@/pages/admin/Settings'
import { AdminAudit } from '@/pages/admin/Audit'
import { ProtectedRoute } from '@/components/auth/ProtectedRoute'

export const router = createBrowserRouter([
  // The marketing homepage and the login screen each bring their own full-bleed chrome
  // (MarketingNav/MarketingFooter, and admin.html's centered .login-screen respectively) —
  // neither is nested in PublicLayout, which would render a second, conflicting header.
  { path: '/', element: <Landing /> },
  { path: '/sourcing', element: <BuyerLanding /> },
  { path: '/selling', element: <SellerLanding /> },
  { path: '/login', element: <Login /> },
  { path: '/apply', element: <BuyerSignup /> },
  { path: '/apply-seller', element: <SellerSignup /> },
  { path: '/verify-email', element: <VerifyEmail /> },
  { path: '/forgot-password', element: <ForgotPassword /> },
  { path: '/reset-password', element: <ResetPassword /> },
  {
    path: '/buyer',
    element: (
      <ProtectedRoute role="buyer">
        <BuyerLayout />
      </ProtectedRoute>
    ),
    children: [
      { index: true, element: <BuyerDashboard /> },
      { path: 'post-requirement', element: <PostRequirement /> },
      { path: 'requirements', element: <BuyerRequirements /> },
      { path: 'deals', element: <BuyerDeals /> },
      { path: 'messages', element: <BuyerMessages /> },
      { path: 'history', element: <BuyerHistory /> },
      { path: 'profile', element: <BuyerProfile /> },
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
      { path: 'requirements', element: <SellerRequirements /> },
      { path: 'quotes', element: <SellerQuotes /> },
      { path: 'offers', element: <SellerOffers /> },
      { path: 'deals', element: <SellerDeals /> },
      { path: 'profile', element: <SellerProfile /> },
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
      { path: 'approvals', element: <AdminApprovals /> },
      { path: 'users', element: <AdminUsers /> },
      { path: 'buyers', element: <AdminBuyers /> },
      { path: 'sellers', element: <AdminSellers /> },
      { path: 'agencies', element: <AdminAgencies /> },
      { path: 'requirements', element: <AdminRequirements /> },
      { path: 'deals', element: <AdminDeals /> },
      { path: 'commissions', element: <AdminCommissions /> },
      { path: 'disputes', element: <AdminDisputes /> },
      { path: 'messages', element: <AdminMessages /> },
      { path: 'settings', element: <AdminSettings /> },
      { path: 'audit', element: <AdminAudit /> },
    ],
  },
])
