import { api } from '../../../core/api/client';
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  RegisterMerchantRequest,
  User,
} from '../types/auth';

const TOKEN_KEY = 'token';
const USER_KEY = 'user';

export const authService = {
  async login(credentials: LoginRequest, remember: boolean = true): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/Auth/login', credentials);
    authService.setStoredAuth(response.data, remember);
    return response.data;
  },

  async register(data: RegisterRequest, remember: boolean = true): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/Auth/register', data);
    authService.setStoredAuth(response.data, remember);
    return response.data;
  },

  async registerMerchant(
    data: RegisterMerchantRequest,
    remember: boolean = true
  ): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/Auth/register-merchant', data);
    authService.setStoredAuth(response.data, remember);
    return response.data;
  },

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
  },

  getStoredToken(): string | null {
    return localStorage.getItem(TOKEN_KEY) || sessionStorage.getItem(TOKEN_KEY);
  },

  getStoredUser(): User | null {
    const raw = localStorage.getItem(USER_KEY) || sessionStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  },

  setStoredAuth(auth: AuthResponse, remember: boolean = true): void {
    const user: User = {
      id: auth.id,
      email: auth.email,
      fullName: auth.fullName,
      role: auth.role,
    };
    const storage = remember ? localStorage : sessionStorage;
    storage.setItem(TOKEN_KEY, auth.token);
    storage.setItem(USER_KEY, JSON.stringify(user));
  },
};

