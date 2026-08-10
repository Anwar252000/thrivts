import { LayoutDashboard, Users, Store, Handshake, Settings } from 'lucide-react'
import { DashboardShell, type NavItem } from './DashboardShell'

const navItems: NavItem[] = [
  { to: '/admin', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/admin/buyers', label: 'Buyers', icon: Users },
  { to: '/admin/sellers', label: 'Sellers', icon: Store },
  { to: '/admin/deals', label: 'Deals', icon: Handshake },
  { to: '/admin/settings', label: 'Settings', icon: Settings },
]

export function AdminLayout() {
  return <DashboardShell roleLabel="Admin" navItems={navItems} />
}
