import type { ComponentType } from 'react'
import { AssistantPage } from '../features/assistant/AssistantPage'
import { CatalogPage } from '../features/catalog/CatalogPage'
import { ClientsPage } from '../features/clients/ClientsPage'
import { RequestsPage } from '../features/clients/RequestsPage'
import { DuesPage } from '../features/finance/DuesPage'
import { HisabPage } from '../features/finance/HisabPage'
import { InventoryPage } from '../features/finance/InventoryPage'
import { FloorPage } from '../features/floor/FloorPage'
import { EscalationsPage } from '../features/manage/EscalationsPage'
import { ManagePage } from '../features/manage/ManagePage'
import { TeamPage } from '../features/team/TeamPage'

export interface PageDef {
  key: string
  icon: string
  title: string
  sub: string
  /** Only shown when the user holds this permission (e.g. only the owner manages logins). */
  permission?: string
  Component: ComponentType
}

/** The legacy owner sidebar, in its order, plus the pages for the new backend features. */
export const PAGES: PageDef[] = [
  { key: 'floor', icon: '🏭', title: 'Live Floor', sub: 'Every bale, where it is right now', Component: FloorPage },
  { key: 'assistant', icon: '🤖', title: 'VH Assistant', sub: 'Ask anything about your business', Component: AssistantPage },
  { key: 'esc', icon: '🚨', title: 'Escalations', sub: 'Issues from the team — reply & resolve', Component: EscalationsPage },
  { key: 'vewc', icon: '🛳️', title: 'VEW Clients', sub: 'Client-facing orders & portals', Component: ClientsPage },
  { key: 'requests', icon: '💬', title: 'Client Requests', sub: 'Calls, videos and notes from client portals', Component: RequestsPage },
  { key: 'hisab', icon: '🧾', title: 'Supplier Hisab', sub: 'Each supplier is its own account', Component: HisabPage },
  { key: 'dues', icon: '💷', title: 'Dues & Money', sub: 'What you owe · what clients owe you', Component: DuesPage },
  { key: 'inv', icon: '📦', title: 'Inventory', sub: 'What we hold + its value', Component: InventoryPage },
  { key: 'catalog', icon: '🧵', title: 'Product Details', sub: 'Cost · sell · grade · criteria', Component: CatalogPage },
  { key: 'manage', icon: '⚙️', title: 'Approvals & Goals', sub: 'Approvals, goals', Component: ManagePage },
  { key: 'team', icon: '👥', title: 'Team & Access', sub: 'Who can sign in, and their PINs', permission: 'users:manage', Component: TeamPage },
]
