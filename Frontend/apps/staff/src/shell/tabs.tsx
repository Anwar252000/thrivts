import type { ComponentType } from 'react'
import type { UserRole } from '@shared/api/types'
import { GateEntry } from '../features/gate/GateEntry'
import { GateHistory } from '../features/gate/GateHistory'
import { Placeholder } from './Placeholder'

export interface TabDef {
  id: string
  label: string
  Component: ComponentType
}

const soon = (title: string): ComponentType => () => <Placeholder title={title} />

/**
 * Each staff login's tabs, in the legacy order. The API decides what each person may actually do
 * (permissions); this only arranges their screens. Co-sign is also on the GM's bar — the rebuild
 * plan moves co-signing to the GM, and the GM role holds that permission.
 */
export const TABS: Partial<Record<UserRole, TabDef[]>> = {
  GateGuard: [
    { id: 'gatein', label: '📥 Stock In', Component: () => <GateEntry direction="In" /> },
    { id: 'gateout', label: '📤 Stock Out', Component: () => <GateEntry direction="Out" /> },
    { id: 'gatehistory', label: '📜 History', Component: GateHistory },
  ],
  Supervisor: [
    { id: 'lots', label: '📦 Lots', Component: soon('Lots') },
    { id: 'finalized', label: '📜 Finalized', Component: soon('Finalized lots') },
    { id: 'attendance', label: '🕒 Attendance', Component: soon('Attendance') },
    { id: 'dashboard', label: '📊 Dashboard', Component: soon('Dashboard') },
  ],
  InventoryManager: [
    { id: 'dashboard', label: '📊 Dashboard', Component: soon('Dashboard') },
    { id: 'stock', label: '✍ Review & Co-sign · Z1', Component: soon('Review & Co-sign') },
    { id: 'requirements', label: '📋 Requirements', Component: soon('Requirements') },
    { id: 'stockout', label: '📤 Stockout', Component: soon('Stockout') },
    { id: 'inventory', label: '📊 Inventory · Z3', Component: soon('Inventory') },
    { id: 'sku', label: '🏷️ SKU · Z4', Component: soon('SKU') },
    { id: 'wash', label: '🧺 Wash', Component: soon('Wash') },
    { id: 'warehouse', label: '🏭 Warehouse', Component: soon('Warehouse') },
  ],
  GeneralManager: [
    { id: 'dashboard', label: '📊 My Dashboard', Component: soon('My Dashboard') },
    { id: 'analytics', label: '📈 Analytics', Component: soon('Analytics') },
    { id: 'orders', label: '📋 Requirements', Component: soon('Requirements') },
    { id: 'cosign', label: '✍ Co-sign', Component: soon('Co-sign') },
    { id: 'team', label: '👥 Team Check', Component: soon('Team Check') },
    { id: 'expenditure', label: '💰 Expenditure', Component: soon('Expenditure') },
    { id: 'blockers', label: '⚠️ Blockers', Component: soon('Blockers') },
  ],
}
