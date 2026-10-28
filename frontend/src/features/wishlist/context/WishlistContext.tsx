import React, { createContext, useContext, useEffect, useState, useCallback, useMemo } from 'react';
import { wishlistService, type WishlistItemDto } from '../services/wishlistService';
import { useAuth } from '../../auth/context/AuthContext';
import type { ProductItem } from '../../../App';

interface WishlistContextType {
  wishlistItems: WishlistItemDto[];
  wishlistCount: number;
  isLoading: boolean;
  isWishlisted: (productIdOrName: string) => boolean;
  toggleWishlist: (product: ProductItem) => Promise<boolean>;
  removeFromWishlist: (productIdOrName: string) => Promise<void>;
  refreshWishlist: () => Promise<void>;
}

const GUEST_WISHLIST_KEY = 'giftfinder_guest_wishlist';

const WishlistContext = createContext<WishlistContextType | undefined>(undefined);

export const WishlistProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated } = useAuth();
  const [wishlistItems, setWishlistItems] = useState<WishlistItemDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);

  // Load from backend if logged in, or from localStorage if guest
  const loadWishlist = useCallback(async () => {
    setIsLoading(true);
    try {
      if (isAuthenticated) {
        const items = await wishlistService.getWishlist();
        setWishlistItems(items || []);
      } else {
        const raw = localStorage.getItem(GUEST_WISHLIST_KEY);
        if (raw) {
          try {
            setWishlistItems(JSON.parse(raw));
          } catch {
            setWishlistItems([]);
          }
        } else {
          setWishlistItems([]);
        }
      }
    } catch (err) {
      console.warn('Failed to load wishlist:', err);
      // Fallback to local storage if API error
      const raw = localStorage.getItem(GUEST_WISHLIST_KEY);
      if (raw) {
        try {
          setWishlistItems(JSON.parse(raw));
        } catch {
          setWishlistItems([]);
        }
      } else {
        setWishlistItems([]);
      }
    } finally {
      setIsLoading(false);
    }
  }, [isAuthenticated]);

  useEffect(() => {
    loadWishlist();
  }, [loadWishlist]);

  const isWishlisted = useCallback(
    (productIdOrName: string) => {
      if (!productIdOrName) return false;
      return wishlistItems.some(
        (item) => item.productId === productIdOrName || item.productName === productIdOrName
      );
    },
    [wishlistItems]
  );

  const toggleWishlist = useCallback(
    async (product: ProductItem): Promise<boolean> => {
      const pId = product.id || product.name;
      const alreadyLiked = wishlistItems.some(
        (item) => item.productId === pId || item.productName === product.name
      );

      if (alreadyLiked) {
        // Remove
        await removeFromWishlist(pId);
        return false;
      } else {
        // Add
        const newItem: WishlistItemDto = {
          id: pId,
          productId: pId,
          productName: product.name,
          price: product.price,
          originalPrice: product.old,
          imageUrl: product.image,
          originalUrl: product.originalUrl,
          affiliateUrl: product.affiliateUrl,
          addedAt: new Date().toISOString(),
          isAvailable: true,
        };

        if (isAuthenticated && product.id && product.id.length > 10) {
          try {
            await wishlistService.addToWishlist(product.id);
          } catch (err) {
            console.warn('API addToWishlist error:', err);
          }
        }

        setWishlistItems((prev) => {
          const updated = [newItem, ...prev.filter((i) => i.productId !== pId && i.productName !== product.name)];
          localStorage.setItem(GUEST_WISHLIST_KEY, JSON.stringify(updated));
          return updated;
        });

        return true;
      }
    },
    [wishlistItems, isAuthenticated]
  );

  const removeFromWishlist = useCallback(
    async (productIdOrName: string) => {
      const target = wishlistItems.find(
        (item) => item.productId === productIdOrName || item.productName === productIdOrName
      );
      const targetId = target?.productId || productIdOrName;

      if (isAuthenticated && targetId && targetId.length > 10) {
        try {
          await wishlistService.removeFromWishlist(targetId);
        } catch (err) {
          console.warn('API removeFromWishlist error:', err);
        }
      }

      setWishlistItems((prev) => {
        const updated = prev.filter(
          (item) => item.productId !== productIdOrName && item.productName !== productIdOrName
        );
        localStorage.setItem(GUEST_WISHLIST_KEY, JSON.stringify(updated));
        return updated;
      });
    },
    [wishlistItems, isAuthenticated]
  );

  const value = useMemo(
    () => ({
      wishlistItems,
      wishlistCount: wishlistItems.length,
      isLoading,
      isWishlisted,
      toggleWishlist,
      removeFromWishlist,
      refreshWishlist: loadWishlist,
    }),
    [wishlistItems, isLoading, isWishlisted, toggleWishlist, removeFromWishlist, loadWishlist]
  );

  return <WishlistContext.Provider value={value}>{children}</WishlistContext.Provider>;
};

export const useWishlist = (): WishlistContextType => {
  const context = useContext(WishlistContext);
  if (!context) {
    throw new Error('useWishlist must be used within a WishlistProvider');
  }
  return context;
};

