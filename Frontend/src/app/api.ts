import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type { RootState } from './store'

/**
 * The single RTK Query API for Thrivts.Api. Feature slices add their endpoints with
 * `api.injectEndpoints({ endpoints: (builder) => ({ ... }) })` from their own `features/<name>/api.ts`,
 * and declare any new cache tags in `tagTypes` below.
 */
export const api = createApi({
  reducerPath: 'api',
  baseQuery: fetchBaseQuery({
    baseUrl: import.meta.env.VITE_API_BASE_URL,
    prepareHeaders: (headers, { getState }) => {
      const token = (getState() as RootState).auth.accessToken
      if (token) headers.set('Authorization', `Bearer ${token}`)
      return headers
    },
  }),
  tagTypes: [],
  endpoints: () => ({}),
})
