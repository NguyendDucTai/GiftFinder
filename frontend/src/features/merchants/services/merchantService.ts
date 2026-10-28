import { api } from '../../../core/api/client';

export interface MerchantDashboardStats {
  shopName: string;
  isVerified: boolean;
  totalProducts: number;
  approvedProducts: number;
  pendingProducts: number;
  rejectedProducts: number;
  featuredProducts: number;
  totalClicks: number;
  topProducts: Array<{
    productId: string;
    productName: string;
    price: number;
    clickCount: number;
    isFeatured: boolean;
  }>;
}

export interface CreateMerchantProductRequest {
  name: string;
  description?: string;
  price: number;
  originalPrice?: number;
  imageUrl?: string;
  originalUrl: string;
  customTagNames?: string[];
}

export interface MerchantProductItem {
  id: string;
  name: string;
  description?: string;
  price: number;
  originalPrice?: number;
  imageUrl?: string;
  status: string;
  rejectReason?: string;
  isFeatured: boolean;
  featuredUntil?: string;
  clickCount: number;
  createdAt: string;
}

export const merchantService = {
  async getDashboardStats(): Promise<MerchantDashboardStats> {
    const res = await api.get<MerchantDashboardStats>('/Merchants/dashboard-stats');
    return res.data;
  },

  async getMyProducts(pageNumber = 1, pageSize = 20): Promise<{ items: MerchantProductItem[]; totalCount: number }> {
    const res = await api.get('/Merchants/my-products', {
      params: { pageNumber, pageSize },
    });
    return res.data;
  },

  async createProduct(data: CreateMerchantProductRequest): Promise<any> {
    const res = await api.post('/Merchants/products', data);
    return res.data;
  },

  async buyFeatured(productId: string, planType: 0 | 1 = 0): Promise<{ paymentUrl: string; transactionId: string }> {
    const res = await api.post(`/Merchants/products/${productId}/buy-featured`, {
      planType,
    });
    return res.data;
  },

  async simulatePayment(productId: string, transactionId: string): Promise<any> {
    const res = await api.post(`/Merchants/products/${productId}/test-pay-featured/${transactionId}`);
    return res.data;
  },
};

