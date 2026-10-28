import { api } from '../../../core/api/client';

export interface ProductRecommendationDto {
  id: string;
  name: string;
  price: number;
  imageUrl?: string;
  affiliateUrl?: string;
  originalUrl: string;
  rating: number;
  totalReviews: number;
  aiAdvice?: string;
  isFeatured?: boolean;
  badgeLabel?: string;
}

export interface PagedRecommendationResult {
  items: ProductRecommendationDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  isValidIntent: boolean;
  message?: string;
  interpretedIntent?: {
    relationship?: string;
    occasion?: string;
    targetAge?: number;
    targetZodiac?: string;
    minBudget?: number;
    maxBudget?: number;
    budgetType?: string;
    hobbies?: string[];
    interestKeywords?: string[];
    summary?: string;
    isValidGiftIntent?: boolean;
    aiMessage?: string;
    suggestedQuestions?: string[];
  };
}

export const recommendationService = {
  async getRecommendations(prompt: string, pageSize: number = 6): Promise<PagedRecommendationResult> {
    const res = await api.get<PagedRecommendationResult>('/Recommendations', {
      params: { Prompt: prompt, PageSize: pageSize },
    });
    return res.data;
  },
};
