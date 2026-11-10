import { api } from '../../../core/api/client';

export interface MerchantProfile {
  id: string;
  userId: string;
  shopName: string;
  contactEmail: string;
  contactPhone: string;
  businessLicense?: string;
  pickupProvince?: string;
  pickupAddress?: string;
  isVerified: boolean;
  verifiedAt?: string;
  createdAt: string;
  token?: string;
}

export interface MerchantDailyChartPoint {
  dateLabel: string;
  fullDate: string;
  clicks: number;
  orders: number;
  revenue: number;
}

export interface MerchantOrderItem {
  id: string;
  productId: string;
  productName: string;
  productImageUrl?: string;
  variant: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface MerchantOrder {
  id: string;
  orderCode: string;
  merchantId: string;
  shopName: string;
  shopPickupProvince: string;
  receiverName: string;
  receiverPhone: string;
  receiverProvince: string;
  receiverDistrict: string;
  receiverAddress: string;
  deliveryNote?: string;
  hasGiftWrap: boolean;
  giftMessage?: string;
  hidePriceOnPackage: boolean;
  subtotal: number;
  shippingFee: number;
  giftWrapFee: number;
  discountAmount: number;
  totalAmount: number;
  paymentMethod: string;
  shippingMethod: string; // 'Standard' | 'Express2H'
  shippingCarrier?: string;
  trackingCode?: string;
  status: string; // 'Pending' | 'Packing' | 'Shipping' | 'Completed' | 'Cancelled'
  createdAt: string;
  packedAt?: string;
  shippedAt?: string;
  completedAt?: string;
  cancelReason?: string;
  items: MerchantOrderItem[];
}

export interface MerchantDashboardStats {
  merchantId?: string;
  shopName: string;
  shopSlug?: string;
  pickupProvince?: string;
  pickupAddress?: string;
  isVerified: boolean;
  totalProducts: number;
  approvedProducts: number;
  pendingProducts: number;
  rejectedProducts: number;
  featuredProducts: number;
  totalClicks: number;

  // Thống kê Đơn hàng & Doanh thu COD
  totalOrders?: number;
  pendingOrders?: number;
  shippingOrders?: number;
  completedOrders?: number;
  cancelledOrders?: number;
  completedRevenue?: number;
  pendingRevenue?: number;
  averageOrderValue?: number;
  conversionRate?: number;

  dailyChart?: MerchantDailyChartPoint[];
  recentOrders?: MerchantOrder[];

  topProducts: Array<{
    productId: string;
    productName: string;
    price: number;
    clickCount: number;
    soldCount?: number;
    revenue?: number;
    isFeatured: boolean;
  }>;
}

export interface CreateMerchantProductRequest {
  name: string;
  description?: string;
  price: number;
  originalPrice?: number;
  imageUrl?: string;
  originalUrl?: string;
  stockQuantity?: number;
  tagIds?: string[];
  customTagNames?: string[];
  suitableZodiacs?: string[];
}

export interface UpdateMerchantProductRequest extends CreateMerchantProductRequest {
  id?: string;
}

export interface MerchantProductItem {
  id: string;
  name: string;
  description?: string;
  price: number;
  originalPrice?: number;
  imageUrl?: string;
  originalUrl: string;
  status: string;
  rejectReason?: string;
  stockQuantity?: number;
  isFeatured: boolean;
  featuredUntil?: string;
  clickCount: number;
  soldCount?: number;
  createdAt: string;
  tagIds?: string[];
  tagNames?: string[];
  suitableZodiacs?: string[];
}

export interface MerchantProductDetail extends MerchantProductItem {
  source: string;
}

export interface SuggestedTagItem {
  tagId: string;
  name: string;
  type: string;
}

export interface AiProductTagSuggestion {
  productName: string;
  suitableZodiacs: string[];
  zodiacRationale: string;
  suggestedTags: SuggestedTagItem[];
  suggestedTagIds: string[];
}

export interface TagDtoItem {
  id: string;
  name: string;
  slug: string;
  type: string | number;
  typeName: string;
  description?: string;
}

export interface BuyFeaturedResponse {
  transactionId: string;
  productId: string;
  productName: string;
  planType: string;
  amount: number;
  paymentMethod: string;
  paymentUrl: string;
  isFeatured: boolean;
  featuredUntil?: string;
  message: string;
}

export interface CheckoutMerchantOrderRequest {
  receiverName: string;
  receiverPhone: string;
  receiverProvince: string;
  receiverDistrict: string;
  receiverAddress: string;
  deliveryNote?: string;
  hasGiftWrap: boolean;
  giftMessage?: string;
  hidePriceOnPackage: boolean;
  shippingMethod: 'Standard' | 'Express2H';
  applyVoucher?: boolean;
  items: Array<{
    productId?: string;
    productName: string;
    productImageUrl?: string;
    variant: string;
    unitPrice: number;
    quantity: number;
  }>;
}

export const merchantService = {
  async register(data: {
    shopName: string;
    contactPhone: string;
    contactEmail?: string;
    businessLicense?: string;
    pickupProvince?: string;
    pickupAddress?: string;
  }): Promise<MerchantProfile> {
    const res = await api.post<MerchantProfile>('/Merchants/register', data);
    return res.data;
  },

  async getProfile(): Promise<MerchantProfile> {
    const res = await api.get<MerchantProfile>('/Merchants/profile');
    return res.data;
  },

  async updateProfile(data: {
    shopName: string;
    contactEmail: string;
    contactPhone: string;
    businessLicense?: string;
    pickupProvince?: string;
    pickupAddress?: string;
  }): Promise<MerchantProfile> {
    const res = await api.put<MerchantProfile>('/Merchants/profile', data);
    return res.data;
  },

  async getDashboardStats(days = 7): Promise<MerchantDashboardStats> {
    const res = await api.get<MerchantDashboardStats>('/Merchants/dashboard-stats', {
      params: { days },
    });
    return res.data;
  },

  async getOrders(status?: string): Promise<MerchantOrder[]> {
    const res = await api.get<MerchantOrder[]>('/Merchants/orders', {
      params: status && status !== 'All' ? { status } : undefined,
    });
    return res.data;
  },

  async updateOrderStatus(
    orderId: string,
    data: {
      targetStatus: 'Packing' | 'Shipping' | 'Completed' | 'Cancelled';
      shippingCarrier?: string;
      trackingCode?: string;
      cancelReason?: string;
    }
  ): Promise<MerchantOrder> {
    const res = await api.put<MerchantOrder>(`/Merchants/orders/${orderId}/status`, data);
    return res.data;
  },

  async checkoutOrder(data: CheckoutMerchantOrderRequest): Promise<MerchantOrder[]> {
    const res = await api.post<MerchantOrder[]>('/Merchants/orders/checkout', data);
    return res.data;
  },

  async suggestTagsAi(data: {
    productName: string;
    description?: string;
    price: number;
  }): Promise<AiProductTagSuggestion> {
    const res = await api.post<AiProductTagSuggestion>('/Merchants/ai-suggest-tags', data);
    return res.data;
  },

  async getTags(type?: string | number): Promise<TagDtoItem[]> {
    const res = await api.get<TagDtoItem[]>('/Tags', {
      params: type !== undefined ? { type } : undefined,
    });
    return res.data;
  },

  async getMyProducts(
    pageNumber = 1,
    pageSize = 50,
    status?: string
  ): Promise<{ items: MerchantProductItem[]; totalCount: number; pageNumber: number; pageSize: number }> {
    const res = await api.get('/Merchants/my-products', {
      params: {
        pageNumber,
        pageSize,
        ...(status && status !== 'All' ? { status } : {}),
      },
    });
    return res.data;
  },

  async getProductById(id: string): Promise<MerchantProductDetail> {
    const res = await api.get<MerchantProductDetail>(`/Merchants/products/${id}`);
    return res.data;
  },

  async createProduct(data: CreateMerchantProductRequest): Promise<MerchantProductDetail> {
    const res = await api.post<MerchantProductDetail>('/Merchants/products', data);
    return res.data;
  },

  async updateProduct(id: string, data: UpdateMerchantProductRequest): Promise<MerchantProductDetail> {
    const res = await api.put<MerchantProductDetail>(`/Merchants/products/${id}`, {
      ...data,
      id,
    });
    return res.data;
  },

  async deleteProduct(id: string): Promise<{ success: boolean; message: string }> {
    const res = await api.delete(`/Merchants/products/${id}`);
    return res.data;
  },

  async buyFeatured(
    productId: string,
    planType: 0 | 1 | 'Weekly' | 'Monthly' = 0
  ): Promise<BuyFeaturedResponse> {
    const res = await api.post<BuyFeaturedResponse>(`/Merchants/products/${productId}/buy-featured`, {
      planType,
    });
    return res.data;
  },

  async simulatePayment(
    productId: string,
    transactionId: string
  ): Promise<{ success: boolean; message: string }> {
    const res = await api.post(`/Merchants/products/${productId}/test-pay-featured/${transactionId}`);
    return res.data;
  },

  async cancelFeatured(
    productId: string
  ): Promise<{ productId: string; isFeatured: boolean; message: string }> {
    const res = await api.delete(`/Merchants/products/${productId}/cancel-featured`);
    return res.data;
  },
};
