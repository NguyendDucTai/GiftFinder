import { api } from '../../../core/api/client';
import type { ProductDto, PagedProductsResponse, GetProductsParams } from '../types/product';

export const productService = {
  // Lấy danh sách sản phẩm phân trang, lọc từ Database
  async getProducts(params: GetProductsParams = {}): Promise<PagedProductsResponse> {
    const response = await api.get<PagedProductsResponse>('/Products', { params });
    return response.data;
  },

  // Lấy chi tiết 1 sản phẩm theo ID
  async getProductById(id: string): Promise<ProductDto> {
    const response = await api.get<ProductDto>(`/Products/${id}`);
    return response.data;
  },

  // Ghi nhận click affiliate và nhận URL chuyển hướng sang sàn
  async trackAffiliateClick(id: string): Promise<string> {
    try {
      const response = await api.post<{ url: string }>(`/Products/${id}/click-affiliate`);
      return response.data.url;
    } catch (error) {
      console.warn('Affiliate tracking failed, fallback to direct open:', error);
      return '';
    }
  },

  // Bật thông báo theo dõi khi giảm giá
  async trackPriceDrop(productId: string): Promise<{ message?: string }> {
    const response = await api.post<{ message?: string }>(`/price-tracking/${productId}`);
    return response.data;
  },

  // Lấy lịch sử giá thật của sản phẩm
  async getPriceHistory(productId: string): Promise<{
    productId: string;
    productName: string;
    currentPrice: number;
    lowestPrice: number;
    highestPrice: number;
    history: Array<{ oldPrice: number; newPrice: number; changedAt: string }>;
  }> {
    const response = await api.get(`/price-tracking/${productId}/history`);
    return response.data;
  },
};
