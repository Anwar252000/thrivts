import { configureStore } from '@reduxjs/toolkit'
import authReducer from '@/features/auth/authSlice'
import { adminApi } from '@/features/admin/adminApi'
import { buyerApi } from '@/features/buyer/buyerApi'
import { sellerApi } from '@/features/seller/sellerApi'

export const store = configureStore({
  reducer: {
    auth: authReducer,
    [adminApi.reducerPath]: adminApi.reducer,
    [buyerApi.reducerPath]: buyerApi.reducer,
    [sellerApi.reducerPath]: sellerApi.reducer,
  },
  middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(adminApi.middleware, buyerApi.middleware, sellerApi.middleware),
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
