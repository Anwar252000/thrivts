import { createContext, useContext } from 'react'

/** Page-to-page navigation inside the owner app (e.g. Dues → a supplier's hisab), with optional
 *  parameters the target page reads once. */
export type Navigate = (page: string, params?: Record<string, string>) => void

export const NavContext = createContext<{ go: Navigate; params: Record<string, string> }>({ go: () => undefined, params: {} })

export const useNavigate = () => useContext(NavContext).go

export const usePageParams = () => useContext(NavContext).params
