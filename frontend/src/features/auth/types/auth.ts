export const UserRole = {
  Admin: 0,
  Member: 1,
  Merchant: 2,
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: string | number;
  avatarUrl?: string;
  tier?: 'Free' | 'Premium';
  premiumExpiresAt?: string;
  shopName?: string;
}

export interface AuthResponse {
  id: string;
  email: string;
  fullName: string;
  role: string;
  token: string;
}

export interface ApiResponse<T> {
  success?: boolean;
  message?: string;
  data?: T;
  errors?: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

export interface RegisterMerchantRequest {
  email: string;
  password: string;
  fullName: string;
  shopName: string;
  contactPhone: string;
  businessLicense?: string;
}
