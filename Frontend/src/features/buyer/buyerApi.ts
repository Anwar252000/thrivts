import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type { RootState } from '@/app/store'
import type { Category, PurchaseOrder } from '@/features/admin/adminTypes'
import type {
  BuyerProfile, BuyerDashboard, MyRequirementListItem, BuyerVisibleBid, MyDealListItem,
  MyMessageThread, PublicExchangeRate, PublicActivityItem,
} from './buyerTypes'
import type { GradeType, CurrencyType } from '@/features/admin/adminTypes'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL

export const buyerApi = createApi({
  reducerPath: 'buyerApi',
  baseQuery: fetchBaseQuery({
    baseUrl: API_BASE_URL,
    prepareHeaders: (headers, { getState }) => {
      const token = (getState() as RootState).auth.accessToken
      if (token) headers.set('Authorization', `Bearer ${token}`)
      return headers
    },
  }),
  tagTypes: ['Profile', 'Dashboard', 'Requirement', 'Bid', 'Deal', 'MessageThread', 'PurchaseOrder'],
  endpoints: (builder) => ({
    // ---- Buyer (self-service) ----
    getMyProfile: builder.query<BuyerProfile, void>({
      query: () => '/api/v1/buyers/me',
      providesTags: ['Profile'],
    }),
    getDashboard: builder.query<BuyerDashboard, void>({
      query: () => '/api/v1/buyers/dashboard',
      providesTags: ['Dashboard'],
    }),
    getMyRequirements: builder.query<MyRequirementListItem[], void>({
      query: () => '/api/v1/buyers/requirements',
      providesTags: (result) =>
        result
          ? [...result.map((r) => ({ type: 'Requirement' as const, id: r.id })), { type: 'Requirement' as const, id: 'LIST' }]
          : [{ type: 'Requirement' as const, id: 'LIST' }],
    }),
    postRequirement: builder.mutation<{ id: string }, {
      itemName: string; categoryId: number; grade: GradeType; quantityPcs: number; shippingMode?: string
      deliveryTimelineDays?: number; destinationCountry: string; currency: CurrencyType; pricePerPc: number; notes?: string
    }>({
      query: (body) => ({ url: '/api/v1/buyers/requirements', method: 'POST', body }),
      invalidatesTags: [{ type: 'Requirement', id: 'LIST' }, 'Dashboard'],
    }),
    deleteRequirement: builder.mutation<void, string>({
      query: (id) => ({ url: `/api/v1/buyers/requirements/${id}`, method: 'DELETE' }),
      invalidatesTags: [{ type: 'Requirement', id: 'LIST' }, 'Dashboard'],
    }),
    getBids: builder.query<BuyerVisibleBid[], string>({
      query: (requirementId) => `/api/v1/buyers/requirements/${requirementId}/bids`,
      providesTags: (_r, _e, requirementId) => [{ type: 'Bid', id: requirementId }],
    }),
    counterBid: builder.mutation<void, { bidId: string; requirementId: string; counterBuyerPriceUsd: number; note?: string }>({
      query: ({ bidId, counterBuyerPriceUsd, note }) => ({ url: `/api/v1/buyers/bids/${bidId}/counter`, method: 'POST', body: { counterBuyerPriceUsd, note } }),
      invalidatesTags: (_r, _e, { requirementId }) => [{ type: 'Bid', id: requirementId }],
    }),
    acceptBid: builder.mutation<string, { bidId: string; requirementId: string }>({
      query: ({ bidId }) => ({ url: `/api/v1/buyers/bids/${bidId}/accept`, method: 'POST' }),
      invalidatesTags: (_r, _e, { requirementId }) => [{ type: 'Bid', id: requirementId }, { type: 'Requirement', id: 'LIST' }, { type: 'Deal', id: 'LIST' }, 'Dashboard'],
    }),
    getMyDeals: builder.query<MyDealListItem[], void>({
      query: () => '/api/v1/buyers/deals',
      providesTags: (result) =>
        result
          ? [...result.map((d) => ({ type: 'Deal' as const, id: d.id })), { type: 'Deal' as const, id: 'LIST' }]
          : [{ type: 'Deal' as const, id: 'LIST' }],
    }),
    raiseDispute: builder.mutation<string, { dealId: string; description: string; category?: string; requestedResolution?: string }>({
      query: ({ dealId, ...body }) => ({ url: `/api/v1/buyers/deals/${dealId}/disputes`, method: 'POST', body }),
      invalidatesTags: [{ type: 'Deal', id: 'LIST' }],
    }),
    getPurchaseOrder: builder.query<PurchaseOrder | null, string>({
      query: (dealId) => `/api/v1/buyers/deals/${dealId}/purchase-order`,
      providesTags: (_r, _e, dealId) => [{ type: 'PurchaseOrder', id: dealId }],
    }),
    requestPaymentLink: builder.mutation<void, string>({
      query: (dealId) => ({ url: `/api/v1/buyers/deals/${dealId}/purchase-order/request-payment-link`, method: 'POST' }),
      invalidatesTags: (_r, _e, dealId) => [{ type: 'PurchaseOrder', id: dealId }],
    }),
    markPoPaid: builder.mutation<void, { dealId: string; receipt: File }>({
      query: ({ dealId, receipt }) => {
        const formData = new FormData()
        formData.append('receipt', receipt)
        return { url: `/api/v1/buyers/deals/${dealId}/purchase-order/mark-paid`, method: 'POST', body: formData }
      },
      invalidatesTags: (_r, _e, { dealId }) => [{ type: 'PurchaseOrder', id: dealId }],
    }),
    getMessageThreads: builder.query<MyMessageThread[], void>({
      query: () => '/api/v1/buyers/message-threads',
      providesTags: ['MessageThread'],
    }),
    applyReferralCode: builder.mutation<void, string>({
      query: (code) => ({ url: '/api/v1/buyers/referral-code', method: 'POST', body: { code } }),
      invalidatesTags: ['Profile'],
    }),

    // ---- Public (anonymous-reachable, also used pre-auth on the signup form) ----
    getPublicCategories: builder.query<Category[], void>({
      query: () => '/api/v1/public/categories',
    }),
    getPublicExchangeRates: builder.query<PublicExchangeRate[], void>({
      query: () => '/api/v1/public/exchange-rates',
    }),
    getPublicActivity: builder.query<PublicActivityItem[], number | void>({
      query: (limit) => ({ url: '/api/v1/public/activity', params: limit ? { limit } : undefined }),
    }),
    getAgencyName: builder.query<{ agencyName: string }, string>({
      query: (code) => `/api/v1/public/agencies/${code}`,
    }),
  }),
})

export const {
  useGetMyProfileQuery, useGetDashboardQuery, useGetMyRequirementsQuery, usePostRequirementMutation, useDeleteRequirementMutation,
  useGetBidsQuery, useCounterBidMutation, useAcceptBidMutation, useGetMyDealsQuery, useRaiseDisputeMutation,
  useGetPurchaseOrderQuery, useRequestPaymentLinkMutation, useMarkPoPaidMutation,
  useGetMessageThreadsQuery, useApplyReferralCodeMutation,
  useGetPublicCategoriesQuery, useGetPublicExchangeRatesQuery, useGetPublicActivityQuery, useGetAgencyNameQuery,
} = buyerApi
