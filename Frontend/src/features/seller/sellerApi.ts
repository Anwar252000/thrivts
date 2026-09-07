import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type { RootState } from '@/app/store'
import type { SellerProfile, SellerDashboard, OpenRequirement, MyQuote, SellerOffer, SellerDeal } from './sellerTypes'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL

export const sellerApi = createApi({
  reducerPath: 'sellerApi',
  baseQuery: fetchBaseQuery({
    baseUrl: API_BASE_URL,
    prepareHeaders: (headers, { getState }) => {
      const token = (getState() as RootState).auth.accessToken
      if (token) headers.set('Authorization', `Bearer ${token}`)
      return headers
    },
  }),
  tagTypes: ['Profile', 'Dashboard', 'Requirement', 'Quote', 'Offer', 'Deal'],
  endpoints: (builder) => ({
    getMyProfile: builder.query<SellerProfile, void>({
      query: () => '/api/v1/sellers/me',
      providesTags: ['Profile'],
    }),
    getDashboard: builder.query<SellerDashboard, void>({
      query: () => '/api/v1/sellers/dashboard',
      providesTags: ['Dashboard'],
    }),
    getOpenRequirements: builder.query<OpenRequirement[], void>({
      query: () => '/api/v1/sellers/requirements',
      providesTags: (result) =>
        result
          ? [...result.map((r) => ({ type: 'Requirement' as const, id: r.id })), { type: 'Requirement' as const, id: 'LIST' }]
          : [{ type: 'Requirement' as const, id: 'LIST' }],
    }),
    getMyQuotes: builder.query<MyQuote[], void>({
      query: () => '/api/v1/sellers/quotes',
      providesTags: ['Quote'],
    }),
    submitQuote: builder.mutation<string, { requirementId: string; availableQuantityPcs: number; pricePerPcUsd: number; sellerNotes?: string }>({
      query: (body) => ({ url: '/api/v1/sellers/quotes', method: 'POST', body }),
      invalidatesTags: ['Quote', { type: 'Requirement', id: 'LIST' }, 'Dashboard'],
    }),
    respondToBuyerCounter: builder.mutation<void, { bidId: string; action: 'accept' | 'decline' | 'counter'; newPriceUsd?: number; note?: string }>({
      query: ({ bidId, ...body }) => ({ url: `/api/v1/sellers/quotes/${bidId}/respond`, method: 'POST', body }),
      invalidatesTags: ['Quote', 'Dashboard', { type: 'Deal', id: 'LIST' }],
    }),
    getOffers: builder.query<SellerOffer[], void>({
      query: () => '/api/v1/sellers/offers',
      providesTags: ['Offer'],
    }),
    respondToOffer: builder.mutation<void, { offerId: string; action: 'accept' | 'decline' | 'counter' | 'message'; pricePerPcUsd?: number; notes?: string }>({
      query: ({ offerId, ...body }) => ({ url: `/api/v1/sellers/offers/${offerId}/respond`, method: 'POST', body }),
      invalidatesTags: ['Offer', 'Dashboard', { type: 'Requirement', id: 'LIST' }],
    }),
    getMyDeals: builder.query<SellerDeal[], void>({
      query: () => '/api/v1/sellers/deals',
      providesTags: ['Deal'],
    }),
    markOffersViewed: builder.mutation<void, string[]>({
      query: (offerIds) => ({ url: '/api/v1/sellers/offers/mark-viewed', method: 'POST', body: { offerIds } }),
      invalidatesTags: ['Offer'],
    }),
  }),
})

export const {
  useGetMyProfileQuery, useGetDashboardQuery, useGetOpenRequirementsQuery, useGetMyQuotesQuery, useSubmitQuoteMutation,
  useRespondToBuyerCounterMutation, useGetOffersQuery, useRespondToOfferMutation, useGetMyDealsQuery, useMarkOffersViewedMutation,
} = sellerApi
