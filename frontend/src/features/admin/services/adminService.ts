import { api } from '../../../core/api/client';
import type { ProductDto } from '../../products/types/product';

export interface PendingProductsResponse {
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  items: ProductDto[];
}

export const adminService = {
  async getPendingProducts(pageNumber = 1, pageSize = 20): Promise<PendingProductsResponse> {
    const res = await api.get<PendingProductsResponse>('/Admin/products/pending', {
      params: { pageNumber, pageSize },
    });
    return res.data;
  },

  async approveProduct(id: string): Promise<boolean> {
    const res = await api.post(`/Admin/products/${id}/approve`);
    return res.data;
  },

  async rejectProduct(id: string, reason: string): Promise<boolean> {
    const res = await api.post(`/Admin/products/${id}/reject`, {
      productId: id,
      reason,
    });
    return res.data;
  },

  async syncTrackedPrices(fastMode = true): Promise<any> {
    const res = await api.post('/Admin/sync-tracked-prices', null, {
      params: { fastMode },
    });
    return res.data;
  },
};

