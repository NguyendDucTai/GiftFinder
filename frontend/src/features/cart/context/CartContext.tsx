import React, { createContext, useContext, useEffect, useState, useMemo, useCallback } from 'react';

export interface CartItem {
  id: string; // unique cart entry key
  productId: string;
  name: string;
  price: number;
  image: string;
  quantity: number;
  variant: string;
  platform: string;
  originalUrl?: string;
  affiliateUrl?: string;
  merchantId?: string;
  merchantName?: string;
  merchantProvince?: string;
  stockQuantity?: number;
}

interface CartContextType {
  items: CartItem[];
  cartCount: number;
  subtotal: number;
  shippingFee: number;
  giftWrapFee: number;
  hasGiftWrap: boolean;
  setHasGiftWrap: (val: boolean) => void;
  giftMessage: string;
  setGiftMessage: (val: string) => void;
  hidePriceOnPackage: boolean;
  setHidePriceOnPackage: (val: boolean) => void;
  total: number;
  addToCart: (
    product: {
      id?: string;
      name: string;
      price: number;
      image?: string;
      platform?: string;
      originalUrl?: string;
      affiliateUrl?: string;
      merchantId?: string;
      merchantName?: string;
      merchantProvince?: string;
      stockQuantity?: number;
    },
    quantity?: number,
    variant?: string
  ) => void;
  updateQuantity: (itemId: string, quantity: number) => void;
  removeFromCart: (itemId: string) => void;
  clearCart: () => void;
}

const CART_STORAGE_KEY = 'giftfinder_cart_items';

const CartContext = createContext<CartContextType | undefined>(undefined);

export const CartProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [items, setItems] = useState<CartItem[]>(() => {
    try {
      const raw = localStorage.getItem(CART_STORAGE_KEY);
      if (!raw) return [];
      const parsed: CartItem[] = JSON.parse(raw);
      return Array.isArray(parsed)
        ? parsed.filter((i) => !i.platform || i.platform.toUpperCase() === 'DIRECT')
        : [];
    } catch {
      return [];
    }
  });

  const [hasGiftWrap, setHasGiftWrap] = useState<boolean>(true);
  const [giftMessage, setGiftMessage] = useState<string>('Chúc bạn luôn rạng rỡ, hạnh phúc và ngập tràn niềm vui! 💕');
  const [hidePriceOnPackage, setHidePriceOnPackage] = useState<boolean>(true);

  // Sync to localStorage
  useEffect(() => {
    try {
      localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
    } catch (e) {
      console.warn('Failed to save cart to localStorage:', e);
    }
  }, [items]);

  const addToCart = useCallback(
    (
      product: {
        id?: string;
        name: string;
        price: number;
        image?: string;
        platform?: string;
        originalUrl?: string;
        affiliateUrl?: string;
        merchantId?: string;
        merchantName?: string;
        merchantProvince?: string;
        stockQuantity?: number;
      },
      quantity: number = 1,
      variant: string = 'Mặc định'
    ) => {
      const pform = (product.platform || 'DIRECT').toUpperCase();
      if (pform !== 'DIRECT') {
        console.warn('Sản phẩm sàn đối tác (Shopee/TikTok) chỉ mua trực tiếp trên sàn.');
        return;
      }
      const pId = product.id || product.name;
      const entryId = `${pId}_${variant}`;

      setItems((prev) => {
        const existingIdx = prev.findIndex((i) => i.id === entryId);
        if (existingIdx >= 0) {
          const updated = [...prev];
          updated[existingIdx].quantity += quantity;
          return updated;
        } else {
          const newItem: CartItem = {
            id: entryId,
            productId: pId,
            name: product.name,
            price: product.price,
            image: product.image || 'https://images.unsplash.com/photo-1603905179139-db12ab535ca9?auto=format&fit=crop&w=400',
            quantity: Math.max(1, quantity),
            variant,
            platform: product.platform || 'DIRECT',
            originalUrl: product.originalUrl,
            affiliateUrl: product.affiliateUrl,
            merchantId: product.merchantId,
            merchantName: product.merchantName,
            merchantProvince: product.merchantProvince,
            stockQuantity: product.stockQuantity,
          };
          return [newItem, ...prev];
        }
      });
    },
    []
  );

  const updateQuantity = useCallback((itemId: string, quantity: number) => {
    setItems((prev) => {
      if (quantity <= 0) {
        return prev.filter((i) => i.id !== itemId);
      }
      return prev.map((i) => (i.id === itemId ? { ...i, quantity } : i));
    });
  }, []);

  const removeFromCart = useCallback((itemId: string) => {
    setItems((prev) => prev.filter((i) => i.id !== itemId));
  }, []);

  const clearCart = useCallback(() => {
    setItems([]);
  }, []);

  const cartCount = useMemo(() => items.reduce((sum, item) => sum + item.quantity, 0), [items]);

  const subtotal = useMemo(() => items.reduce((sum, item) => sum + item.price * item.quantity, 0), [items]);

  const shippingFee = useMemo(() => (items.length > 0 ? (subtotal >= 500000 ? 0 : 25000) : 0), [items, subtotal]);

  const giftWrapFee = useMemo(() => (hasGiftWrap && items.length > 0 ? 30000 : 0), [hasGiftWrap, items]);

  const total = useMemo(() => subtotal + shippingFee + giftWrapFee, [subtotal, shippingFee, giftWrapFee]);

  const value = useMemo(
    () => ({
      items,
      cartCount,
      subtotal,
      shippingFee,
      giftWrapFee,
      hasGiftWrap,
      setHasGiftWrap,
      giftMessage,
      setGiftMessage,
      hidePriceOnPackage,
      setHidePriceOnPackage,
      total,
      addToCart,
      updateQuantity,
      removeFromCart,
      clearCart,
    }),
    [
      items,
      cartCount,
      subtotal,
      shippingFee,
      giftWrapFee,
      hasGiftWrap,
      giftMessage,
      hidePriceOnPackage,
      total,
      addToCart,
      updateQuantity,
      removeFromCart,
      clearCart,
    ]
  );

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
};

export const useCart = (): CartContextType => {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error('useCart must be used within a CartProvider');
  }
  return context;
};

