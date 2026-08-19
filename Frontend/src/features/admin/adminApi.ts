import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type { RootState } from '@/app/store'
import type {
  PagedResult, UserListItem, BuyerListItem, BuyerDetail, SellerListItem, SellerDetail, AgencyListItem, AgencyDetail,
  RequirementListItem, RequirementDetail, AdminBidBoardRow, DealListItem, DealDetail, DealAllocation, CommissionListItem,
  PlatformFeeRevenueMonth, DisputeListItem, MessageThreadListItem, AdminMessage, PendingApproval,
  AuditLogEntry, Category, ShippingRate, ExchangeRate, InfluencerListItem, PartnerApplication,
  PlatformFeeConfig, OfferListItem, OfferRound, DashboardStats,
  ProfileApprovalAction, UserRoleEnum, SellerTier, GradeType, CurrencyType,
  RequirementStatus, DealStatus, CommissionStatus, DisputeStatus, OfferStatus,
} from './adminTypes'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL

export const adminApi = createApi({
  reducerPath: 'adminApi',
  baseQuery: fetchBaseQuery({
    baseUrl: API_BASE_URL,
    prepareHeaders: (headers, { getState }) => {
      const token = (getState() as RootState).auth.accessToken
      if (token) headers.set('Authorization', `Bearer ${token}`)
      return headers
    },
  }),
  tagTypes: [
    'User', 'Buyer', 'Seller', 'Agency', 'Requirement', 'BidBoard', 'Deal', 'DealAllocation', 'Commission',
    'Dispute', 'MessageThread', 'Message', 'Approval', 'Audit', 'Category', 'ShippingRate',
    'ExchangeRate', 'Influencer', 'PartnerApplication', 'FeeConfig', 'Offer', 'OfferRound', 'Dashboard',
  ],
  endpoints: (builder) => ({
    // ---- Dashboard ----
    getDashboardStats: builder.query<DashboardStats, void>({
      query: () => '/api/v1/admin/dashboard/stats',
      providesTags: ['Dashboard'],
    }),

    // ---- Users (role-agnostic) ----
    getUsers: builder.query<PagedResult<UserListItem>, { role?: UserRoleEnum; search?: string; page?: number; pageSize?: number } | void>({
      query: (args) => ({
        url: '/api/v1/admin/users',
        params: { role: args?.role, search: args?.search, page: args?.page ?? 1, pageSize: args?.pageSize ?? 25 },
      }),
      providesTags: (result) =>
        result
          ? [...result.items.map((u) => ({ type: 'User' as const, id: u.id })), { type: 'User' as const, id: 'LIST' }]
          : [{ type: 'User' as const, id: 'LIST' }],
    }),
    createUser: builder.mutation<{ id: string }, {
      email: string; password: string; fullName: string; role: UserRoleEnum; phone?: string; whatsApp?: string
      companyName?: string; country?: string; publicAlias?: string; locationCity?: string; locationCountry?: string
      agencyName?: string; commissionRate?: number
    }>({
      query: (body) => ({ url: '/api/v1/admin/users', method: 'POST', body }),
      invalidatesTags: [{ type: 'User', id: 'LIST' }, 'Dashboard'],
    }),
    updateUser: builder.mutation<void, { userId: string; fullName: string; phone?: string; whatsApp?: string }>({
      query: ({ userId, ...body }) => ({ url: `/api/v1/admin/users/${userId}`, method: 'PUT', body }),
      invalidatesTags: (_r, _e, { userId }) => [{ type: 'User', id: userId }, { type: 'User', id: 'LIST' }],
    }),
    deleteUser: builder.mutation<void, string>({
      query: (id) => ({ url: `/api/v1/admin/users/${id}`, method: 'DELETE' }),
      invalidatesTags: [{ type: 'User', id: 'LIST' }, 'Dashboard'],
    }),

    // ---- Approvals ----
    getPendingApprovals: builder.query<PendingApproval[], { role?: UserRoleEnum } | void>({
      query: (args) => ({ url: '/api/v1/admin/approvals', params: args?.role ? { role: args.role } : undefined }),
      providesTags: ['Approval'],
    }),

    // ---- Buyers ----
    getBuyers: builder.query<PagedResult<BuyerListItem>, { page?: number; pageSize?: number } | void>({
      query: (args) => ({ url: '/api/v1/admin/buyers', params: { page: args?.page ?? 1, pageSize: args?.pageSize ?? 25 } }),
      providesTags: (result) =>
        result
          ? [...result.items.map((b) => ({ type: 'Buyer' as const, id: b.id })), { type: 'Buyer' as const, id: 'LIST' }]
          : [{ type: 'Buyer' as const, id: 'LIST' }],
    }),
    getBuyerById: builder.query<BuyerDetail, string>({
      query: (id) => `/api/v1/admin/buyers/${id}`,
      providesTags: (_r, _e, id) => [{ type: 'Buyer', id }],
    }),
    setBuyerApproval: builder.mutation<void, { buyerId: string; action: ProfileApprovalAction; reason?: string }>({
      query: ({ buyerId, ...body }) => ({ url: `/api/v1/admin/buyers/${buyerId}/approval`, method: 'POST', body }),
      invalidatesTags: (_r, _e, { buyerId }) => [{ type: 'Buyer', id: buyerId }, { type: 'Buyer', id: 'LIST' }, 'Approval', 'Dashboard'],
    }),
    updateBuyer: builder.mutation<void, { buyerId: string; companyName: string; country: string; city?: string | null; website?: string | null; instagram?: string | null }>({
      query: ({ buyerId, ...body }) => ({ url: `/api/v1/admin/buyers/${buyerId}`, method: 'PUT', body }),
      invalidatesTags: (_r, _e, { buyerId }) => [{ type: 'Buyer', id: buyerId }, { type: 'Buyer', id: 'LIST' }],
    }),
    deleteBuyer: builder.mutation<void, string>({
      query: (id) => ({ url: `/api/v1/admin/buyers/${id}`, method: 'DELETE' }),
      invalidatesTags: [{ type: 'Buyer', id: 'LIST' }, 'Dashboard'],
    }),

    // ---- Sellers ----
    getSellers: builder.query<PagedResult<SellerListItem>, { page?: number; pageSize?: number } | void>({
      query: (args) => ({ url: '/api/v1/admin/sellers', params: { page: args?.page ?? 1, pageSize: args?.pageSize ?? 25 } }),
      providesTags: (result) =>
        result
          ? [...result.items.map((s) => ({ type: 'Seller' as const, id: s.id })), { type: 'Seller' as const, id: 'LIST' }]
          : [{ type: 'Seller' as const, id: 'LIST' }],
    }),
    getSellerById: builder.query<SellerDetail, string>({
      query: (id) => `/api/v1/admin/sellers/${id}`,
      providesTags: (_r, _e, id) => [{ type: 'Seller', id }],
    }),
    setSellerApproval: builder.mutation<void, { sellerId: string; action: ProfileApprovalAction; reason?: string }>({
      query: ({ sellerId, ...body }) => ({ url: `/api/v1/admin/sellers/${sellerId}/approval`, method: 'POST', body }),
      invalidatesTags: (_r, _e, { sellerId }) => [{ type: 'Seller', id: sellerId }, { type: 'Seller', id: 'LIST' }, 'Approval', 'Dashboard'],
    }),
    setSellerKyc: builder.mutation<void, { sellerId: string; verified: boolean; notes?: string }>({
      query: ({ sellerId, ...body }) => ({ url: `/api/v1/admin/sellers/${sellerId}/kyc`, method: 'POST', body }),
      invalidatesTags: (_r, _e, { sellerId }) => [{ type: 'Seller', id: sellerId }],
    }),
    updateSeller: builder.mutation<void, { sellerId: string; tier?: SellerTier; tags?: string[]; phone?: string; whatsApp?: string; referenceContact?: string; tierNotes?: string }>({
      query: ({ sellerId, ...body }) => ({ url: `/api/v1/admin/sellers/${sellerId}`, method: 'PUT', body }),
      invalidatesTags: (_r, _e, { sellerId }) => [{ type: 'Seller', id: sellerId }, { type: 'Seller', id: 'LIST' }],
    }),
    deleteSeller: builder.mutation<void, string>({
      query: (id) => ({ url: `/api/v1/admin/sellers/${id}`, method: 'DELETE' }),
      invalidatesTags: [{ type: 'Seller', id: 'LIST' }, 'Dashboard'],
    }),

    // ---- Agencies ----
    getAgencies: builder.query<AgencyListItem[], void>({
      query: () => '/api/v1/admin/agencies',
      providesTags: (result) =>
        result
          ? [...result.map((a) => ({ type: 'Agency' as const, id: a.id })), { type: 'Agency' as const, id: 'LIST' }]
          : [{ type: 'Agency' as const, id: 'LIST' }],
    }),
    getAgencyById: builder.query<AgencyDetail, string>({
      query: (id) => `/api/v1/admin/agencies/${id}`,
      providesTags: (_r, _e, id) => [{ type: 'Agency', id }],
    }),
    createAgency: builder.mutation<{ id: string }, {
      userId: string; email: string; agencyName: string; ownerFullName: string; country: string
      city?: string; phone?: string; whatsApp?: string; commissionRate: number; teamSize?: number; notes?: string
    }>({
      query: (body) => ({ url: '/api/v1/admin/agencies', method: 'POST', body }),
      invalidatesTags: [{ type: 'Agency', id: 'LIST' }],
    }),
    updateAgency: builder.mutation<void, { agencyId: string; commissionRate?: number; isActive?: boolean }>({
      query: ({ agencyId, ...body }) => ({ url: `/api/v1/admin/agencies/${agencyId}`, method: 'PUT', body }),
      invalidatesTags: (_r, _e, { agencyId }) => [{ type: 'Agency', id: agencyId }, { type: 'Agency', id: 'LIST' }],
    }),
    setAgencyApproval: builder.mutation<void, { agencyId: string; action: ProfileApprovalAction; reason?: string }>({
      query: ({ agencyId, ...body }) => ({ url: `/api/v1/admin/agencies/${agencyId}/approval`, method: 'POST', body }),
      invalidatesTags: (_r, _e, { agencyId }) => [{ type: 'Agency', id: agencyId }, { type: 'Agency', id: 'LIST' }, 'Approval', 'Dashboard'],
    }),

    // ---- Requirements ----
    getRequirements: builder.query<PagedResult<RequirementListItem>, { status?: RequirementStatus; page?: number; pageSize?: number } | void>({
      query: (args) => ({ url: '/api/v1/admin/requirements', params: { status: args?.status, page: args?.page ?? 1, pageSize: args?.pageSize ?? 25 } }),
      providesTags: (result) =>
        result
          ? [...result.items.map((r) => ({ type: 'Requirement' as const, id: r.id })), { type: 'Requirement' as const, id: 'LIST' }]
          : [{ type: 'Requirement' as const, id: 'LIST' }],
    }),
    getBidBoard: builder.query<AdminBidBoardRow[], { requirementId?: string } | void>({
      query: (args) => ({ url: '/api/v1/admin/requirements/bid-board', params: args?.requirementId ? { requirementId: args.requirementId } : undefined }),
      providesTags: ['BidBoard'],
    }),
    getRequirementById: builder.query<RequirementDetail, string>({
      query: (id) => `/api/v1/admin/requirements/${id}`,
      providesTags: (_r, _e, id) => [{ type: 'Requirement', id }],
    }),
    updateRequirement: builder.mutation<void, {
      requirementId: string; itemName: string; quantityPcs: number; grade: GradeType
      destinationCountry: string; buyerTargetPriceUsd: number; adminNotes?: string
    }>({
      query: ({ requirementId, ...body }) => ({ url: `/api/v1/admin/requirements/${requirementId}`, method: 'PUT', body }),
      invalidatesTags: (_r, _e, { requirementId }) => [{ type: 'Requirement', id: requirementId }, { type: 'Requirement', id: 'LIST' }],
    }),
    deleteRequirement: builder.mutation<void, { requirementId: string; confirm?: boolean }>({
      query: ({ requirementId, confirm }) => ({ url: `/api/v1/admin/requirements/${requirementId}`, method: 'DELETE', params: { confirm: confirm ?? false } }),
      invalidatesTags: [{ type: 'Requirement', id: 'LIST' }, 'Dashboard'],
    }),
    postRequirementLive: builder.mutation<void, string>({
      query: (id) => ({ url: `/api/v1/admin/requirements/${id}/post-live`, method: 'POST' }),
      invalidatesTags: (_r, _e, id) => [{ type: 'Requirement', id }, { type: 'Requirement', id: 'LIST' }],
    }),
    setRequirementPublicDisplay: builder.mutation<void, { requirementId: string; public: boolean }>({
      query: ({ requirementId, public: isPublic }) => ({ url: `/api/v1/admin/requirements/${requirementId}/public-display`, method: 'POST', body: { public: isPublic } }),
      invalidatesTags: (_r, _e, { requirementId }) => [{ type: 'Requirement', id: requirementId }],
    }),

    // ---- Deals ----
    getDeals: builder.query<PagedResult<DealListItem>, { status?: DealStatus; page?: number; pageSize?: number } | void>({
      query: (args) => ({ url: '/api/v1/admin/deals', params: { status: args?.status, page: args?.page ?? 1, pageSize: args?.pageSize ?? 25 } }),
      providesTags: (result) =>
        result
          ? [...result.items.map((d) => ({ type: 'Deal' as const, id: d.id })), { type: 'Deal' as const, id: 'LIST' }]
          : [{ type: 'Deal' as const, id: 'LIST' }],
    }),
    getDealById: builder.query<DealDetail, string>({
      query: (id) => `/api/v1/admin/deals/${id}`,
      providesTags: (_r, _e, id) => [{ type: 'Deal', id }],
    }),
    createDealFromMatch: builder.mutation<{ id: string }, {
      requirementId: string; sellerId: string; finalQuantityPcs: number; buyerPricePerPcUsd: number
      sellerCostPerPcUsd: number; shippingCostUsd: number; sourceResponseId?: string | null
      sourceOfferId?: string | null; estimatedDispatchDate?: string | null; adminNotes?: string | null
    }>({
      query: (body) => ({ url: '/api/v1/admin/deals/from-match', method: 'POST', body }),
      invalidatesTags: [{ type: 'Deal', id: 'LIST' }, { type: 'Requirement', id: 'LIST' }, 'BidBoard', 'Dashboard'],
    }),
    advanceDealStatus: builder.mutation<void, { dealId: string; newStatus: DealStatus }>({
      query: ({ dealId, newStatus }) => ({ url: `/api/v1/admin/deals/${dealId}/advance-status`, method: 'POST', body: { newStatus } }),
      invalidatesTags: (_r, _e, { dealId }) => [{ type: 'Deal', id: dealId }, { type: 'Deal', id: 'LIST' }, 'Commission', 'Dashboard'],
    }),
    recordDealPayment: builder.mutation<void, { dealId: string; paymentMethod: string; paymentReference?: string }>({
      query: ({ dealId, ...body }) => ({ url: `/api/v1/admin/deals/${dealId}/payment`, method: 'POST', body }),
      invalidatesTags: (_r, _e, { dealId }) => [{ type: 'Deal', id: dealId }],
    }),
    setDealShipping: builder.mutation<void, {
      dealId: string; containerNumber?: string; containerSize?: string; shippingLine?: string; vesselName?: string; billOfLading?: string
    }>({
      query: ({ dealId, ...body }) => ({ url: `/api/v1/admin/deals/${dealId}/shipping`, method: 'POST', body }),
      invalidatesTags: (_r, _e, { dealId }) => [{ type: 'Deal', id: dealId }],
    }),
    setDealTracking: builder.mutation<void, { dealId: string; trackingNumber?: string; trackingUrl?: string; courier?: string }>({
      query: ({ dealId, ...body }) => ({ url: `/api/v1/admin/deals/${dealId}/tracking`, method: 'POST', body }),
      invalidatesTags: (_r, _e, { dealId }) => [{ type: 'Deal', id: dealId }],
    }),
    cancelDeal: builder.mutation<void, { dealId: string; reason: string }>({
      query: ({ dealId, reason }) => ({ url: `/api/v1/admin/deals/${dealId}/cancel`, method: 'POST', body: { reason } }),
      invalidatesTags: (_r, _e, { dealId }) => [{ type: 'Deal', id: dealId }, { type: 'Deal', id: 'LIST' }, { type: 'Requirement', id: 'LIST' }, 'Commission', 'Dashboard'],
    }),
    getDealAllocations: builder.query<DealAllocation[], string>({
      query: (dealId) => `/api/v1/admin/deals/${dealId}/allocations`,
      providesTags: (_r, _e, dealId) => [{ type: 'DealAllocation', id: dealId }],
    }),
    acceptSellerResponse: builder.mutation<{ dealId: string }, string>({
      query: (sellerResponseId) => ({ url: `/api/v1/admin/seller-responses/${sellerResponseId}/accept`, method: 'POST' }),
      invalidatesTags: [{ type: 'Deal', id: 'LIST' }, { type: 'Requirement', id: 'LIST' }, 'BidBoard', 'Dashboard'],
    }),

    // ---- Commissions ----
    getCommissions: builder.query<CommissionListItem[], { status?: CommissionStatus } | void>({
      query: (args) => ({ url: '/api/v1/admin/commissions', params: args?.status ? { status: args.status } : undefined }),
      providesTags: ['Commission'],
    }),
    getFeeRevenue: builder.query<PlatformFeeRevenueMonth[], void>({
      query: () => '/api/v1/admin/commissions/fee-revenue',
    }),

    // ---- Disputes ----
    getDisputes: builder.query<DisputeListItem[], { status?: DisputeStatus } | void>({
      query: (args) => ({ url: '/api/v1/admin/disputes', params: args?.status ? { status: args.status } : undefined }),
      providesTags: ['Dispute'],
    }),
    beginDisputeInvestigation: builder.mutation<void, string>({
      query: (id) => ({ url: `/api/v1/admin/disputes/${id}/investigate`, method: 'POST' }),
      invalidatesTags: ['Dispute'],
    }),
    resolveDispute: builder.mutation<void, { disputeId: string; resolution: string; resolutionNotes?: string; refundAmountUsd: number }>({
      query: ({ disputeId, ...body }) => ({ url: `/api/v1/admin/disputes/${disputeId}/resolve`, method: 'POST', body }),
      invalidatesTags: ['Dispute', { type: 'Deal', id: 'LIST' }],
    }),
    rejectDispute: builder.mutation<void, { disputeId: string; resolutionNotes?: string }>({
      query: ({ disputeId, ...body }) => ({ url: `/api/v1/admin/disputes/${disputeId}/reject`, method: 'POST', body }),
      invalidatesTags: ['Dispute', { type: 'Deal', id: 'LIST' }],
    }),

    // ---- Messages ----
    getMessageThreads: builder.query<MessageThreadListItem[], { unreadOnly?: boolean } | void>({
      query: (args) => ({ url: '/api/v1/admin/message-threads', params: args?.unreadOnly ? { unreadOnly: true } : undefined }),
      providesTags: ['MessageThread'],
    }),
    getThreadMessages: builder.query<AdminMessage[], string>({
      query: (threadId) => `/api/v1/admin/message-threads/${threadId}/messages`,
      providesTags: (_r, _e, threadId) => [{ type: 'Message', id: threadId }],
    }),
    sendAdminMessage: builder.mutation<{ id: string }, { threadId: string; body: string }>({
      query: ({ threadId, body }) => ({ url: `/api/v1/admin/message-threads/${threadId}/messages`, method: 'POST', body: { body } }),
      invalidatesTags: (_r, _e, { threadId }) => [{ type: 'Message', id: threadId }, 'MessageThread'],
    }),
    markThreadRead: builder.mutation<void, string>({
      query: (threadId) => ({ url: `/api/v1/admin/message-threads/${threadId}/read`, method: 'POST' }),
      invalidatesTags: ['MessageThread'],
    }),

    // ---- Offers ----
    getOffers: builder.query<OfferListItem[], { requirementId?: string; status?: OfferStatus } | void>({
      query: (args) => ({ url: '/api/v1/admin/offers', params: { requirementId: args?.requirementId, status: args?.status } }),
      providesTags: ['Offer'],
    }),
    getOfferRounds: builder.query<OfferRound[], string>({
      query: (offerId) => `/api/v1/admin/offers/${offerId}/rounds`,
      providesTags: (_r, _e, offerId) => [{ type: 'OfferRound', id: offerId }],
    }),
    createOffer: builder.mutation<{ id: string }, { requirementId: string; sellerId: string; offerPricePerPc: number }>({
      query: (body) => ({ url: '/api/v1/admin/offers', method: 'POST', body }),
      invalidatesTags: ['Offer'],
    }),
    postOfferRound: builder.mutation<void, { offerId: string; kind: 'counter' | 'message'; pricePerPcUsd?: number; notes?: string }>({
      query: ({ offerId, ...body }) => ({ url: `/api/v1/admin/offers/${offerId}/rounds`, method: 'POST', body }),
      invalidatesTags: (_r, _e, { offerId }) => ['Offer', { type: 'OfferRound', id: offerId }],
    }),
    acceptOffer: builder.mutation<void, string>({
      query: (offerId) => ({ url: `/api/v1/admin/offers/${offerId}/accept`, method: 'POST' }),
      invalidatesTags: ['Offer', { type: 'Deal', id: 'LIST' }],
    }),
    declineOffer: builder.mutation<void, { offerId: string; reason: string }>({
      query: ({ offerId, reason }) => ({ url: `/api/v1/admin/offers/${offerId}/decline`, method: 'POST', body: { reason } }),
      invalidatesTags: ['Offer'],
    }),
    deleteOffer: builder.mutation<void, string>({
      query: (offerId) => ({ url: `/api/v1/admin/offers/${offerId}`, method: 'DELETE' }),
      invalidatesTags: ['Offer'],
    }),

    // ---- Audit ----
    getAuditLog: builder.query<AuditLogEntry[], { take?: number } | void>({
      query: (args) => ({ url: '/api/v1/admin/audit-log', params: { take: args?.take ?? 100 } }),
      providesTags: ['Audit'],
    }),

    // ---- Settings: Categories ----
    getCategories: builder.query<Category[], void>({
      query: () => '/api/v1/admin/categories',
      providesTags: ['Category'],
    }),
    createCategory: builder.mutation<{ id: number }, { name: string; nameFr?: string; weightPerPieceKg?: number; displayOrder: number }>({
      query: (body) => ({ url: '/api/v1/admin/categories', method: 'POST', body }),
      invalidatesTags: ['Category'],
    }),
    updateCategory: builder.mutation<void, { categoryId: number; name: string; nameFr?: string; isActive: boolean }>({
      query: ({ categoryId, ...body }) => ({ url: `/api/v1/admin/categories/${categoryId}`, method: 'PUT', body }),
      invalidatesTags: ['Category'],
    }),

    // ---- Settings: Shipping rates ----
    getShippingRates: builder.query<ShippingRate[], void>({
      query: () => '/api/v1/admin/shipping-rates',
      providesTags: ['ShippingRate'],
    }),
    createShippingRate: builder.mutation<{ id: number }, { destinationCountry: string; rateUsdPerKg?: number; flatRateUsd?: number; transitDays?: number }>({
      query: (body) => ({ url: '/api/v1/admin/shipping-rates', method: 'POST', body }),
      invalidatesTags: ['ShippingRate'],
    }),
    updateShippingRate: builder.mutation<void, { shippingRateId: number; rateUsdPerKg?: number; flatRateUsd?: number; isActive: boolean }>({
      query: ({ shippingRateId, ...body }) => ({ url: `/api/v1/admin/shipping-rates/${shippingRateId}`, method: 'PUT', body }),
      invalidatesTags: ['ShippingRate'],
    }),
    deleteShippingRate: builder.mutation<void, number>({
      query: (id) => ({ url: `/api/v1/admin/shipping-rates/${id}`, method: 'DELETE' }),
      invalidatesTags: ['ShippingRate'],
    }),

    // ---- Settings: Exchange rates ----
    getExchangeRates: builder.query<ExchangeRate[], void>({
      query: () => '/api/v1/admin/exchange-rates',
      providesTags: ['ExchangeRate'],
    }),
    setExchangeRate: builder.mutation<{ id: number }, { currency: CurrencyType; rateToUsd: number; notes?: string }>({
      query: (body) => ({ url: '/api/v1/admin/exchange-rates', method: 'POST', body }),
      invalidatesTags: ['ExchangeRate'],
    }),

    // ---- Settings: Influencers ----
    getInfluencers: builder.query<InfluencerListItem[], void>({
      query: () => '/api/v1/admin/influencers',
      providesTags: ['Influencer'],
    }),
    createInfluencer: builder.mutation<{ id: string }, {
      fullName: string; email: string; phone?: string; instagram?: string; tiktok?: string
      referralCode?: string; commissionRate: number; userId?: string
    }>({
      query: (body) => ({ url: '/api/v1/admin/influencers', method: 'POST', body }),
      invalidatesTags: ['Influencer', 'PartnerApplication'],
    }),

    // ---- Settings: Partner applications ----
    getPartnerApplications: builder.query<PartnerApplication[], { status?: string } | void>({
      query: (args) => ({ url: '/api/v1/admin/partner-applications', params: args?.status ? { status: args.status } : undefined }),
      providesTags: ['PartnerApplication'],
    }),
    setPartnerApplicationStatus: builder.mutation<void, { applicationId: string; approve: boolean; influencerId?: string }>({
      query: ({ applicationId, ...body }) => ({ url: `/api/v1/admin/partner-applications/${applicationId}/status`, method: 'POST', body }),
      invalidatesTags: ['PartnerApplication'],
    }),

    // ---- Settings: Platform fee config ----
    getFeeConfig: builder.query<PlatformFeeConfig, void>({
      query: () => '/api/v1/admin/settings/fee-config',
      providesTags: ['FeeConfig'],
    }),
    updateFeeConfig: builder.mutation<void, { feePerPcUsd: number; pkrReference: string }>({
      query: (body) => ({ url: '/api/v1/admin/settings/fee-config', method: 'PUT', body }),
      invalidatesTags: ['FeeConfig'],
    }),
  }),
})

export const {
  useGetDashboardStatsQuery,
  useGetUsersQuery, useCreateUserMutation, useUpdateUserMutation, useDeleteUserMutation,
  useGetPendingApprovalsQuery,
  useGetBuyersQuery, useGetBuyerByIdQuery, useSetBuyerApprovalMutation, useUpdateBuyerMutation, useDeleteBuyerMutation,
  useGetSellersQuery, useGetSellerByIdQuery, useSetSellerApprovalMutation, useSetSellerKycMutation, useUpdateSellerMutation, useDeleteSellerMutation,
  useGetAgenciesQuery, useGetAgencyByIdQuery, useCreateAgencyMutation, useUpdateAgencyMutation, useSetAgencyApprovalMutation,
  useGetRequirementsQuery, useGetBidBoardQuery, useGetRequirementByIdQuery, useUpdateRequirementMutation, useDeleteRequirementMutation,
  usePostRequirementLiveMutation, useSetRequirementPublicDisplayMutation,
  useGetDealsQuery, useGetDealByIdQuery, useCreateDealFromMatchMutation, useAdvanceDealStatusMutation,
  useRecordDealPaymentMutation, useSetDealShippingMutation, useSetDealTrackingMutation, useCancelDealMutation,
  useGetDealAllocationsQuery, useAcceptSellerResponseMutation,
  useGetCommissionsQuery, useGetFeeRevenueQuery,
  useGetDisputesQuery, useBeginDisputeInvestigationMutation, useResolveDisputeMutation, useRejectDisputeMutation,
  useGetMessageThreadsQuery, useGetThreadMessagesQuery, useSendAdminMessageMutation, useMarkThreadReadMutation,
  useGetOffersQuery, useGetOfferRoundsQuery, useCreateOfferMutation, usePostOfferRoundMutation,
  useAcceptOfferMutation, useDeclineOfferMutation, useDeleteOfferMutation,
  useGetAuditLogQuery,
  useGetCategoriesQuery, useCreateCategoryMutation, useUpdateCategoryMutation,
  useGetShippingRatesQuery, useCreateShippingRateMutation, useUpdateShippingRateMutation, useDeleteShippingRateMutation,
  useGetExchangeRatesQuery, useSetExchangeRateMutation,
  useGetInfluencersQuery, useCreateInfluencerMutation,
  useGetPartnerApplicationsQuery, useSetPartnerApplicationStatusMutation,
  useGetFeeConfigQuery, useUpdateFeeConfigMutation,
} = adminApi
