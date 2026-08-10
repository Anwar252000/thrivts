import { LayoutDashboard, PackageSearch, Gavel, Handshake } from 'lucide-react'
import { DashboardShell, type NavItem } from './DashboardShell'

const navItems: NavItem[] = [
  { to: '/seller', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/seller/requirements', label: 'Open Requirements', icon: PackageSearch },
  { to: '/seller/quotes', label: 'My Quotes', icon: Gavel },
  { to: '/seller/deals', label: 'My Deals', icon: Handshake },
]

export function SellerLayout() {
  return <DashboardShell roleLabel="Seller" navItems={navItems} />
}
