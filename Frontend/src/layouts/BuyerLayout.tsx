import { LayoutDashboard, FileText, Handshake } from 'lucide-react'
import { DashboardShell, type NavItem } from './DashboardShell'

const navItems: NavItem[] = [
  { to: '/buyer', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/buyer/requirements', label: 'My Requirements', icon: FileText },
  { to: '/buyer/deals', label: 'My Deals', icon: Handshake },
]

export function BuyerLayout() {
  return <DashboardShell roleLabel="Buyer" navItems={navItems} />
}
