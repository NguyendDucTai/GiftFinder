import { api } from '../../../core/api/client';

export interface CatalogProductItemDto {
  id: string;
  name: string;
  description?: string;
  price: number;
  originalPrice?: number;
  imageUrl?: string;
  originalUrl: string;
  affiliateUrl?: string;
  source: string;
  rating: number;
  totalReviews: number;
  isFeatured: boolean;
  tags: string[];
}

export interface CatalogCategoryInfoDto {
  slug: string;
  title: string;
  group: string;
}

export interface CatalogProductsResultDto {
  category: CatalogCategoryInfoDto;
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
  items: CatalogProductItemDto[];
}

export interface CatalogCategoryCountDto {
  slug: string;
  title: string;
  group: string;
  count: number;
}

export interface GetCatalogProductsParams {
  category?: string;
  keyword?: string;
  page?: number;
  pageSize?: number;
  minPrice?: number;
  maxPrice?: number;
  sort?: string;
  source?: string;
}

export const catalogService = {
  async getCatalogProducts(params: GetCatalogProductsParams = {}): Promise<CatalogProductsResultDto> {
    const res = await api.get<CatalogProductsResultDto>('/Catalog/products', {
      params: {
        category: params.category || 'tat-ca',
        keyword: params.keyword || undefined,
        page: params.page || 1,
        pageSize: params.pageSize || 24,
        minPrice: params.minPrice || undefined,
        maxPrice: params.maxPrice || undefined,
        sort: params.sort || 'relevance',
        source: params.source || undefined,
      },
    });
    return res.data;
  },

  async getCategories(): Promise<CatalogCategoryCountDto[]> {
    const res = await api.get<CatalogCategoryCountDto[]>('/Catalog/categories');
    return res.data;
  },

  async getRelatedProducts(productId: string, limit = 10): Promise<CatalogProductItemDto[]> {
    const res = await api.get<CatalogProductItemDto[]>(`/Catalog/related/${productId}`, {
      params: { limit },
    });
    return res.data;
  },
};

