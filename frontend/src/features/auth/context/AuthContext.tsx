import React, { createContext, useContext, useEffect, useState, useMemo } from 'react';
import { authService } from '../services/authService';
import type {
  AuthResponse,
  LoginRequest,
  RegisterMerchantRequest,
  RegisterRequest,
  User,
} from '../types/auth';

interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isAdmin: boolean;
  isMerchant: boolean;
  isMember: boolean;
  login: (req: LoginRequest, remember?: boolean) => Promise<AuthResponse>;
  register: (req: RegisterRequest, remember?: boolean) => Promise<AuthResponse>;
  registerMerchant: (req: RegisterMerchantRequest, remember?: boolean) => Promise<AuthResponse>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    // Initial check from storage
    const storedToken = authService.getStoredToken();
    const storedUser = authService.getStoredUser();

    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(storedUser);
    }
    setIsLoading(false);

    // Listen for unauthorized 401 events from Axios client
    const handleUnauthorized = () => {
      setUser(null);
      setToken(null);
    };

    window.addEventListener('auth:unauthorized', handleUnauthorized);
    return () => {
      window.removeEventListener('auth:unauthorized', handleUnauthorized);
    };
  }, []);

  const login = async (req: LoginRequest, remember: boolean = true): Promise<AuthResponse> => {
    const res = await authService.login(req, remember);
    setToken(res.token);
    setUser({
      id: res.id,
      email: res.email,
      fullName: res.fullName,
      role: res.role,
    });
    return res;
  };

  const register = async (req: RegisterRequest, remember: boolean = true): Promise<AuthResponse> => {
    const res = await authService.register(req, remember);
    setToken(res.token);
    setUser({
      id: res.id,
      email: res.email,
      fullName: res.fullName,
      role: res.role,
    });
    return res;
  };

  const registerMerchant = async (
    req: RegisterMerchantRequest,
    remember: boolean = true
  ): Promise<AuthResponse> => {
    const res = await authService.registerMerchant(req, remember);
    setToken(res.token);
    setUser({
      id: res.id,
      email: res.email,
      fullName: res.fullName,
      role: res.role,
      shopName: req.shopName,
    });
    return res;
  };

  const logout = () => {
    authService.logout();
    setToken(null);
    setUser(null);
  };

  const roleNormalized = (user?.role?.toString() || '').toLowerCase();
  const isAdmin = roleNormalized === 'admin' || user?.role === 0;
  const isMerchant = roleNormalized === 'merchant' || user?.role === 2;
  const isMember = roleNormalized === 'member' || user?.role === 1;

  const value = useMemo(
    () => ({
      user,
      token,
      isAuthenticated: !!token && !!user,
      isLoading,
      isAdmin,
      isMerchant,
      isMember,
      login,
      register,
      registerMerchant,
      logout,
    }),
    [user, token, isLoading, isAdmin, isMerchant, isMember]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

