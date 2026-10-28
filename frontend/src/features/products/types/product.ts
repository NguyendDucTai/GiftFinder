export interface TagDto {
  id: string;
  name: string;
  type: string;
}

export interface ProductDto {
  id: string;
  name: string;
  description?: string;
  price: number;
  originalPrice: number;
  imageUrl: string;
  originalUrl?: string;
  affiliateUrl?: string;
  source: 'Shopee' | 'TikTokShop' | 'Lazada' | 'Tiki' | 'DirectMerchant' | string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Inactive' | string;
  rejectReason?: string;
  rating: number;
  totalReviews: number;
  createdAt?: string;
  tags?: TagDto[];
}

export interface PagedProductsResponse {
  total: number;
  page: number;
  pageSize: number;
  items: ProductDto[];
}

export interface GetProductsParams {
  keyword?: string;
  source?: number;
  status?: number;
  page?: number;
  pageSize?: number;
}

