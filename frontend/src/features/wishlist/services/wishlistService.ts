import { api } from '../../../core/api/client';

export interface WishlistItemDto {
  id: string;
  productId: string;
  productName: string;
  imageUrl?: string;
  price: number;
  originalPrice?: number;
  originalUrl?: string;
  affiliateUrl?: string;
  targetPrice?: number;
  note?: string;
  addedAt: string;
  isAvailable?: boolean;
}

export const wishlistService = {
  async getWishlist(): Promise<WishlistItemDto[]> {
    const res = await api.get<WishlistItemDto[]>('/Wishlists');
    return res.data;
  },

  async addToWishlist(productId: string): Promise<any> {
    const res = await api.post('/Wishlists', { productId });
    return res.data;
  },

  async removeFromWishlist(productId: string): Promise<any> {
    const res = await api.delete(`/Wishlists/${productId}`);
    return res.data;
  },
};
