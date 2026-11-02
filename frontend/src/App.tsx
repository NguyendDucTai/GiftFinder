import { useEffect, useState, useRef, useMemo, type ReactNode } from "react";
import { productService, type ProductDto } from "./features/products";
import { useAuth } from "./features/auth";
import { useWishlist } from "./features/wishlist";
import { useCart } from "./features/cart";
import { recommendationService } from "./features/recommendations";
import { catalogService } from "./features/catalog";
import { adminService } from "./features/admin/services/adminService";
import { MerchantPortal } from "./features/merchants/components/MerchantPortal";
import heroLuxuryPodium from "./assets/hero-luxury-podium.png";
import heroTechPodium from "./assets/hero-tech-podium.png";
import heroWellnessPodium from "./assets/hero-wellness-podium.png";
import heroPink from "./assets/hero-pink.png";
import heroLuxury from "./assets/hero-luxury.png";
import heroTea from "./assets/hero-tea.png";

export interface ProductItem {
  id?: string;
  name: string;
  price: number;
  old: number;

  rating: string;
  sold: string;
  image: string;
  platform: "SHOPEE" | "TIKTOK" | "DIRECT" | string;
  discount: string;
  originalUrl?: string;
  affiliateUrl?: string;
  tags?: string[];
}

/**
 * Tự động nâng cấp URL ảnh lên chuẩn HD sắc nét:
 * - TikTok Shop: Nâng thumbnail 260x260 lên 800x800 WebP Full HD
 * - Shopee: Loại bỏ đuôi thumbnail _tn để lấy ảnh gốc sắc nét 1000px
 * - Lazada: Nâng chất lượng từ 720q80 lên 1200x1200q90
 */
export function getHighResImageUrl(url?: string): string {
  if (!url) return images.candle;
  let cleanUrl = url.trim();

  // 1. TikTok Shop (ibyteimg.com / byteimg.com): Chuyển thumbnail 260:260 thành 800:800 HD
  if (cleanUrl.includes("ibyteimg.com") || cleanUrl.includes("byteimg.com")) {
    cleanUrl = cleanUrl
      .replace(/resize-webp:\d+:\d+/gi, "resize-webp:800:800")
      .replace(/resize-jpeg:\d+:\d+/gi, "resize-jpeg:800:800")
      .replace(/resize:\d+:\d+/gi, "resize:800:800");
  }

  // 2. Shopee: Loại bỏ hậu tố _tn để lấy ảnh gốc 1000px
  if (cleanUrl.includes("susercontent.com")) {
    cleanUrl = cleanUrl
      .replace(/_tn(\.jpg|\.png|\.webp)?$/i, "$1")
      .replace(/_tn$/i, "");
  }

  // 3. Lazada: Nâng cấp chất lượng ảnh lên 1200x1200
  if (cleanUrl.includes("lazcdn.com")) {
    cleanUrl = cleanUrl.replace(/_720x720q\d+/gi, "_1200x1200q90");
  }

  return cleanUrl;
}

function mapDtoToProductItem(dto: ProductDto): ProductItem {
  const origPrice =
    dto.originalPrice > dto.price
      ? dto.originalPrice
      : Math.round(dto.price * 1.25);
  const discountPercent = Math.max(
    5,
    Math.round((1 - dto.price / origPrice) * 100),
  );

  let platform = "SHOPEE";
  const src = (dto.source || "").toLowerCase();
  if (src.includes("tiktok")) {
    platform = "TIKTOK";
  } else if (src.includes("direct")) {
    platform = "DIRECT";
  } else if (src.includes("shopee")) {
    platform = "SHOPEE";
  }

  const reviewsCount = dto.totalReviews || 0;
  const soldStr =
    reviewsCount > 1000
      ? `${(reviewsCount / 1000).toFixed(1)}k`
      : reviewsCount > 0
        ? `${reviewsCount}`
        : "1.2k";

  return {
    id: dto.id,
    name: dto.name,
    price: dto.price,
    old: origPrice,
    rating: dto.rating > 0 ? dto.rating.toFixed(1) : "4.9",
    sold: soldStr,
    image: getHighResImageUrl(dto.imageUrl),
    platform: platform,
    discount: `-${discountPercent}%`,
    originalUrl: dto.originalUrl,
    affiliateUrl: dto.affiliateUrl,
    tags: (dto.tags || []).map((t: any) =>
      typeof t === "string" ? t : t.name,
    ),
  };
}

type Page =
  | "home"
  | "catalog"
  | "product"
  | "wishlist"
  | "merchant"
  | "auth"
  | "cart"
  | "checkout"
  | "success"
  | "admin";
type IconName =
  | "search"
  | "sparkle"
  | "heart"
  | "cart"
  | "user"
  | "chevron"
  | "chevron-down"
  | "arrow"
  | "arrow-left"
  | "arrow-right"
  | "bell"
  | "check"
  | "trash"
  | "minus"
  | "plus"
  | "menu"
  | "close"
  | "play"
  | "shield"
  | "logout"
  | "clock"
  | "eye"
  | "eye-off"
  | "external"
  | "gift"
  | "store"
  | "tag"
  | "trend"
  | "users"
  | "refresh"
  | "maximize"
  | "minimize";

const iconPaths: Record<IconName, ReactNode> = {
  search: (
    <>
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-4-4" />
    </>
  ),
  sparkle: (
    <>
      <path d="m12 2 1.5 5.3L18 10l-4.5 2.7L12 18l-1.5-5.3L6 10l4.5-2.7L12 2Z" />
      <path d="m19 16 .6 2 1.9 1-1.9 1-.6 2-.6-2-1.9-1 1.9-1 .6-2Z" />
    </>
  ),
  heart: (
    <path d="M20.8 4.7a5.4 5.4 0 0 0-7.7 0L12 5.8l-1.1-1.1a5.4 5.4 0 0 0-7.7 7.7L12 21l8.8-8.6a5.4 5.4 0 0 0 0-7.7Z" />
  ),
  cart: (
    <>
      <path d="M3 3h2l2.4 11h10.8l2-8H6" />
      <circle cx="9" cy="20" r="1" />
      <circle cx="17" cy="20" r="1" />
    </>
  ),
  user: (
    <>
      <circle cx="12" cy="8" r="4" />
      <path d="M4 21a8 8 0 0 1 16 0" />
    </>
  ),
  chevron: <path d="m9 18 6-6-6-6" />,
  arrow: (
    <>
      <path d="M5 12h14" />
      <path d="m14 7 5 5-5 5" />
    </>
  ),
  bell: (
    <>
      <path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" />
      <path d="M10 21h4" />
    </>
  ),
  check: <path d="m5 12 4 4L19 6" />,
  trash: (
    <>
      <path d="M4 7h16M9 7V4h6v3M7 7l1 14h8l1-14" />
    </>
  ),
  minus: <path d="M5 12h14" />,
  plus: <path d="M12 5v14M5 12h14" />,
  menu: <path d="M4 7h16M4 12h16M4 17h16" />,
  close: <path d="m6 6 12 12M18 6 6 18" />,
  play: <path d="m9 7 8 5-8 5V7Z" />,
  shield: (
    <>
      <path d="M12 22s8-3 8-10V5l-8-3-8 3v7c0 7 8 10 8 10Z" />
      <path d="m9 12 2 2 4-4" />
    </>
  ),
  logout: (
    <>
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
      <polyline points="16 17 21 12 16 7" />
      <line x1="21" y1="12" x2="9" y2="12" />
    </>
  ),
  "arrow-left": (
    <>
      <path d="m15 18-6-6 6-6" />
      <path d="M9 12h12" />
    </>
  ),
  "arrow-right": (
    <>
      <path d="M5 12h14" />
      <path d="m13 6 6 6-6 6" />
    </>
  ),
  "chevron-down": <path d="m6 9 6 6 6-6" />,
  clock: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </>
  ),
  eye: (
    <>
      <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6S2 12 2 12" />
      <circle cx="12" cy="12" r="2.5" />
    </>
  ),
  "eye-off": (
    <>
      <path d="m3 3 18 18" />
      <path d="M10.6 10.7a2 2 0 0 0 2.7 2.7" />
      <path d="M9.9 5.2A10 10 0 0 1 12 5c6.5 0 10 7 10 7a18 18 0 0 1-2.1 3.1M6.6 6.6C3.7 8.4 2 12 2 12s3.5 7 10 7a9.7 9.7 0 0 0 3.4-.6" />
    </>
  ),
  external: (
    <>
      <path d="M14 4h6v6" />
      <path d="m20 4-9 9" />
      <path d="M18 13v6a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h6" />
    </>
  ),
  gift: (
    <>
      <rect x="3" y="8" width="18" height="13" rx="2" />
      <path d="M12 8v13M3 12h18" />
      <path d="M12 8H7.5A2.5 2.5 0 1 1 10 5.5C10 7 12 8 12 8Zm0 0h4.5A2.5 2.5 0 1 0 14 5.5C14 7 12 8 12 8Z" />
    </>
  ),
  store: (
    <>
      <path d="M3 9h18l-2-5H5L3 9Z" />
      <path d="M5 9v11h14V9M9 20v-6h6v6" />
    </>
  ),
  tag: (
    <>
      <path d="M20 13 13 20 4 11V4h7l9 9Z" />
      <circle cx="8.5" cy="8.5" r="1" />
    </>
  ),
  trend: (
    <>
      <path d="m3 17 6-6 4 4 8-9" />
      <path d="M15 6h6v6" />
    </>
  ),
  users: (
    <>
      <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M22 21v-2a4 4 0 0 0-3-3.9M16 3.1a4 4 0 0 1 0 7.8" />
    </>
  ),
  refresh: (
    <>
      <path d="M20 7h-6V1" />
      <path d="M20 7a9 9 0 1 0 1 7" />
    </>
  ),
  maximize: (
    <>
      <path d="M15 3h6v6" />
      <path d="m21 3-7 7" />
      <path d="M9 21H3v-6" />
      <path d="m3 21 7-7" />
    </>
  ),
  minimize: (
    <>
      <path d="M4 14h6v6" />
      <path d="m10 14-7 7" />
      <path d="M20 10h-6V4" />
      <path d="m14 10 7-7" />
    </>
  ),
};

function Icon({ name, size = 19 }: { name: IconName; size?: number }) {
  return (
    <svg
      aria-hidden="true"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      {iconPaths[name]}
    </svg>
  );
}

const images = {
  hero: heroLuxuryPodium,
  heroLuxuryPodium,
  heroTechPodium,
  heroWellnessPodium,
  heroPink,
  heroLuxury,
  heroTea,
  giftbox: heroLuxuryPodium,
  rose: heroPink,
  candle: heroWellnessPodium,
  tray: "https://images.unsplash.com/photo-1780664561038-99d5f98d0935?auto=format&fit=crop&w=800&q=86",
  blue: "https://images.unsplash.com/photo-1700307638934-2319c356ba46?auto=format&fit=crop&w=800&q=86",
  macaron:
    "https://images.unsplash.com/photo-1518314144686-36a36d62a13c?auto=format&fit=crop&w=800&q=86",
  flower:
    "https://images.unsplash.com/photo-1589810852005-2aa57f9de547?auto=format&fit=crop&w=800&q=86",
  review1:
    "https://images.unsplash.com/photo-1758521540068-13a29ba07f78?auto=format&fit=crop&w=800&q=86",
  review2:
    "https://images.unsplash.com/photo-1758525223826-e010b024a7bf?auto=format&fit=crop&w=800&q=86",
};

const defaultProducts: ProductItem[] = [
  {
    name: "Set quà Lavender & Peony cao cấp",
    price: 320000,
    old: 400000,
    rating: "4.9",
    sold: "1.8k",
    image: images.candle,
    platform: "SHOPEE",
    discount: "-20%",
  },
  {
    name: "Giftbox Rose Gold dành cho nàng",
    price: 459000,
    old: 590000,
    rating: "4.8",
    sold: "980",
    image: images.rose,
    platform: "DIRECT",
    discount: "-22%",
  },
  {
    name: "Nến thơm gốm thủ công Mộc Miên",
    price: 249000,
    old: 319000,
    rating: "4.9",
    sold: "2.1k",
    image: images.tray,
    platform: "TIKTOK",
    discount: "-21%",
  },
  {
    name: "Hộp hoa hồng xanh Eternal Love",
    price: 689000,
    old: 850000,
    rating: "4.7",
    sold: "640",
    image: images.blue,
    platform: "SHOPEE",
    discount: "-19%",
  },
  {
    name: "Macaron & Flowers Celebration Box",
    price: 385000,
    old: 460000,
    rating: "4.8",
    sold: "1.2k",
    image: images.macaron,
    platform: "DIRECT",
    discount: "-16%",
  },
  {
    name: "Blooming Day – Set hoa tươi tinh tế",
    price: 529000,
    old: 650000,
    rating: "4.9",
    sold: "870",
    image: images.flower,
    platform: "TIKTOK",
    discount: "-18%",
  },
  {
    name: "Nến thơm Botanical Weekend",
    price: 289000,
    old: 355000,
    rating: "4.8",
    sold: "720",
    image: images.candle,
    platform: "SHOPEE",
    discount: "-18%",
  },
  {
    name: "Hộp quà Love Notes phiên bản giới hạn",
    price: 749000,
    old: 920000,
    rating: "5.0",
    sold: "430",
    image: images.rose,
    platform: "DIRECT",
    discount: "-19%",
  },
];

const money = (value: number) => `${value.toLocaleString("vi-VN")}đ`;

function Logo({ onClick }: { onClick?: () => void }) {
  return (
    <button className="brand" onClick={onClick}>
      <span>🎁</span>
      <b>GiftFinder</b>
    </button>
  );
}

function Button({
  children,
  variant = "primary",
  icon,
  className = "",
  onClick,
  type = "button",
  disabled = false,
  style,
}: {
  children: ReactNode;
  variant?:
    | "primary"
    | "secondary"
    | "rose"
    | "dark"
    | "danger"
    | "success"
    | "ghost"
    | "indigo";
  icon?: IconName;
  className?: string;
  onClick?: () => void;
  type?: "button" | "submit" | "reset";
  disabled?: boolean;
  style?: React.CSSProperties;
}) {
  const variantClass =
    variant === "rose" ? "button-primary" : `button-${variant}`;
  return (
    <button
      type={type}
      className={`button ${variantClass} ${className}`}
      onClick={onClick}
      disabled={disabled}
      style={style}
    >
      {icon && <Icon name={icon} />}
      {children}
    </button>
  );
}

function Badge({
  children,
  tone = "neutral",
  className = "",
}: {
  children: ReactNode;
  tone?: string;
  className?: string;
}) {
  const toneClass = tone.startsWith("badge-") ? tone : `badge-${tone}`;
  return (
    <span className={`badge ${toneClass} ${className}`.trim()}>{children}</span>
  );
}

function Header({
  go,
  currentPage,
  onSelectCategory,
}: {
  go: (page: Page) => void;
  currentPage?: Page;
  cartCount?: number;
  onSelectCategory?: (
    title: string,
    slug?: string,
    parentCategory?: string,
    keyword?: string,
    minPrice?: number,
    maxPrice?: number,
  ) => void;
}) {
  const { user, isAuthenticated, isAdmin, isMerchant, logout } = useAuth();
  const { wishlistCount } = useWishlist();
  const { cartCount } = useCart();
  const [menu, setMenu] = useState(false);
  const [navDropdown, setNavDropdown] = useState<
    "occasion" | "recipient" | "category" | null
  >(null);
  const [userDropdown, setUserDropdown] = useState(false);
  const [headerSearch, setHeaderSearch] = useState("");
  const navRef = useRef<HTMLDivElement>(null);
  const openTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const closeTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleMouseEnter = (type: "occasion" | "recipient" | "category") => {
    // Xoá ngay bộ hẹn giờ đóng nếu đang có
    if (closeTimeoutRef.current) {
      clearTimeout(closeTimeoutRef.current);
      closeTimeoutRef.current = null;
    }

    // Nếu đã có 1 menu đang mở, chuyển tab ngay lập tức (0ms delay) để lướt mượt mà giữa các mục
    if (navDropdown && navDropdown !== type) {
      if (openTimeoutRef.current) {
        clearTimeout(openTimeoutRef.current);
        openTimeoutRef.current = null;
      }
      setNavDropdown(type);
      return;
    }

    if (navDropdown === type) return;

    // Độ trễ ý định (Intent delay 90ms) khi mới rê chuột vào từ ngoài, tránh tình trạng quét chuột ngang qua bị bung menu đột ngột
    if (openTimeoutRef.current) {
      clearTimeout(openTimeoutRef.current);
    }
    openTimeoutRef.current = setTimeout(() => {
      setNavDropdown(type);
      openTimeoutRef.current = null;
    }, 90);
  };

  const handleMouseLeave = () => {
    // Huỷ mở nếu chuột rời đi trước khi hết thời gian chờ ý định
    if (openTimeoutRef.current) {
      clearTimeout(openTimeoutRef.current);
      openTimeoutRef.current = null;
    }

    if (closeTimeoutRef.current) {
      clearTimeout(closeTimeoutRef.current);
    }
    // Rời chuột là đóng gần như tức thì (60ms đệm siêu ngắn chống rung chuột)
    closeTimeoutRef.current = setTimeout(() => {
      setNavDropdown(null);
      closeTimeoutRef.current = null;
    }, 60);
  };

  useEffect(() => {
    const handleOutsideClick = (e: MouseEvent) => {
      if (navRef.current && !navRef.current.contains(e.target as Node)) {
        setNavDropdown(null);
      }
    };
    if (navDropdown) {
      document.addEventListener("mousedown", handleOutsideClick);
    }
    return () => {
      document.removeEventListener("mousedown", handleOutsideClick);
      if (openTimeoutRef.current) clearTimeout(openTimeoutRef.current);
      if (closeTimeoutRef.current) clearTimeout(closeTimeoutRef.current);
    };
  }, [navDropdown]);
  return (
    <>
      <header className="site-header">
        <div className="nav-main">
          <button
            className="mobile-menu"
            onClick={() => setMenu(!menu)}
            aria-label="Mở menu"
          >
            <Icon name={menu ? "close" : "menu"} />
          </button>
          <Logo onClick={() => go("home")} />
          <label className="global-search">
            <Icon name="sparkle" />
            <input
              placeholder="Tìm quà tặng: tên sản phẩm, người nhận, dịp lễ..."
              value={headerSearch}
              onChange={(e) => setHeaderSearch(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  const q = headerSearch.trim();
                  if (onSelectCategory) {
                    onSelectCategory(
                      q
                        ? `Kết quả tìm kiếm: "${q}"`
                        : "Tất cả quà tặng tuyển chọn",
                      "tat-ca",
                      "Tìm kiếm quà tặng",
                      q,
                    );
                  } else {
                    go("catalog");
                  }
                }
              }}
            />
            <button
              onClick={() => {
                const q = headerSearch.trim();
                if (onSelectCategory) {
                  onSelectCategory(
                    q
                      ? `Kết quả tìm kiếm: "${q}"`
                      : "Tất cả quà tặng tuyển chọn",
                    "tat-ca",
                    "Tìm kiếm quà tặng",
                    q,
                  );
                } else {
                  go("catalog");
                }
              }}
            >
              <Icon name="search" />
            </button>
          </label>
          {isAdmin ? (
            <button
              className="merchant-link"
              onClick={() => go("admin")}
              style={{
                background: "#111827",
                color: "#fff",
                border: "1px solid #111827",
                fontWeight: 600,
              }}
            >
              🛡️ Quản trị Admin
            </button>
          ) : isMerchant ? (
            <button
              className="merchant-link"
              onClick={() => go("merchant")}
              style={{
                background: "#4f46e5",
                color: "#fff",
                border: "1px solid #4f46e5",
                fontWeight: 600,
              }}
            >
              🏪 Kênh Người Bán
            </button>
          ) : (
            <button className="merchant-link" onClick={() => go("merchant")}>
              Bán hàng cùng GiftFinder
            </button>
          )}
          <button className="nav-icon" onClick={() => go("wishlist")}>
            <Icon name="heart" />
            {wishlistCount > 0 && <i>{wishlistCount}</i>}
            <small>Yêu thích</small>
          </button>
          <button className="nav-icon" onClick={() => go("cart")}>
            <Icon name="cart" />
            {cartCount > 0 && <i>{cartCount}</i>}
            <small>Giỏ hàng</small>
          </button>
          {!isAuthenticated ? (
            <Button variant="secondary" icon="user" onClick={() => go("auth")}>
              Đăng nhập
            </Button>
          ) : (
            <div style={{ position: "relative" }}>
              <button
                className="button secondary"
                onClick={() => setUserDropdown(!userDropdown)}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "7px",
                  padding: "0 12px",
                }}
              >
                <div
                  style={{
                    width: "24px",
                    height: "24px",
                    borderRadius: "50%",
                    background: isAdmin
                      ? "#111827"
                      : isMerchant
                        ? "#4f46e5"
                        : "#e11d48",
                    color: "#fff",
                    display: "grid",
                    placeItems: "center",
                    fontSize: "11px",
                    fontWeight: 800,
                  }}
                >
                  {user?.fullName?.charAt(0) || "U"}
                </div>
                <span
                  style={{
                    maxWidth: "90px",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                  }}
                >
                  {user?.fullName}
                </span>
                <Badge tone={isAdmin ? "dark" : isMerchant ? "indigo" : "rose"}>
                  {isAdmin ? "Admin" : isMerchant ? "Shop" : "Member"}
                </Badge>
              </button>
              {userDropdown && (
                <div
                  style={{
                    position: "absolute",
                    top: "calc(100% + 8px)",
                    right: 0,
                    background: "#fff",
                    borderRadius: "14px",
                    boxShadow: "0 10px 30px rgba(15,23,42,0.15)",
                    border: "1px solid var(--line)",
                    width: "230px",
                    padding: "8px",
                    zIndex: 100,
                  }}
                >
                  <div
                    style={{
                      padding: "8px 10px",
                      borderBottom: "1px solid #f1f5f9",
                    }}
                  >
                    <div
                      style={{
                        fontWeight: 700,
                        fontSize: "13px",
                        color: "var(--ink)",
                      }}
                    >
                      {user?.fullName}
                    </div>
                    <div
                      style={{
                        fontSize: "11px",
                        color: "var(--muted)",
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                      }}
                    >
                      {user?.email}
                    </div>
                  </div>
                  <div style={{ padding: "4px 0" }}>
                    {isAdmin && (
                      <button
                        onClick={() => {
                          go("admin");
                          setUserDropdown(false);
                        }}
                        style={{
                          width: "100%",
                          textAlign: "left",
                          padding: "8px 10px",
                          border: 0,
                          background: "none",
                          fontSize: "12px",
                          fontWeight: 600,
                          color: "var(--ink)",
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                          borderRadius: "8px",
                          cursor: "pointer",
                        }}
                        onMouseEnter={(e) =>
                          (e.currentTarget.style.background = "#f8fafc")
                        }
                        onMouseLeave={(e) =>
                          (e.currentTarget.style.background = "none")
                        }
                      >
                        🛡️ Quản trị hệ thống (Admin)
                      </button>
                    )}
                    {isMerchant && (
                      <button
                        onClick={() => {
                          go("merchant");
                          setUserDropdown(false);
                        }}
                        style={{
                          width: "100%",
                          textAlign: "left",
                          padding: "8px 10px",
                          border: 0,
                          background: "none",
                          fontSize: "12px",
                          fontWeight: 600,
                          color: "var(--ink)",
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                          borderRadius: "8px",
                          cursor: "pointer",
                        }}
                        onMouseEnter={(e) =>
                          (e.currentTarget.style.background = "#f8fafc")
                        }
                        onMouseLeave={(e) =>
                          (e.currentTarget.style.background = "none")
                        }
                      >
                        🏪 Kênh Người Bán (Shop)
                      </button>
                    )}
                    {!isAdmin && !isMerchant && (
                      <button
                        onClick={() => {
                          go("merchant");
                          setUserDropdown(false);
                        }}
                        style={{
                          width: "100%",
                          textAlign: "left",
                          padding: "8px 10px",
                          border: 0,
                          background: "none",
                          fontSize: "12px",
                          fontWeight: 600,
                          color: "var(--ink)",
                          display: "flex",
                          alignItems: "center",
                          gap: "8px",
                          borderRadius: "8px",
                          cursor: "pointer",
                        }}
                        onMouseEnter={(e) =>
                          (e.currentTarget.style.background = "#f8fafc")
                        }
                        onMouseLeave={(e) =>
                          (e.currentTarget.style.background = "none")
                        }
                      >
                        🏪 Mở gian hàng bán quà
                      </button>
                    )}
                    <button
                      onClick={() => {
                        go("wishlist");
                        setUserDropdown(false);
                      }}
                      style={{
                        width: "100%",
                        textAlign: "left",
                        padding: "8px 10px",
                        border: 0,
                        background: "none",
                        fontSize: "12px",
                        fontWeight: 600,
                        color: "var(--ink)",
                        display: "flex",
                        alignItems: "center",
                        gap: "8px",
                        borderRadius: "8px",
                        cursor: "pointer",
                      }}
                      onMouseEnter={(e) =>
                        (e.currentTarget.style.background = "#f8fafc")
                      }
                      onMouseLeave={(e) =>
                        (e.currentTarget.style.background = "none")
                      }
                    >
                      ♡ Quà đã yêu thích
                    </button>
                    <button
                      onClick={() => {
                        logout();
                        setUserDropdown(false);
                      }}
                      style={{
                        width: "100%",
                        textAlign: "left",
                        padding: "8px 10px",
                        border: 0,
                        background: "none",
                        fontSize: "12px",
                        fontWeight: 600,
                        color: "#e11d48",
                        display: "flex",
                        alignItems: "center",
                        gap: "8px",
                        borderRadius: "8px",
                        cursor: "pointer",
                        borderTop: "1px solid #f1f5f9",
                        marginTop: "4px",
                      }}
                      onMouseEnter={(e) =>
                        (e.currentTarget.style.background = "#fff1f2")
                      }
                      onMouseLeave={(e) =>
                        (e.currentTarget.style.background = "none")
                      }
                    >
                      <Icon name="logout" size={14} /> Đăng xuất
                    </button>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
        {currentPage !== "merchant" && (
          <div className={`category-nav ${menu ? "open" : ""}`} ref={navRef}>
          {/* Dropdown 1: Quà Theo Dịp */}
          <div
            className={`nav-dropdown-wrapper ${navDropdown === "occasion" ? "active" : ""}`}
            onMouseEnter={() => handleMouseEnter("occasion")}
            onMouseLeave={handleMouseLeave}
          >
            <button
              className={`nav-pill-btn ${navDropdown === "occasion" ? "active" : ""}`}
              onClick={() =>
                setNavDropdown(navDropdown === "occasion" ? null : "occasion")
              }
              onMouseEnter={() => handleMouseEnter("occasion")}
            >
              <span className="btn-icon">🎁</span>
              <span>Quà Theo Dịp</span>
              <span
                className={`btn-arrow ${navDropdown === "occasion" ? "open" : ""}`}
              >
                <Icon name="chevron-down" size={12} />
              </span>
            </button>

            {navDropdown === "occasion" && (
              <div
                className="category-dropdown-card"
                onMouseEnter={() => handleMouseEnter("occasion")}
              >
                <div className="category-dropdown-grid">
                  <div className="category-dropdown-column">
                    <div className="category-dropdown-heading">
                      SỰ KIỆN & KỶ NIỆM
                    </div>
                    <div className="category-dropdown-list">
                      {[
                        {
                          emoji: "🎁",
                          label: "Quà Sinh Nhật",
                          slug: "qua-sinh-nhat",
                        },
                        {
                          emoji: "💖",
                          label: "Quà Kỷ Niệm",
                          slug: "qua-ky-niem",
                        },
                        {
                          emoji: "🎓",
                          label: "Quà Tốt Nghiệp",
                          slug: "qua-tot-nghiep",
                        },
                        {
                          emoji: "💄",
                          label: "Quà Valentine",
                          slug: "qua-valentine",
                        },
                        {
                          emoji: "🌸",
                          label: "Quà Ngày Của Mẹ",
                          slug: "qua-ngay-cua-me",
                        },
                        {
                          emoji: "🎉",
                          label: "Quà Ngày Lễ",
                          slug: "qua-ngay-le",
                        },
                      ].map((item) => (
                        <button
                          key={item.label}
                          className="category-dropdown-item"
                          onClick={() => {
                            setNavDropdown(null);
                            if (onSelectCategory) {
                              onSelectCategory(
                                item.label,
                                item.slug,
                                "Quà Theo Dịp",
                              );
                            } else {
                              go("catalog");
                            }
                          }}
                        >
                          <div className="item-left">
                            <span className="item-emoji">{item.emoji}</span>
                            <span className="item-title">{item.label}</span>
                          </div>
                          <span className="item-arrow">
                            <Icon name="chevron" size={12} />
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>

                  <div className="category-dropdown-column">
                    <div className="category-dropdown-heading">
                      DỊP LỄ & ĐẶC BIỆT
                    </div>
                    <div className="category-dropdown-list">
                      {[
                        {
                          emoji: "🥮",
                          label: "Quà Trung Thu",
                          slug: "qua-trung-thu",
                        },
                        {
                          emoji: "🎄",
                          label: "Quà Giáng Sinh",
                          slug: "qua-giang-sinh",
                        },
                        { emoji: "🧧", label: "Quà Tết", slug: "qua-tet" },
                        {
                          emoji: "💐",
                          label: "Quà 20/11 & Thầy Cô",
                          slug: "qua-20-11-thay-co",
                        },
                        {
                          emoji: "✈️",
                          label: "Quà Chia Tay / Đi Xa",
                          slug: "qua-chia-tay-di-xa",
                        },
                        {
                          emoji: "🎈",
                          label: "Quà Tặng Không Cần Dịp",
                          slug: "qua-tang-khong-can-dip",
                        },
                      ].map((item) => (
                        <button
                          key={item.label}
                          className="category-dropdown-item"
                          onClick={() => {
                            setNavDropdown(null);
                            if (onSelectCategory) {
                              onSelectCategory(
                                item.label,
                                item.slug,
                                "Quà Theo Dịp",
                              );
                            } else {
                              go("catalog");
                            }
                          }}
                        >
                          <div className="item-left">
                            <span className="item-emoji">{item.emoji}</span>
                            <span className="item-title">{item.label}</span>
                          </div>
                          <span className="item-arrow">
                            <Icon name="chevron" size={12} />
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Dropdown 2: Quà Theo Người Nhận */}
          <div
            className={`nav-dropdown-wrapper ${navDropdown === "recipient" ? "active" : ""}`}
            onMouseEnter={() => handleMouseEnter("recipient")}
            onMouseLeave={handleMouseLeave}
          >
            <button
              className={`nav-pill-btn ${navDropdown === "recipient" ? "active" : ""}`}
              onClick={() =>
                setNavDropdown(navDropdown === "recipient" ? null : "recipient")
              }
              onMouseEnter={() => handleMouseEnter("recipient")}
            >
              <span className="btn-icon">💖</span>
              <span>Quà Theo Người Nhận</span>
              <span
                className={`btn-arrow ${navDropdown === "recipient" ? "open" : ""}`}
              >
                <Icon name="chevron-down" size={12} />
              </span>
            </button>

            {navDropdown === "recipient" && (
              <div
                className="category-dropdown-card"
                onMouseEnter={() => handleMouseEnter("recipient")}
              >
                <div className="category-dropdown-grid">
                  <div className="category-dropdown-column">
                    <div className="category-dropdown-heading">
                      GIA ĐÌNH & NGƯỜI THÂN
                    </div>
                    <div className="category-dropdown-list">
                      {[
                        {
                          emoji: "🌸",
                          label: "Quà Cho Mẹ",
                          slug: "qua-cho-me",
                        },
                        {
                          emoji: "👔",
                          label: "Quà Cho Bố",
                          slug: "qua-cho-bo",
                        },
                        {
                          emoji: "👵",
                          label: "Quà Cho Ông Bà",
                          slug: "qua-cho-ong-ba",
                        },
                        {
                          emoji: "🏡",
                          label: "Quà Tân Gia & Gia Đình",
                          slug: "qua-tan-gia-gia-dinh",
                        },
                        {
                          emoji: "👫",
                          label: "Quà Cho Vợ / Chồng",
                          slug: "qua-cho-vo-chong",
                        },
                      ].map((item) => (
                        <button
                          key={item.label}
                          className="category-dropdown-item"
                          onClick={() => {
                            setNavDropdown(null);
                            if (onSelectCategory) {
                              onSelectCategory(
                                item.label,
                                item.slug,
                                "Quà Theo Người Nhận",
                              );
                            } else {
                              go("catalog");
                            }
                          }}
                        >
                          <div className="item-left">
                            <span className="item-emoji">{item.emoji}</span>
                            <span className="item-title">{item.label}</span>
                          </div>
                          <span className="item-arrow">
                            <Icon name="chevron" size={12} />
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>

                  <div className="category-dropdown-column">
                    <div className="category-dropdown-heading">
                      TÌNH YÊU & CÔNG SỞ
                    </div>
                    <div className="category-dropdown-list">
                      {[
                        {
                          emoji: "💄",
                          label: "Quà Cho Nàng (Bạn Gái)",
                          slug: "qua-cho-nang",
                        },
                        {
                          emoji: "⌚",
                          label: "Quà Cho Chàng (Bạn Trai)",
                          slug: "qua-cho-chang",
                        },
                        {
                          emoji: "💼",
                          label: "Quà Cho Đồng Nghiệp",
                          slug: "qua-cho-dong-nghiep",
                        },
                        {
                          emoji: "👑",
                          label: "Quà Cho Sếp",
                          slug: "qua-cho-sep",
                        },
                        {
                          emoji: "🎓",
                          label: "Quà Tri Ân Thầy Cô",
                          slug: "qua-tri-an-thay-co",
                        },
                        {
                          emoji: "🤝",
                          label: "Quà Cho Bạn Thân",
                          slug: "qua-cho-ban-than",
                        },
                      ].map((item) => (
                        <button
                          key={item.label}
                          className="category-dropdown-item"
                          onClick={() => {
                            setNavDropdown(null);
                            if (onSelectCategory) {
                              onSelectCategory(
                                item.label,
                                item.slug,
                                "Quà Theo Người Nhận",
                              );
                            } else {
                              go("catalog");
                            }
                          }}
                        >
                          <div className="item-left">
                            <span className="item-emoji">{item.emoji}</span>
                            <span className="item-title">{item.label}</span>
                          </div>
                          <span className="item-arrow">
                            <Icon name="chevron" size={12} />
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Dropdown 3: Dòng Sản Phẩm */}
          <div
            className={`nav-dropdown-wrapper ${navDropdown === "category" ? "active" : ""}`}
            onMouseEnter={() => handleMouseEnter("category")}
            onMouseLeave={handleMouseLeave}
          >
            <button
              className={`nav-pill-btn ${navDropdown === "category" ? "active" : ""}`}
              onClick={() =>
                setNavDropdown(navDropdown === "category" ? null : "category")
              }
              onMouseEnter={() => handleMouseEnter("category")}
            >
              <span className="btn-icon">📦</span>
              <span>Dòng Sản Phẩm</span>
              <span
                className={`btn-arrow ${navDropdown === "category" ? "open" : ""}`}
              >
                <Icon name="chevron-down" size={12} />
              </span>
            </button>

            {navDropdown === "category" && (
              <div
                className="category-dropdown-card"
                onMouseEnter={() => handleMouseEnter("category")}
              >
                <div className="category-dropdown-grid">
                  <div className="category-dropdown-column">
                    <div className="category-dropdown-heading">
                      LÀM ĐẸP & PHONG CÁCH
                    </div>
                    <div className="category-dropdown-list">
                      {[
                        {
                          emoji: "💄",
                          label: "Mỹ Phẩm & Chăm Sóc Da",
                          slug: "my-pham-cham-soc-da",
                        },
                        {
                          emoji: "🕯️",
                          label: "Nến Thơm & Tinh Dầu",
                          slug: "nen-thom-tinh-dau",
                        },
                        {
                          emoji: "⌚",
                          label: "Đồng Hồ & Phụ Kiện Nam",
                          slug: "dong-ho-phu-kien-nam",
                        },
                        {
                          emoji: "💍",
                          label: "Trang Sức Nữ Cao Cấp",
                          slug: "trang-suc-nu-cao-cap",
                        },
                        {
                          emoji: "👜",
                          label: "Túi Xách & Ví Da",
                          slug: "tui-xach-vi-da",
                        },
                        {
                          emoji: "🧣",
                          label: "Khăn Choàng & Phụ Kiện",
                          slug: "khan-choang-phu-kien",
                        },
                      ].map((item) => (
                        <button
                          key={item.label}
                          className="category-dropdown-item"
                          onClick={() => {
                            setNavDropdown(null);
                            if (onSelectCategory) {
                              onSelectCategory(
                                item.label,
                                item.slug,
                                "Dòng Sản Phẩm",
                              );
                            } else {
                              go("catalog");
                            }
                          }}
                        >
                          <div className="item-left">
                            <span className="item-emoji">{item.emoji}</span>
                            <span className="item-title">{item.label}</span>
                          </div>
                          <span className="item-arrow">
                            <Icon name="chevron" size={12} />
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>

                  <div className="category-dropdown-column">
                    <div className="category-dropdown-heading">
                      CÔNG NGHỆ & ĐỜI SỐNG
                    </div>
                    <div className="category-dropdown-list">
                      {[
                        {
                          emoji: "🎧",
                          label: "Đồ Công Nghệ & Gaming",
                          slug: "do-cong-nghe-gaming",
                        },
                        {
                          emoji: "🎁",
                          label: "Set Quà Thủ Công Giftbox",
                          slug: "set-qua-thu-cong-giftbox",
                        },
                        {
                          emoji: "🍵",
                          label: "Bộ Ấm Trà & Ly Giữ Nhiệt",
                          slug: "bo-am-tra-ly-giu-nhiet",
                        },
                        {
                          emoji: "🌿",
                          label: "Sức Khỏe & Thảo Mộc",
                          slug: "suc-khoe-thao-moc",
                        },
                        {
                          emoji: "🪴",
                          label: "Đồ Decor Bàn Làm Việc",
                          slug: "do-decor-ban-lam-viec",
                        },
                        {
                          emoji: "📚",
                          label: "Sách & Sổ Tay Quà Tặng",
                          slug: "sach-so-tay-qua-tang",
                        },
                      ].map((item) => (
                        <button
                          key={item.label}
                          className="category-dropdown-item"
                          onClick={() => {
                            setNavDropdown(null);
                            if (onSelectCategory) {
                              onSelectCategory(
                                item.label,
                                item.slug,
                                "Dòng Sản Phẩm",
                              );
                            } else {
                              go("catalog");
                            }
                          }}
                        >
                          <div className="item-left">
                            <span className="item-emoji">{item.emoji}</span>
                            <span className="item-title">{item.label}</span>
                          </div>
                          <span className="item-arrow">
                            <Icon name="chevron" size={12} />
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>

          <button
            className="nav-text-btn"
            onClick={() => {
              setNavDropdown(null);
              if (onSelectCategory) {
                onSelectCategory(
                  "Quà Doanh Nghiệp & Đối Tác",
                  "qua-doanh-nghiep",
                  "Quà Doanh Nghiệp",
                );
              } else {
                go("catalog");
              }
            }}
          >
            Quà Doanh Nghiệp
          </button>

          <button
            className="nav-text-btn"
            onClick={() => {
              setNavDropdown(null);
              if (onSelectCategory) {
                onSelectCategory(
                  "Quà Theo Cung Hoàng Đạo",
                  "qua-cung-hoang-dao",
                  "Cung Hoàng Đạo",
                );
              } else {
                go("catalog");
              }
            }}
          >
            Quà theo cung hoàng đạo
          </button>

          <span />

          <button
            className="hot-nav-btn"
            onClick={() => {
              setNavDropdown(null);
              if (onSelectCategory) {
                onSelectCategory(
                  "⚡ Flash Sale — Deal Quà Hot Hôm Nay",
                  "flash-sale",
                  "Flash Sale",
                );
              } else {
                go("catalog");
              }
            }}
          >
            Flash Sale <span className="hot-nav-badge">HOT</span>
          </button>

          <div className="mobile-nav-links">
            <button onClick={() => go("wishlist")}>
              ♡ Quà đã yêu thích {wishlistCount > 0 ? `(${wishlistCount})` : ""}
            </button>
            {isAdmin ? (
              <button onClick={() => go("admin")}>🛡️ Quản trị Admin</button>
            ) : isMerchant ? (
              <button onClick={() => go("merchant")}>🏪 Kênh Người Bán</button>
            ) : (
              <button onClick={() => go("merchant")}>
                Bán hàng cùng GiftFinder
              </button>
            )}
            {!isAuthenticated ? (
              <button onClick={() => go("auth")}>Đăng nhập / Đăng ký</button>
            ) : (
              <button onClick={() => logout()} style={{ color: "var(--rose)" }}>
                Đăng xuất ({user?.fullName})
              </button>
            )}
          </div>
        </div>
        )}
      </header>
    </>
  );
}

function SectionTitle({
  eyebrow,
  title,
  action,
}: {
  eyebrow?: string;
  title: string;
  action?: ReactNode;
}) {
  return (
    <div className="section-title">
      <div>
        {eyebrow && <span>{eyebrow}</span>}
        <h2>{title}</h2>
      </div>
      {action}
    </div>
  );
}

function ProductCard({
  product,
  onOpen,
  onAffiliate,
  deal = false,
  sponsored = false,
}: {
  product: ProductItem;
  onOpen: (p?: ProductItem) => void;
  onAffiliate: () => void;
  deal?: boolean;
  sponsored?: boolean;
}) {
  const { isWishlisted, toggleWishlist } = useWishlist();
  const liked = product.id
    ? isWishlisted(product.id)
    : isWishlisted(product.name);
  const [tracked, setTracked] = useState(false);

  return (
    <article className="product-card">
      <div
        className="product-photo"
        onClick={() => onOpen(product)}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            onOpen(product);
          }
        }}
        style={{ cursor: "pointer" }}
      >
        <img
          src={getHighResImageUrl(product.image)}
          alt={product.name}
          loading="lazy"
          decoding="async"
        />
        <div className="product-badges">
          <Badge
            tone={
              product.platform === "TIKTOK"
                ? "dark"
                : product.platform === "DIRECT"
                  ? "indigo"
                  : "rose"
            }
          >
            {product.platform === "DIRECT"
              ? "GiftFinder Direct"
              : product.platform}
          </Badge>
          {sponsored && <Badge>Được tài trợ</Badge>}
        </div>
        <span className="discount">{product.discount}</span>
        <button
          className={`wish ${liked ? "liked" : ""}`}
          aria-label="Yêu thích"
          onClick={(e) => {
            e.stopPropagation();
            toggleWishlist(product);
          }}
        >
          <Icon name="heart" size={17} />
        </button>
      </div>
      <div className="product-info">
        <button className="product-name" onClick={() => onOpen(product)}>
          {product.name}
        </button>
        <div className="rating">
          ★★★★★{" "}
          <span>
            {product.rating} · Đã bán {product.sold}
          </span>
        </div>
        <div className="price">
          <b>{money(product.price)}</b>
          <del>{money(product.old)}</del>
        </div>
        {deal && (
          <>
            <div className="sold-bar">
              <i />
            </div>
            <small className="sold-label">Đã bán 85%</small>
          </>
        )}
        <div className="card-actions">
          <button
            aria-label="Theo dõi giá"
            style={
              tracked
                ? {
                    borderColor: "#e11d48",
                    color: "#e11d48",
                    background: "#fff1f2",
                  }
                : {}
            }
            onClick={() => setTracked(!tracked)}
            title={tracked ? "Đang theo dõi giảm giá" : "Bật theo dõi giá"}
          >
            <Icon name="bell" size={17} />
          </button>
          <Button
            variant="primary"
            onClick={() => {
              if (product.platform === "DIRECT") {
                onOpen(product);
              } else {
                onAffiliate();
              }
            }}
          >
            Mua ngay <Icon name="arrow" size={15} />
          </Button>
        </div>
      </div>
    </article>
  );
}

const addons = [
  { name: "Túi quà ruy băng ép nhũ", price: 15000, image: images.rose },
  { name: "Thiệp hoa khô viết tay", price: 20000, image: images.flower },
  { name: "Hộp nhạc gỗ quay tay", price: 85000, image: images.tray },
  { name: "Đèn Led đom đóm lung linh", price: 12000, image: images.candle },
  { name: "Bó hoa baby mini vĩnh cửu", price: 25000, image: images.blue },
];

const combos = [
  {
    name: "Combo Lời Yêu 20/10",
    occasion: "20/10 & 8/3",
    price: 489000,
    old: 590000,
    image: images.rose,
    items: "Hoa hồng sáp vĩnh cửu + Nước hoa bỏ túi + Thiệp viết tay",
    rating: "5.0",
    sold: "860",
  },
  {
    name: "Combo Sinh Nhật Ngọt Ngào",
    occasion: "Sinh nhật",
    price: 395000,
    old: 480000,
    image: images.macaron,
    items: "Bánh Macaron Pháp + Nến thơm + Trà hoa cúc hoàng gia",
    rating: "4.9",
    sold: "1.2k",
  },
  {
    name: "Combo Tân Gia Như Ý",
    occasion: "Tân gia",
    price: 520000,
    old: 650000,
    image: images.tray,
    items: "Khay gốm thủ công + Tinh dầu quế đại ngàn + Đèn trang trí",
    rating: "4.8",
    sold: "530",
  },
  {
    name: "Combo Ấm Áp Tình Thân",
    occasion: "Quà bố mẹ",
    price: 360000,
    old: 450000,
    image: images.candle,
    items: "Trà thảo mộc an giấc + Mật ong hoa rừng + Ly gốm mộc",
    rating: "4.9",
    sold: "940",
  },
];

interface ChatMessage {
  sender: "bot" | "user";
  text: string;
  summary?: string;
  advice?: string;
  directAnswer?: string;
  intentType?: string;
  product?: ProductItem;
  suggestedQuestions?: string[];
  effectiveQuery?: string;
}

const INITIAL_CHAT_MESSAGES: ChatMessage[] = [
  {
    sender: "bot",
    text: "👋 Chào bạn! Mình là AI Gift Concierge. Hôm nay bạn muốn tìm quà cho ai (ví dụ: bạn gái, bạn trai, bố mẹ, đồng nghiệp...), vào dịp gì và ngân sách khoảng bao nhiêu để mình gợi ý nhé! 🎁✨",
    suggestedQuestions: [
      "Quà sinh nhật cho bạn gái tầm 500k",
      "Quà tặng mẹ dịp 20/10 tinh tế, ý nghĩa",
      "Quà cho đồng nghiệp nam ngân sách 300k",
    ],
  },
];

function AiChatbotWidget({
  go: _go,
  products: _products = defaultProducts,
  openAffiliate,
  onSelectProduct,
}: {
  go: (p: Page) => void;
  products?: ProductItem[];
  openAffiliate?: (p?: ProductItem) => void;
  onSelectProduct?: (p: ProductItem) => void;
}) {
  const { user } = useAuth();
  const [open, setOpen] = useState(false);
  const [isExpanded, setIsExpanded] = useState(false);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const chatEndRef = useRef<HTMLDivElement>(null);

  // Cơ chế lưu trữ:
  // - Nếu ĐÃ ĐĂNG NHẬP: Dùng localStorage để giữ lại sau khi tắt trình duyệt (theo User ID)
  // - Nếu LÀ KHÁCH (chưa đăng nhập): Dùng sessionStorage để chỉ giữ trong phiên duyệt web; khi TẮT TAB/TRÌNH DUYỆT LÀ TỰ XÓA SẠCH!
  const isAuth = Boolean(user?.id);
  const storageKey = isAuth
    ? `giftfinder_ai_chat_${user!.id}`
    : "giftfinder_ai_chat_guest";
  const getStorage = () => (isAuth ? localStorage : sessionStorage);

  // Khôi phục tin nhắn từ Storage tương ứng
  const [messages, setMessages] = useState<ChatMessage[]>(() => {
    try {
      // Dọn sạch tàn dư cũ của guest trong localStorage (nếu có từ phiên trước)
      if (!isAuth && typeof window !== "undefined") {
        localStorage.removeItem("giftfinder_ai_chat_guest");
      }
      const storage = isAuth ? localStorage : sessionStorage;
      const saved = storage.getItem(storageKey);
      if (saved) {
        const parsed = JSON.parse(saved);
        if (Array.isArray(parsed) && parsed.length > 0) return parsed;
      }
    } catch (e) {
      console.warn("Failed to read chat history:", e);
    }
    return INITIAL_CHAT_MESSAGES;
  });

  // Khi trạng thái đăng nhập thay đổi (User ID thay đổi), tải đúng phiên chat của tài khoản đó
  useEffect(() => {
    try {
      const storage = getStorage();
      const saved = storage.getItem(storageKey);
      if (saved) {
        const parsed = JSON.parse(saved);
        if (Array.isArray(parsed) && parsed.length > 0) {
          setMessages(parsed);
          return;
        }
      }
      setMessages(INITIAL_CHAT_MESSAGES);
    } catch {
      setMessages(INITIAL_CHAT_MESSAGES);
    }
  }, [storageKey, isAuth]);

  // Tự động lưu lịch sử chat vào Storage tương ứng mỗi khi có tin nhắn mới
  useEffect(() => {
    try {
      const storage = getStorage();
      storage.setItem(storageKey, JSON.stringify(messages));
    } catch (e) {
      console.warn("Failed to persist chat history:", e);
    }
  }, [messages, storageKey, isAuth]);

  // Tự động cuộn xuống tin nhắn mới nhất
  useEffect(() => {
    if (open) {
      chatEndRef.current?.scrollIntoView({ behavior: "smooth" });
    }
  }, [messages, loading, open]);

  // Làm mới cuộc trò chuyện (Bắt đầu lượt tìm kiếm mới)
  const handleResetChat = () => {
    setMessages(INITIAL_CHAT_MESSAGES);
    try {
      localStorage.removeItem("giftfinder_ai_chat_guest");
      sessionStorage.removeItem("giftfinder_ai_chat_guest");
      if (user?.id) {
        localStorage.removeItem(`giftfinder_ai_chat_${user.id}`);
      }
    } catch {}
  };

  const quickPrompts = [
    "🎁 Quà 20/10 cho bạn gái tầm 500k",
    "🎂 Sinh nhật đồng nghiệp nam 300k",
    "🏡 Quà tân gia nhà mới sang trọng",
  ];

  const handleSend = async (textToSend?: string) => {
    const q = (textToSend || input).trim();
    if (!q || loading) return;

    const newMsgs: ChatMessage[] = [...messages, { sender: "user", text: q }];
    setMessages(newMsgs);
    if (!textToSend) setInput("");

    const lowerQ = q.toLowerCase();

    // 1. Nhận diện lời CẢM ƠN / KHEN NGỢI / HÀI LÒNG (Xử lý ngay tại Frontend, tốn 0 token! Không đính kèm sản phẩm thừa)
    const isThanks =
      /(cảm ơn|cam on|thank|thanks|tks|cmon|rất tốt|tuyệt vời|quá đẹp|quá ưng|ưng ý|ưng rồi|chuẩn rồi|tốt lắm|hay quá|ok rồi|được rồi)/i.test(
        lowerQ,
      );
    if (isThanks) {
      setTimeout(() => {
        const thankMessages = [
          "❤️ Rất vui vì đã giúp bạn tìm được món quà ưng ý! Chúc bạn và người nhận có một dịp kỷ niệm thật ấm áp, ý nghĩa và ngập tràn niềm vui nhé! 🎉 Nếu cần tư vấn thêm bất cứ món quà nào khác, mình luôn ở đây sẵn sàng hỗ trợ bạn!",
          "🥰 Dạ không có chi ạ! Chúc món quà này sẽ mang lại thật nhiều bất ngờ và nụ cười rạng rỡ cho người thương của bạn. Cần tìm thêm quà dịp nào bạn cứ nhắn mình nhé!",
          "✨ Cảm ơn bạn rất nhiều! Hy vọng món quà sẽ thay bạn gửi gắm trọn vẹn sự yêu thương và tinh tế. Bất cứ khi nào cần gợi ý quà mới, bạn cứ ghé GiftFinder nhé! 🎁",
        ];
        const randomMsg =
          thankMessages[Math.floor(Math.random() * thankMessages.length)];
        setMessages((prev) => [
          ...prev,
          {
            sender: "bot",
            text: randomMsg,
          },
        ]);
      }, 350);
      return;
    }

    // 2. Nhận diện lời CHÀO TẠM BIỆT (Xử lý ngay, 0 token)
    const isFarewell = /(tạm biệt|bye|goodbye|gặp lại sau|hẹn gặp lại)/i.test(
      lowerQ,
    );
    if (isFarewell) {
      setTimeout(() => {
        setMessages((prev) => [
          ...prev,
          {
            sender: "bot",
            text: "👋 Tạm biệt bạn nhé! Chúc bạn một ngày thật tuyệt vời và nhiều niềm vui! Hẹn gặp lại bạn bất cứ khi nào bạn cần tìm quà tặng nha! ✨",
          },
        ]);
      }, 300);
      return;
    }

    // 3. Nhận diện câu CHÀO HỎI ĐƠN THUẦN (Xử lý ngay, 0 token, ngắn gọn dưới 25 ký tự)
    const isPureGreeting =
      /^(hello|hêlo|helo|hi|chào|alo|hey|ê|ơi|bạn ơi|test|xin chào|good morning)(\s|[!.,?]|$)/i.test(
        q.trim(),
      ) && q.trim().length <= 25;
    if (isPureGreeting) {
      setTimeout(() => {
        setMessages((prev) => [
          ...prev,
          {
            sender: "bot",
            text: "👋 Chào bạn! Mình là AI Gift Concierge. Hôm nay bạn muốn tìm quà cho ai (bạn gái, bạn trai, bố mẹ, đồng nghiệp...), vào dịp nào và mức ngân sách khoảng bao nhiêu?",
            suggestedQuestions: [
              "Quà sinh nhật cho bạn gái tầm 500k",
              "Quà tặng mẹ dịp 20/10 tinh tế, ý nghĩa",
              "Quà cho đồng nghiệp nam ngân sách 300k",
            ],
          },
        ]);
      }, 300);
      return;
    }

    // 4. Nhận diện câu hỏi TOÁN HỌC / TÍNH TOÁN / CÔNG THỨC (Xử lý ngay tại Frontend, 0 token, không gọi API)
    // LƯU Ý: Tuyệt đối không chặn các ngày lễ dạng ngày/tháng như 20/10, 8/3, 20/11, 14/2, 1/6, 24/12 hoặc câu có ngữ cảnh quà tặng!
    const hasGiftContextInQuery =
      /(quà|tặng|mua|gợi\s*ý|sinh\s*nhật|valentine|giáng\s*sinh|noel|tết|kỷ\s*niệm|tân\s*gia|tốt\s*nghiệp|20\/10|8\/3|20\/11|14\/2|1\/6|24\/12|bạn\s*gái|bạn\s*trai|nam|nữ|nàng|chàng|bố|ba|mẹ|má|ông\s*bà|sếp|đồng\s*nghiệp|vợ|chồng|người\s*yêu|crush|em\s*bé|trẻ\s*em|thiếu\s*nhi|bé\s*trai|bé\s*gái|bé|con|cháu|thầy|cô|ngân\s*sách|giá|tiền|k\b|cành|lít|củ|triệu|đ\b)/i.test(
        lowerQ,
      );
    const isDateOnly =
      /^\s*(0?[1-9]|[12]\d|3[01])\s*\/\s*(0?[1-9]|1[0-2])(\s*\/\s*\d{2,4})?\s*$/i.test(
        q.trim(),
      );

    const isMathOrCalculation =
      !hasGiftContextInQuery &&
      !isDateOnly &&
      ((/^[\d\s+\-*/=().,^%?]+$/.test(q.trim()) &&
        /[\+\*]|\d+\s*[\-\/]\s*\d+/.test(q)) ||
        /(giải\s+toán|làm\s+toán|phép\s+tính|bài\s+toán)/i.test(lowerQ));
    if (isMathOrCalculation) {
      setTimeout(() => {
        setMessages((prev) => [
          ...prev,
          {
            sender: "bot",
            text: "Dạ GiftFinder là trợ lý AI chuyên gợi ý và tư vấn quà tặng thôi ạ! 🎁 Mình không hỗ trợ giải toán hay làm phép tính được. Bạn có thể cho mình biết người nhận, dịp tặng hoặc mức ngân sách để mình tìm giúp món quà ưng ý nhất nhé! ✨",
            suggestedQuestions: [
              "Quà sinh nhật cho bạn gái tầm 500k",
              "Quà tặng mẹ dịp 20/10 tinh tế, ý nghĩa",
              "Quà cho đồng nghiệp nam ngân sách 300k",
            ],
          },
        ]);
      }, 300);
      return;
    }

    // 5. Nhận diện câu hỏi NGOÀI LỀ / LẠC ĐỀ (Thời tiết, tin tức, code...) (0 token)
    const isOffTopic =
      /(thời tiết|dự báo thời tiết|mấy giờ|hôm nay ngày mấy|tin tức|viết code|lập trình|dịch tiếng anh|viết văn|làm thơ)/i.test(
        lowerQ,
      );
    if (isOffTopic) {
      setTimeout(() => {
        setMessages((prev) => [
          ...prev,
          {
            sender: "bot",
            text: "Dạ GiftFinder là trợ lý AI chuyên tư vấn và gợi ý quà tặng thôi ạ! 🎁 Mình chưa hỗ trợ các câu hỏi ngoài lề như thời tiết, tin tức hay viết code. Bạn hãy thử nhắn cho mình người nhận (ví dụ: bạn gái, bạn trai, bố mẹ...), dịp tặng hoặc mức ngân sách để mình chọn quà thật chuẩn nhé! ✨",
            suggestedQuestions: [
              "Quà sinh nhật cho bạn gái tầm 500k",
              "Quà tặng mẹ dịp 20/10 tinh tế, ý nghĩa",
              "Quà cho đồng nghiệp nam ngân sách 300k",
            ],
          },
        ]);
      }, 300);
      return;
    }

    // 6. Nhận diện câu hỏi QUÁ CHUNG CHUNG / CHỈ CÓ TỪ "QUÀ" HOẶC YÊU CẦU TƯ VẤN THIẾU MỌI THÔNG TIN CỤ THỂ (0 token, xử lý ngay)
    const isTooGenericGiftQuery =
      /^(tôi\s*muốn\s*|mình\s*muốn\s*|em\s*muốn\s*|cần\s*|hãy\s*|giúp\s*mình\s*|giúp\s*em\s*|cho\s*mình\s*|cho\s*em\s*)?(quà|quà\s*tặng|món\s*quà|tìm\s*quà|tìm\s*quà\s*tặng|tìm\s*món\s*quà|mua\s*quà|mua\s*quà\s*tặng|mua\s*món\s*quà|chọn\s*quà|chọn\s*quà\s*tặng|chọn\s*món\s*quà|tư\s*vấn|tư\s*vấn\s*quà|tư\s*vấn\s*quà\s*tặng|tư\s*vấn\s*chọn\s*quà|tư\s*vấn\s*giúp|gợi\s*ý|gợi\s*ý\s*quà|gợi\s*ý\s*quà\s*tặng|gợi\s*ý\s*giúp|cần\s*tìm\s*quà|cần\s*mua\s*quà|có\s*quà\s*gì|có\s*món\s*gì|mua\s*gì|tặng\s*gì|quà\s*gì|chọn\s*gì|mua\s*quà\s*gì|tặng\s*quà\s*gì)(\s*(ạ|nhé|nha|với|giúp|giúp\s*mình|giúp\s*em|giùm|đi|nào|với\s*ạ))*[.?!,\s]*$/i.test(
        q.trim(),
      );
    if (isTooGenericGiftQuery) {
      setTimeout(() => {
        setMessages((prev) => [
          ...prev,
          {
            sender: "bot",
            text: "Dạ bạn muốn tìm quà cho ai (ví dụ: bạn gái, bạn trai, bố mẹ, đồng nghiệp...), vào dịp gì (sinh nhật, kỷ niệm, 20/10...) và ngân sách khoảng bao nhiêu để mình gợi ý món quà chuẩn gu nhất nhé! 🎁✨",
            suggestedQuestions: [
              "Quà sinh nhật cho bạn gái tầm 500k",
              "Quà tặng mẹ dịp 20/10 tinh tế, ý nghĩa",
              "Quà cho đồng nghiệp nam ngân sách 300k",
            ],
          },
        ]);
      }, 300);
      return;
    }

    setLoading(true);

    // Lấy danh sách các câu hỏi trước đó của người dùng trong phiên chat
    const previousUserQueries = messages
      .filter((m) => m.sender === "user")
      .map((m) => m.text.trim());

    // 1. Nhận diện các câu chào hỏi, cảm thán vu vơ hoặc ngoài lề
    // 1. Nhận diện các câu chào hỏi, cảm thán vu vơ hoặc ngoài lề
    const isGreetingOrCasual =
      /^(hello|hêlo|helo|hi|chào|alo|hey|ê|ơi|bạn ơi|test|xin chào|good morning|cảm ơn|thanks|bye|tạm biệt)(\s|[!.,?]|$)/i.test(
        q.trim(),
      );

    // Các từ ngữ chỉ hành động đổi món / xem món khác / đề xuất sản phẩm khác
    const askAnotherPattern =
      /(món\s+khác|món\s+quà\s+khác|sản\s+phẩm\s+khác|sp\s+khác|quà\s+khác|mẫu\s+khác|set\s+khác|combo\s+khác|cái\s+khác|đổi\s+món|đổi\s+quà|đổi\s+sản\s+phẩm|đổi\s+sp|gợi\s+ý\s+khác|gợi\s+ý\s+món\s+khác|gợi\s+ý\s+sản\s+phẩm\s+khác|đề\s+xuất\s+khác|đề\s+xuất\s+món\s+khác|đề\s+xuất\s+món\s+quà\s+khác|đề\s+xuất\s+sản\s+phẩm\s+khác|đề\s+xuất\s+cái\s+khác|món\s+nào\s+khác|sản\s+phẩm\s+nào\s+khác|xem\s+thêm\s+món|xem\s+sản\s+phẩm\s+khác|thay\s+món|thay\s+sản\s+phẩm|có\s+món\s+nào\s+khác|có\s+sản\s+phẩm\s+nào\s+khác|món\s+khác\s+xem|sản\s+phẩm\s+khác\s+xem|cái\s+khác\s+xem|món\s+mới|sản\s+phẩm\s+mới|cho\s+món\s+khác|cho\s+sản\s+phẩm\s+khác|xem\s+món\s+khác|còn\s+món\s+nào|còn\s+sản\s+phẩm\s+nào|còn\s+gì\s+nữa|chưa\s+ưng|không\s+thích\s+món\s+này|không\s+hợp)/i;

    // 2. Nhận diện các câu yêu cầu đổi món
    // - isPureAskAnother: chỉ thuần túy yêu cầu đổi món (không kèm thêm ngân sách hay thuộc tính mới)
    const isPureAskAnother =
      /^(khác|đổi|đổi\s*đi|khác\s*nữa|khác\s*đi|thay\s*đổi|còn\s*gì\s*khác|còn\s*không|món\s*khác|món\s*quà\s*khác|sản\s*phẩm\s*khác|sp\s*khác|quà\s*khác|mẫu\s*khác|set\s*khác|combo\s*khác|cái\s*khác|đổi\s*món|đổi\s*quà|đổi\s*sản\s*phẩm|đổi\s*sp|gợi\s*ý\s*khác|gợi\s*ý\s*món\s*khác|gợi\s*ý\s*sản\s*phẩm\s*khác|đề\s*xuất\s*khác|đề\s*xuất\s*món\s*khác|đề\s*xuất\s*món\s*quà\s*khác|đề\s*xuất\s*sản\s*phẩm\s*khác|đề\s*xuất\s*cái\s*khác|món\s*nào\s*khác|sản\s*phẩm\s*nào\s*khác|xem\s*thêm\s*món|xem\s*sản\s*phẩm\s*khác|thay\s*món|thay\s*sản\s*phẩm|có\s*món\s*nào\s*khác|có\s*sản\s*phẩm\s*nào\s*khác|món\s*khác\s*xem|sản\s*phẩm\s*khác\s*xem|cái\s*khác\s*xem|món\s*mới|sản\s*phẩm\s*mới|cho\s*món\s*khác|cho\s*sản\s*phẩm\s*khác|xem\s*món\s*khác|còn\s*món\s*nào|còn\s*sản\s*phẩm\s*nào|còn\s*gì\s*nữa|chưa\s*ưng|chưa\s*thích|không\s*thích\s*món\s*này|không\s*hợp|món\s*khác\s*nữa|món\s*khác\s*đi|sản\s*phẩm\s*khác\s*đi)(\s*(ạ|nhé|nha|đi|với|nào|xem|nữa|hơn))*[.?!,\s]*$/i.test(
        q.trim(),
      );

    // - isAskAnother: câu có chứa ý định đổi món (kể cả đổi thuần túy hoặc đổi kèm tiêu chí mới)
    const isAskAnother = isPureAskAnother || askAnotherPattern.test(q);

    // 3. Nhận diện câu nhắc lại đối tượng cũ (Ví dụ: "cho bố cơ mà", "tặng bố mà", "của bố mà")
    const isReaffirmCurrent = /(cơ mà|mà|nhé|nè|nha|đã bảo)/i.test(q);

    // 4. Nhận diện đối tượng người nhận
    const recipientRegex =
      /(bạn gái|bạn trai|bạn thân là nam|bạn thân là nữ|bạn thân nam|bạn thân nữ|bạn thân|bạn bè|bạn nam|bạn nữ|đồng nghiệp|ông bà|ông|bà|bố|ba|cha|mẹ|má|sếp|thầy cô|thầy|cô giáo|chồng|vợ|anh trai|em trai|chị gái|em gái|cho nam|cho nữ|em bé|trẻ em|thiếu nhi|bé trai|bé gái|cho bé|tặng bé|bé|con|cháu|crush|phụ nữ|nam giới|người yêu|con trai|con gái|đàn ông|phái mạnh|phái đẹp)/i;
    const currentRecipientMatch = q.match(recipientRegex);
    const currentRecipient = currentRecipientMatch
      ? currentRecipientMatch[0].toLowerCase()
      : "";

    // Lấy ngữ cảnh truy vấn gần nhất (từ tin nhắn bot trước hoặc câu hỏi thực tế gần nhất)
    const lastBotWithQuery = [...messages]
      .reverse()
      .find((m) => m.sender === "bot" && m.effectiveQuery);

    // Tìm câu hỏi tìm kiếm quà có nội dung thực tế gần nhất
    let lastMeaningfulQuery = lastBotWithQuery?.effectiveQuery || "";
    if (!lastMeaningfulQuery) {
      for (let i = previousUserQueries.length - 1; i >= 0; i--) {
        const prevQ = previousUserQueries[i];
        const isPrevAsk = askAnotherPattern.test(prevQ);
        const isPrevGreet = /^(hello|hi|chào|test|alo|hey)/i.test(prevQ);
        if (!isPrevAsk && !isPrevGreet && prevQ.length > 3) {
          lastMeaningfulQuery = prevQ;
          break;
        }
      }
    }

    // Kiểm tra xem người dùng có đổi sang đối tượng người nhận mới không
    const prevRecipientMatch = lastMeaningfulQuery
      ? lastMeaningfulQuery.match(recipientRegex)
      : null;
    const prevRecipient = prevRecipientMatch
      ? prevRecipientMatch[0].toLowerCase()
      : "";
    const isFemale = (r: string) => /nữ|gái|mẹ|má|bà|chị|em gái|vợ|cô/i.test(r);
    const isMale = (r: string) =>
      /nam|trai|bố|ba|cha|ông|anh|em trai|chồng|chú/i.test(r);
    const isGenderSwitch = Boolean(
      (currentRecipient &&
        prevRecipient &&
        isFemale(currentRecipient) &&
        isMale(prevRecipient)) ||
      (currentRecipient &&
        prevRecipient &&
        isMale(currentRecipient) &&
        isFemale(prevRecipient)),
    );
    const hasNewTarget = Boolean(
      (currentRecipient &&
        prevRecipient &&
        currentRecipient !== prevRecipient &&
        !isReaffirmCurrent) ||
      isGenderSwitch,
    );

    // Hàm làm sạch ngữ cảnh: Gạt bỏ các cụm giá cũ và các cụm đổi món rác, tránh hiện tượng snowballing nối đuôi dài dặc
    const stripOldBudgetAndNoise = (text: string) => {
      return (
        text
          // Xóa các cụm giá cũ dạng "giá trên 500k", "dưới 500k", "tầm 300k", "khoảng 1 triệu", v.v.
          .replace(
            /(mức\s*giá|ngân\s*sách|giá)?\s*(trên|hơn|dưới|tối\s*đa|đổ\s*lại|tầm|khoảng|loanh\s*quanh|từ)?\s*\d+\s*(k|cành|lít|lốp|củ|triệu|tr|đ|vnđ)?(\s*(đến|-)\s*\d+\s*(k|cành|lít|lốp|củ|triệu|tr|đ|vnđ)?)?/gi,
            "",
          )
          .replace(
            /(ngân\s*sách|mức\s*giá|giá\s*cả|tầm|khoảng)\s*(trên|dưới|hơn)?/gi,
            "",
          )
          .replace(askAnotherPattern, "")
          .replace(/,\s*,+/g, ",")
          .replace(/^[\s,]+|[\s,]+$/g, "")
          .trim()
      );
    };

    // Hàm làm sạch cụm từ chỉ ngân sách (gạt bỏ các từ dẫn dắt như "Đổi sang mức giá")
    const cleanBudgetPhrase = (text: string) => {
      return text
        .replace(
          /^(đổi\s*sang|đổi\s*mức\s*giá|đổi\s*món|món\s*khác|xem\s*món|tìm\s*món|gợi\s*ý\s*món|chuyển\s*sang)\s*(mức\s*giá|giá)?\s*/i,
          "",
        )
        .replace(/^giá\s+/i, "")
        .trim();
    };

    // Hàm nhận diện tiêu chí phong cách / chủ đề / sở thích / tính chất quà tặng
    const isStyleOrThemeQuery = (text: string) => {
      return /(phong\s*cách|chủ\s*đề|sở\s*thích|gu\b|tinh\s*tế|sang\s*trọng|lịch\s*lãm|hiện\s*đại|thanh\s*lịch|quý\s*phái|dễ\s*thương|cute|đáng\s*yêu|xinh\s*xắn|tiện\s*dụng|thiết\s*thực|hữu\s*dụng|công\s*nghệ|thông\s*minh|tiện\s*ích|làm\s*đẹp|thời\s*trang|trang\s*sức|phụ\s*kiện|mỹ\s*phẩm|chăm\s*sóc\s*sức\s*khỏe|sức\s*khỏe|thư\s*giãn|massage|nến\s*thơm|decor|trang\s*trí|độc\s*lạ|vintage|hộp\s*quà|hộp\s*đẹp|kèm\s*thiệp|full\s*box|lãng\s*mạn)/i.test(
        text,
      );
    };

    // Hàm gạt bỏ các tiêu chí phong cách/chủ đề cũ để tránh tích tụ snowballing chuỗi dài dặc
    const stripOldStylesAndThemes = (text: string) => {
      const parts = text
        .split(/[,;\n]+/)
        .map((p) => p.trim())
        .filter(Boolean);
      if (parts.length <= 1) return text;

      const filtered = parts.filter((part, idx) => {
        if (idx === 0) return true; // Luôn giữ cụm gốc đầu tiên (chứa người nhận / dịp / ngân sách)
        if (
          recipientRegex.test(part) ||
          /(sinh\s*nhật|20\/10|8\/3|20\/11|14\/2|valentine|giáng\s*sinh|noel|tết|kỷ\s*niệm|tân\s*gia)/i.test(
            part,
          )
        ) {
          return true;
        }
        if (
          /(trên|hơn|dưới|tối\s*đa|đổ\s*lại|tầm|khoảng|loanh\s*quanh|từ)?\s*\d+\s*(k|cành|lít|lốp|củ|triệu|tr|đ|vnđ)/i.test(
            part,
          )
        ) {
          return true;
        }
        if (isStyleOrThemeQuery(part)) {
          return false;
        }
        return true;
      });

      return filtered.join(", ");
    };

    // Kiểm tra xem câu hiện tại người dùng nhập có chứa ngân sách mới hay không
    const hasNewBudgetInQuery =
      /(trên|hơn|dưới|tối\s*đa|đổ\s*lại|tầm|khoảng|loanh\s*quanh|từ|đến|\b\d+\s*(k|cành|lít|lốp|củ|triệu|tr|đ|vnđ)\b)/i.test(
        q,
      );

    // Bóc tách tiêu chí mới từ câu của người dùng (bỏ các cụm đổi món thừa)
    const newCriteriaOnly = q
      .replace(askAnotherPattern, "")
      .replace(/^[\s,]+|[\s,]+$/g, "")
      .trim();

    // Lấy ID sản phẩm của tin nhắn bot gần nhất (nếu có) để phục vụ hỏi đáp sản phẩm theo ngữ cảnh (Contextual Product Q&A)
    const lastBotMsgWithProduct = [...messages]
      .reverse()
      .find((m) => m.sender === "bot" && m.product?.id);
    const currentProductId = lastBotMsgWithProduct?.product?.id;
    const currentProductContext = lastBotMsgWithProduct?.product
      ? `${lastBotMsgWithProduct.product.name} (${lastBotMsgWithProduct.product.price}đ)`
      : undefined;

    // Nhận diện nếu người dùng đang hỏi đáp về sản phẩm hiện tại (Contextual Product Q&A)
    const isProductInquiry =
      /(món|sản\s*phẩm|cái|em|chiếc|bộ|set|đồng\s*hồ|vòng|dây\s*chuyền|gối)\s*này/i.test(
        q,
      ) ||
      /(nó có|có chống nước|chống nước không|chất liệu gì|bằng gì|bảo hành|dây da|dây kim loại|size|kích thước|mấy món|full box|như nào|thế nào|nói qua)/i.test(
        q,
      );

    // Nhận diện câu hỏi điều chỉnh ngân sách tương đối (thấp hơn / rẻ hơn / đắt hơn...)
    const isRelativeBudgetAdjustment =
      /(thấp\s*hơn|rẻ\s*hơn|mềm\s*hơn|bình\s*dân\s*hơn|tiết\s*kiệm\s*hơn|giá\s*cao|đắt\s*(quá|vậy|thế)|cao\s*vậy|chát\s*quá|bớt\s*tiền|đắt\s*hơn|cao\s*hơn|xịn\s*hơn)/i.test(
        q,
      );

    // Xây dựng câu truy vấn gửi lên Backend AI:
    let queryWithContext = q;

    if (isProductInquiry && currentProductId) {
      queryWithContext = q;
    } else if (isRelativeBudgetAdjustment) {
      const cleanBase =
        stripOldBudgetAndNoise(lastMeaningfulQuery) || lastMeaningfulQuery;
      queryWithContext = cleanBase ? `${cleanBase}, ${q}` : q;
    } else if (isPureAskAnother) {
      // Khi yêu cầu đổi món thuần túy ("món khác", "đề xuất sản phẩm khác"):
      // Giữ nguyên ngữ cảnh đang truy vấn, không nối thêm chữ rác, và để excludeIds xoay vòng sản phẩm
      queryWithContext = lastMeaningfulQuery ? lastMeaningfulQuery : q;
    } else if (isAskAnother) {
      // Khi yêu cầu đổi món nhưng CÓ KÈM TIÊU CHÍ MỚI (ví dụ: "Đề xuất sản phẩm khác trên 500k", "Món khác phong cách tinh tế")
      if (hasNewBudgetInQuery && lastMeaningfulQuery) {
        // Gạt bỏ mức giá cũ, chỉ giữ lại [Đối tượng + Dịp tặng] + [Mức giá mới]
        const cleanBase =
          stripOldBudgetAndNoise(lastMeaningfulQuery) || lastMeaningfulQuery;
        const bPhrase = cleanBudgetPhrase(newCriteriaOnly || q);
        queryWithContext = `${cleanBase}, ${bPhrase}`;
      } else if (isStyleOrThemeQuery(newCriteriaOnly || q)) {
        // Gạt bỏ phong cách cũ, thay thế bằng phong cách mới
        const cleanBase =
          stripOldStylesAndThemes(lastMeaningfulQuery) || lastMeaningfulQuery;
        queryWithContext = `${cleanBase}, ${newCriteriaOnly || q}`;
      } else {
        queryWithContext = lastMeaningfulQuery
          ? `${lastMeaningfulQuery}, ${newCriteriaOnly || q}`
          : q;
      }
    } else if (isReaffirmCurrent) {
      queryWithContext = lastMeaningfulQuery ? lastMeaningfulQuery : q;
    } else if (
      !hasNewTarget &&
      !isGreetingOrCasual &&
      !currentRecipient &&
      lastMeaningfulQuery
    ) {
      // Khi người dùng chỉ nhắn thêm tinh chỉnh ngắn (ví dụ: "giá trên 500k", "màu hồng", "phong cách tinh tế")
      if (hasNewBudgetInQuery) {
        // Gạt bỏ mức giá cũ để mốc giá mới ghi đè triệt để
        const cleanBase =
          stripOldBudgetAndNoise(lastMeaningfulQuery) || lastMeaningfulQuery;
        const bPhrase = cleanBudgetPhrase(q);
        queryWithContext = `${cleanBase}, ${bPhrase}`;
      } else if (isStyleOrThemeQuery(q)) {
        // Gạt bỏ phong cách cũ, thay thế bằng phong cách mới
        const cleanBase =
          stripOldStylesAndThemes(lastMeaningfulQuery) || lastMeaningfulQuery;
        queryWithContext = `${cleanBase}, ${q}`;
      } else {
        queryWithContext = `${lastMeaningfulQuery}, ${q}`;
      }
    } else {
      // Trường hợp tìm kiếm mới hoặc câu đầy đủ: GỬI NGUYÊN VĂN 100% CÂU CỦA NGƯỜI DÙNG!
      queryWithContext = q;
    }

    // Thu thập danh sách sản phẩm đã từng hiển thị trong phiên chat (tránh lặp đi lặp lại 1 món quà)
    const seenProductIds = messages
      .map((m) => m.product?.id)
      .filter((id): id is string => Boolean(id));

    // Nếu không phải chuyển sang người nhận mới hoàn toàn -> gửi danh sách sản phẩm đã xem để hệ thống tự xoay vòng món mới
    const excludeIds =
      !hasNewTarget && seenProductIds.length > 0 ? seenProductIds : undefined;

    try {
      const res = await recommendationService.getRecommendations(
        queryWithContext,
        6,
        excludeIds,
        currentProductId,
        currentProductContext,
      );

      // Nếu câu hỏi không liên quan đến tìm quà (chào hỏi, nói vu vơ, ngoài lề, hoặc quá chung chung), chỉ hiển thị lời đối đáp của AI, KHÔNG gắn sản phẩm
      if (res.isValidIntent === false) {
        setMessages((prev) => [
          ...prev,
          {
            sender: "bot",
            text:
              res.message ||
              "👋 Chào bạn! Mình là AI Gift Concierge. Hôm nay bạn muốn tìm quà cho ai, vào dịp nào và ngân sách khoảng bao nhiêu?",
            suggestedQuestions: res.interpretedIntent?.suggestedQuestions || [
              "Quà sinh nhật cho bạn gái tầm 500k",
              "Quà tặng mẹ dịp 20/10 tinh tế, ý nghĩa",
              "Quà cho đồng nghiệp nam ngân sách 300k",
            ],
          },
        ]);
        return;
      }

      let replyText =
        res.message ||
        "Dựa trên yêu cầu của bạn, AI đã phân tích và gợi ý món quà phù hợp nhất từ hệ thống:";
      let matchedProduct: ProductItem | undefined = undefined;
      let summaryText = res.interpretedIntent?.summary || undefined;
      let adviceText: string | undefined = undefined;
      let rawSuggestions = res.interpretedIntent?.suggestedQuestions || [];

      // Nếu câu đổi món khiến suggestedQuestions bị trống, kế thừa từ tin nhắn trước
      if (!rawSuggestions || rawSuggestions.length === 0) {
        const prevWithSuggestions = [...messages]
          .reverse()
          .find((m) => m.suggestedQuestions && m.suggestedQuestions.length > 0);
        if (prevWithSuggestions?.suggestedQuestions) {
          rawSuggestions = prevWithSuggestions.suggestedQuestions;
        }
      }

      // Lọc bỏ câu hỏi gợi ý trùng lặp với câu người dùng vừa chọn hoặc đã từng hỏi trong phiên chat
      const askedQueriesLower = [...previousUserQueries, q].map((s) =>
        s.toLowerCase().trim(),
      );
      let freshSuggestedQuestions = rawSuggestions.filter((sq) => {
        const sqLower = sq.toLowerCase().trim();
        if (
          askedQueriesLower.some(
            (asked) =>
              asked === sqLower ||
              asked.includes(sqLower) ||
              sqLower.includes(asked),
          )
        ) {
          return false;
        }
        // Kiểm tra trùng từ khóa phong cách giữa câu vừa hỏi và chip gợi ý
        const styleKeys = [
          "sức khỏe",
          "thư giãn",
          "tinh tế",
          "sang trọng",
          "lịch lãm",
          "thiết thực",
          "tiện dụng",
          "hộp quà",
          "kèm thiệp",
          "công nghệ",
          "thông minh",
          "tiện ích",
          "thời trang",
          "phụ kiện",
          "xinh xắn",
        ];
        for (const sk of styleKeys) {
          if (lowerQ.includes(sk) && sqLower.includes(sk)) return false;
        }
        return true;
      });

      // Đảm bảo luôn có đủ ít nhất 3 gợi ý tinh chỉnh đa dạng, không trùng lặp
      const fallbackSuggestions = [
        "Tìm quà mang phong cách tinh tế, sang trọng",
        "Quà có hộp đóng gói kèm thiệp chúc mừng",
        "Tìm quà thiết thực, tiện dụng hàng ngày",
        "Đổi sang mức giá trên 500k",
        "Quà thời trang, phụ kiện xinh xắn",
        "Quà công nghệ thông minh, tiện ích",
        "Quà chăm sóc sức khỏe, thư giãn",
      ];

      for (const fb of fallbackSuggestions) {
        if (freshSuggestedQuestions.length >= 3) break;
        const fbLower = fb.toLowerCase();
        if (
          !askedQueriesLower.some(
            (asked) =>
              asked === fbLower ||
              asked.includes(fbLower) ||
              fbLower.includes(asked),
          ) &&
          !freshSuggestedQuestions.some(
            (item) => item.toLowerCase() === fbLower,
          )
        ) {
          freshSuggestedQuestions.push(fb);
        }
      }

      if (res.items && res.items.length > 0) {
        const top = res.items[0];
        adviceText = top.aiAdvice || undefined;

        matchedProduct = {
          id: top.id,
          name: top.name,
          price: top.price,
          old: Math.round(top.price * 1.25),
          rating: String(top.rating > 0 ? top.rating : 4.9),
          sold: String(top.totalReviews > 0 ? top.totalReviews : 128),
          image: top.imageUrl || images.candle,
          platform: (top.originalUrl || "").toLowerCase().includes("tiktok")
            ? "TIKTOK"
            : (top.originalUrl || "").toLowerCase().includes("shopee")
              ? "SHOPEE"
              : "DIRECT",
          discount: "-20%",
          originalUrl: top.originalUrl,
          affiliateUrl: top.affiliateUrl,
        };
      } else {
        replyText =
          res.message ||
          "AI đã xem hết các món quà phù hợp trong tầm giá nới lỏng. Bạn hãy thử mở rộng ngân sách hoặc chọn một sở thích cụ thể khác nhé! ✨";
      }

      // Chỉ lưu effectiveQuery nếu đây là câu tìm quà thực sự (không phải hỏi thống kê kho hay chính sách dịch vụ)
      const isActualGiftQuery =
        res.intentType === "Recommendation" ||
        (res.items && res.items.length > 0);
      const nextEffectiveQuery = isActualGiftQuery
        ? queryWithContext
        : lastMeaningfulQuery;

      setMessages((prev) => [
        ...prev,
        {
          sender: "bot",
          text: replyText,
          summary: summaryText,
          advice: adviceText,
          directAnswer: res.directAnswer || undefined,
          intentType: res.intentType || undefined,
          product: matchedProduct,
          suggestedQuestions:
            freshSuggestedQuestions.length > 0
              ? freshSuggestedQuestions
              : undefined,
          effectiveQuery: nextEffectiveQuery,
        },
      ]);
    } catch (err) {
      console.warn("AI recommendation API error:", err);
      setMessages((prev) => [
        ...prev,
        {
          sender: "bot",
          text: "Hệ thống tư vấn quà tặng đang tạm thời bận hoặc gặp sự cố kết nối. Bạn vui lòng bấm gửi lại câu hỏi hoặc nhấn nút 'Làm mới' ở trên để thử lại nhé! ✨",
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <div className="chatbot-trigger">
        {!open && (
          <div className="chatbot-tooltip" onClick={() => setOpen(true)}>
            <Icon name="sparkle" size={14} />
            <span>Cần gợi ý quà? Nhắn AI ngay nhé!</span>
          </div>
        )}
        <button
          className="chatbot-btn"
          onClick={() => setOpen(!open)}
          aria-label="Trợ lý AI"
        >
          <Icon name={open ? "close" : "sparkle"} size={24} />
          {!open && <span className="chatbot-online-dot" />}
        </button>
      </div>

      {open && (
        <div className={`chatbot-window ${isExpanded ? "expanded" : ""}`}>
          <div className="chatbot-header">
            <div className="chatbot-header-left">
              <div className="chatbot-avatar">
                <Icon name="sparkle" size={19} />
              </div>
              <div>
                <b className="chatbot-header-title">GiftFinder AI Concierge</b>
                <div className="chatbot-header-status">
                  <i />
                  <span>Đang trực tuyến sẵn sàng</span>
                </div>
              </div>
            </div>
            <div className="chatbot-header-actions">
              <button
                type="button"
                className="chatbot-header-btn"
                onClick={handleResetChat}
                title="Làm mới cuộc trò chuyện (Bắt đầu tìm kiếm mới)"
              >
                <Icon name="refresh" size={12} />
                <span>Làm mới</span>
              </button>
              <button
                type="button"
                className="chatbot-icon-btn"
                onClick={() => setIsExpanded(!isExpanded)}
                title={
                  isExpanded ? "Thu nhỏ kích thước" : "Phóng to cửa sổ chat"
                }
              >
                <Icon name={isExpanded ? "minimize" : "maximize"} size={16} />
              </button>
              <button
                type="button"
                className="chatbot-icon-btn"
                onClick={() => setOpen(false)}
                title="Đóng trợ lý chat"
              >
                <Icon name="close" size={18} />
              </button>
            </div>
          </div>

          <div className="chatbot-body">
            {messages.map((m, idx) => (
              <div key={idx} className={`chat-msg ${m.sender}`}>
                {m.sender === "bot" && (
                  <div className="chat-bot-avatar">🎁</div>
                )}
                <div className="chat-msg-content">
                  {/* Nếu là bot có tóm tắt ý định: hiển thị huy hiệu tóm tắt tiêu chí */}
                  {m.summary && (
                    <div className="chat-intent-badge">
                      <span>🎯 {m.summary}</span>
                    </div>
                  )}

                  {/* Phản hồi trực tiếp từ AI Concierge (cho câu hỏi tư vấn / thắc mắc kho hàng / hỏi thông số) */}
                  {m.directAnswer && (
                    <div className="chat-direct-answer-card">
                      <div className="chat-advice-card-header">
                        <Icon name="sparkle" size={13} />
                        <span>
                          {m.intentType === "ProductDetailQnA"
                            ? "Giải đáp sản phẩm từ AI Concierge"
                            : m.intentType === "InventoryInquiry"
                              ? "Phản hồi kho quà từ AI Concierge"
                              : "Tư vấn từ AI Concierge"}
                        </span>
                      </div>
                      <p className="chat-advice-card-body">{m.directAnswer}</p>
                    </div>
                  )}

                  {/* Lời khuyên tư vấn của AI Concierge (chỉ hiển thị khi không có directAnswer để tránh trùng lặp) */}
                  {!m.directAnswer && m.advice ? (
                    <div className="chat-advice-card">
                      <div className="chat-advice-card-header">
                        <Icon name="sparkle" size={13} />
                        <span>Lời khuyên từ AI Concierge</span>
                      </div>
                      <p className="chat-advice-card-body">{m.advice}</p>
                    </div>
                  ) : (
                    !m.directAnswer &&
                    !m.advice && <div className="chat-bubble">{m.text}</div>
                  )}

                  {/* Thẻ sản phẩm quà tặng cao cấp */}
                  {m.product && (
                    <div className="chat-product-card">
                      <div className="chat-product-thumb-wrap">
                        <img
                          src={m.product.image}
                          alt={m.product.name}
                          className="chat-product-thumb"
                        />
                        <span
                          className={`chat-product-platform-pill ${
                            m.product.platform === "TIKTOK"
                              ? "tiktok"
                              : m.product.platform === "SHOPEE"
                                ? "shopee"
                                : "direct"
                          }`}
                        >
                          {m.product.platform === "TIKTOK"
                            ? "TikTok"
                            : m.product.platform === "SHOPEE"
                              ? "Shopee"
                              : "Chính hãng"}
                        </span>
                      </div>

                      <div className="chat-product-details">
                        <b
                          className="chat-product-title"
                          title={m.product.name}
                        >
                          {m.product.name}
                        </b>

                        <div className="chat-product-meta">
                          <b>⭐ {m.product.rating || "4.9"}</b>
                          <span>•</span>
                          <span>{m.product.sold || "128"} đã bán</span>
                        </div>

                        <div className="chat-product-price-row">
                          <span className="chat-product-price">
                            {money(m.product.price)}
                          </span>
                          {m.product.old && (
                            <span className="chat-product-old-price">
                              {money(m.product.old)}
                            </span>
                          )}
                          <span className="chat-product-discount">-20%</span>
                        </div>

                        <div className="chat-product-btn-group">
                          <button
                            type="button"
                            className="chat-product-cta"
                            onClick={() => {
                              if (
                                openAffiliate &&
                                m.product?.platform !== "DIRECT"
                              ) {
                                openAffiliate(m.product);
                              } else if (m.product) {
                                setOpen(false);
                                onSelectProduct?.(m.product);
                              }
                            }}
                          >
                            <span>
                              {m.product.platform === "DIRECT"
                                ? "Xem chi tiết quà"
                                : "Mở gian hàng mua ngay"}
                            </span>
                            <Icon name="arrow" size={13} />
                          </button>
                          {m.product.platform !== "DIRECT" && (
                            <button
                              type="button"
                              className="chat-product-sub-btn"
                              onClick={() => {
                                setOpen(false);
                                if (m.product) {
                                  onSelectProduct?.(m.product);
                                }
                              }}
                              title="Xem chi tiết trên GiftFinder"
                            >
                              Chi tiết
                            </button>
                          )}
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Câu hỏi gợi ý thêm từ AI (Tự động cập nhật theo ngữ cảnh) */}
                  {m.suggestedQuestions && m.suggestedQuestions.length > 0 && (
                    <div className="chat-dynamic-suggestions">
                      <div className="chat-suggestions-title">
                        <Icon name="sparkle" size={12} />
                        <span>Gợi ý câu hỏi tinh chỉnh tiếp theo</span>
                      </div>
                      <div className="chat-chips-list">
                        {m.suggestedQuestions.map((sq) => (
                          <button
                            key={sq}
                            type="button"
                            className="chat-chip-pill"
                            onClick={() => handleSend(sq)}
                          >
                            <span>👉</span>
                            <span style={{ flex: 1 }}>{sq}</span>
                          </button>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              </div>
            ))}

            {loading && (
              <div className="chat-msg bot">
                <div className="chat-bot-avatar">✨</div>
                <div className="chat-msg-content">
                  <div
                    className="chat-bubble chat-typing-bubble"
                    aria-label="AI đang phản hồi"
                  >
                    <span className="chat-dot" />
                    <span className="chat-dot" />
                    <span className="chat-dot" />
                  </div>
                </div>
              </div>
            )}

            {/* Chỉ hiển thị gợi ý khởi đầu khi mới mở chat hoặc chưa có nhiều tương tác */}
            {messages.length <= 1 && (
              <div className="chat-suggestions">
                <div className="chat-suggestions-title">
                  <Icon name="sparkle" size={12} />
                  <span>Hoặc chọn câu hỏi mẫu nhanh:</span>
                </div>
                {quickPrompts.map((p) => (
                  <button key={p} type="button" onClick={() => handleSend(p)}>
                    {p}
                  </button>
                ))}
              </div>
            )}

            <div ref={chatEndRef} />
          </div>

          <form
            className="chat-input-bar"
            onSubmit={(e) => {
              e.preventDefault();
              handleSend();
            }}
          >
            <input
              placeholder="Nhập yêu cầu: người nhận, dịp, giá, sở thích..."
              value={input}
              onChange={(e) => setInput(e.target.value)}
            />
            <button
              type="submit"
              className="chat-send-btn"
              disabled={loading || !input.trim()}
              title="Gửi câu hỏi"
            >
              <Icon name="arrow" size={16} />
            </button>
          </form>
        </div>
      )}
    </>
  );
}

interface TestimonialItem {
  id: string;
  avatarId: number;
  avatarBg: string;
  name: string;
  handle: string;
  quote: string;
}

function CustomerAvatar({ avatarId, bg }: { avatarId: number; bg: string }) {
  return (
    <div className="testimonial-avatar" style={{ backgroundColor: bg }}>
      <svg
        viewBox="0 0 36 36"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        className="testimonial-avatar-svg"
      >
        <circle
          cx="18"
          cy="15"
          r="7"
          fill={avatarId % 2 === 0 ? "#fcd34d" : "#fed7aa"}
        />
        {avatarId === 1 && (
          <path
            d="M11 15c0-4.5 3.1-8 7-8s7 3.5 7 8c0 1-.5 1.8-1 2.2C22.8 13.5 20.5 11 18 11s-4.8 2.5-6 6.2c-.5-.4-1-1.2-1-2.2z"
            fill="#78350f"
          />
        )}
        {avatarId === 2 && (
          <>
            <path
              d="M10 16c0-5 3.6-9 8-9s8 4 8 9c0 1-.8 1.5-1.5 2-1-4-3.5-6.5-6.5-6.5S12.5 14 11.5 18c-.7-.5-1.5-1-1.5-2z"
              fill="#1e293b"
            />
            <circle
              cx="15"
              cy="15"
              r="2.2"
              stroke="#475569"
              strokeWidth="0.8"
              fill="none"
            />
            <circle
              cx="21"
              cy="15"
              r="2.2"
              stroke="#475569"
              strokeWidth="0.8"
              fill="none"
            />
            <line
              x1="17.2"
              y1="15"
              x2="18.8"
              y2="15"
              stroke="#475569"
              strokeWidth="0.8"
            />
          </>
        )}
        {avatarId === 3 && (
          <path
            d="M11 14c.5-4.5 3.5-7.5 7-7.5s6.5 3 7 7.5c-1.5-2-4-3-7-3s-5.5 1-7 3z"
            fill="#334155"
          />
        )}
        {avatarId === 4 && (
          <>
            <path
              d="M11 16c0-4.5 3.1-8 7-8s7 3.5 7 8v4c-1-2-3-4-7-4s-6 2-7 4v-4z"
              fill="#9a3412"
            />
            <path
              d="M10 20c0 3 2 5 2 5s1-3 1-5"
              stroke="#9a3412"
              strokeWidth="1"
            />
            <path
              d="M26 20c0 3-2 5-2 5s-1-3-1-5"
              stroke="#9a3412"
              strokeWidth="1"
            />
          </>
        )}
        {avatarId === 5 && (
          <path
            d="M11 14c1-4.5 4-7.5 7-7.5 4 0 7 3.5 7 7.5-2.5-3-5.5-4-8-3-2 .8-4 1.8-6 3z"
            fill="#451a03"
          />
        )}
        {avatarId === 6 && (
          <path
            d="M11 15c0-4.5 3.1-8 7-8s7 3.5 7 8v2c-2-3-4.5-4.5-7-4.5s-5 1.5-7 4.5v-2z"
            fill="#172554"
          />
        )}
        {avatarId === 7 && (
          <path
            d="M11 15c0-4.5 3.1-8 7-8s7 3.5 7 8c0 1.5-.5 3-1 4.5-1-4-3.5-6-6-6s-5 2-6 6c-.5-1.5-1-3-1-4.5z"
            fill="#b45309"
          />
        )}
        {avatarId === 8 && (
          <>
            <circle cx="18" cy="7" r="3" fill="#1e1b4b" />
            <path
              d="M11 15c0-4 3.1-7.5 7-7.5s7 3.5 7 7.5c-2-2-4.5-3-7-3s-5 1-7 3z"
              fill="#1e1b4b"
            />
          </>
        )}
        <circle cx="15.5" cy="15" r="0.8" fill="#1e293b" />
        <circle cx="20.5" cy="15" r="0.8" fill="#1e293b" />
        <path
          d="M16 18.2c.6.8 1.4 1 2 1s1.4-.2 2-1"
          stroke="#e11d48"
          strokeWidth="0.9"
          strokeLinecap="round"
        />
        <path
          d="M8 34c0-5.5 4.5-10 10-10s10 4.5 10 10"
          fill={
            avatarId === 1
              ? "#10b981"
              : avatarId === 2
                ? "#f97316"
                : avatarId === 3
                  ? "#3b82f6"
                  : avatarId === 4
                    ? "#ec4899"
                    : avatarId === 5
                      ? "#6366f1"
                      : avatarId === 6
                        ? "#14b8a6"
                        : avatarId === 7
                          ? "#8b5cf6"
                          : "#f43f5e"
          }
        />
      </svg>
    </div>
  );
}

function TestimonialCard({ item }: { item: TestimonialItem }) {
  return (
    <article className="testimonial-card">
      <div className="testimonial-card-header">
        <CustomerAvatar avatarId={item.avatarId} bg={item.avatarBg} />
        <div className="testimonial-user-meta">
          <h4 className="testimonial-name">{item.name}</h4>
          <span className="testimonial-handle">{item.handle}</span>
        </div>
      </div>
      <div className="testimonial-stars" aria-label="5 sao">
        ★★★★★
      </div>
      <p className="testimonial-quote">“{item.quote}”</p>
    </article>
  );
}

const TESTIMONIALS_COL1: TestimonialItem[] = [
  {
    id: "t1",
    avatarId: 1,
    avatarBg: "#dcfce7",
    name: "Khách hàng 07",
    handle: "@khachhang07",
    quote:
      "Em thích chiếc ly tri thức của bên mình lắm ạ. Cho em đặt 20 cái để em tiếp tục đi quảng bá tri thức tuyệt quý này anh nhé. Biết ơn GiftFinder lắm lắm 😊",
  },
  {
    id: "t2",
    avatarId: 2,
    avatarBg: "#fee2e2",
    name: "Thuỳ Linh",
    handle: "@thuylinh_hn",
    quote:
      "Mình chỉ mô tả vài câu về tính cách bạn gái, AI gợi ý ngay set nến thơm cao cấp. Bạn ấy xúc động rơi nước mắt luôn. Hộp gói đẹp hơn ảnh rất nhiều!",
  },
  {
    id: "t3",
    avatarId: 3,
    avatarBg: "#e0e7ff",
    name: "Khách hàng 10",
    handle: "@khachhang10",
    quote:
      "Mình cũng vừa được nhận món quà siêu yêu, nhìn là thấy cả nhà Wit và nhà 253 đang rạng rỡ xinh tươi 💕 Từng chút đóng gói, in ấn đều rất tận tâm.",
  },
  {
    id: "t4",
    avatarId: 4,
    avatarBg: "#fef3c7",
    name: "Minh Hoàng",
    handle: "@hoang_gaming",
    quote:
      "Tặng bạn thân chuột gaming không dây FURYCUBE và lót chuột ROG. Nó khen rê chuột mượt đét, đèn LED RGB cực ngầu, 10/10 điểm uy tín!",
  },
];

const TESTIMONIALS_COL2: TestimonialItem[] = [
  {
    id: "t5",
    avatarId: 5,
    avatarBg: "#fed7aa",
    name: "Khách hàng 02",
    handle: "@khachhang02",
    quote:
      "Biết ơn GiftFinder đã gieo duyên bộ lịch Mã Niên Như Ý. Bộ lịch còn mang nhiều bài học nhân sinh đính kèm và có thêm phong bì lì xì rất tinh tế.",
  },
  {
    id: "t6",
    avatarId: 6,
    avatarBg: "#ccfbf1",
    name: "Khách hàng 05",
    handle: "@khachhang05",
    quote:
      "Em đã nhận được quà gửi tặng cô giáo 20/11 rồi ạ. Món quà siêu đẹp và ý nghĩa. Trên sản phẩm còn có thiệp viết tay nắn nót, người nhận xúc động lắm ạ!",
  },
  {
    id: "t7",
    avatarId: 7,
    avatarBg: "#fce7f3",
    name: "Phương Thảo",
    handle: "@thao_nguyen",
    quote:
      "Kỷ niệm 2 năm ngày yêu, nhờ web mà mình tìm được chiếc đèn ngủ quả cầu pha lê phát sáng 3D. Tối bật lên lung linh ấm cúng cực kỳ, bạn trai mê tít.",
  },
  {
    id: "t8",
    avatarId: 8,
    avatarBg: "#f3e8ff",
    name: "Quốc Bảo",
    handle: "@bao_quoc94",
    quote:
      "Đặt set quà trà thượng hạng biếu sếp nhân dịp thăng chức. Sếp khen gu tinh tế, đóng hộp gỗ sang trọng. Giao hỏa tốc 2 tiếng đúng giờ hẹn, rất hài lòng.",
  },
];

const TESTIMONIALS_COL3: TestimonialItem[] = [
  {
    id: "t9",
    avatarId: 1,
    avatarBg: "#ffedd5",
    name: "Người bạn tặng quà",
    handle: "@lananh_2026",
    quote:
      "Người bạn tặng mình cuốn lịch năm 2026 – năm con Ngựa. Ngay khi mở trang đầu tháng 1, mình bắt gặp một câu rất hay: 'Nguyên lý Thuận Dòng.' Món quà mang lại nguồn năng lượng tích cực!",
  },
  {
    id: "t10",
    avatarId: 3,
    avatarBg: "#fae8ff",
    name: "Khách hàng 06",
    handle: "@khachhang06",
    quote:
      "Cầm sản phẩm của GiftFinder trên tay là thấy đằng sau đó là cả sự dụng tâm của con người. Từ khâu đóng gói, thiết kế, in ấn... Từng chút, từng chút tỉ mỉ, tận tâm. Biết ơn mọi người lắm!",
  },
  {
    id: "t11",
    avatarId: 7,
    avatarBg: "#e0f2fe",
    name: "Hải Đăng",
    handle: "@haidang_pt",
    quote:
      "Trước giờ đau đầu nhất khoản chọn quà sinh nhật cho mẹ và bạn gái. Từ ngày dùng GiftFinder, chỉ 2 phút là chọn được món ưng ý chuẩn gu, tiết kiệm bao thời gian.",
  },
  {
    id: "t12",
    avatarId: 2,
    avatarBg: "#fef08a",
    name: "Khánh Vy",
    handle: "@vykhanh_cute",
    quote:
      "Bó hoa hướng dương vĩnh cửu kèm thiệp gửi cho em gái tốt nghiệp. Bé nhận được chụp ảnh khoe khắp Facebook. Đóng hộp chắc chắn, không bị móp méo chút nào.",
  },
];

const POPULAR_STORES = [
  {
    id: "tm",
    name: "Tiệm Quà Thảo Mộc",
    subtitle: "Xưởng quà thủ công & Vintage",
    desc: "Chuyên các set quà nến thơm hoa khô, trà thảo mộc organic và sổ tay da thủ công đóng hộp sang trọng.",
    rating: "4.9",
    reviewsCount: "15.2k",
    badge: "🌿 Thủ Công & Tinh Tế",
    badgeColor: "#059669",
    badgeBg: "#ecfdf5",
    logoBg: "#d1fae5",
    logoColor: "#065f46",
    logoText: "TM",
    tags: ["Hộp quà Vintage", "Thiệp viết tay", "Giao 2H"],
    searchQuery: "nến thơm",
  },
  {
    id: "shopee",
    name: "Shopee Mall Official",
    subtitle: "Gian hàng phân phối chính hãng",
    desc: "Tổng hợp các sản phẩm quà tặng công nghệ, phụ kiện thông minh, tai nghe và loa bluetooth chính hãng 100%.",
    rating: "4.9",
    reviewsCount: "48.6k",
    badge: "🛍️ Mall Chính Hãng 100%",
    badgeColor: "#ea580c",
    badgeBg: "#fff7ed",
    logoBg: "#ffedd5",
    logoColor: "#c2410c",
    logoText: "S",
    tags: ["Bảo hành chính hãng", "Freeship Xtra", "Đổi trả 15N"],
    searchQuery: "tai nghe bluetooth",
  },
  {
    id: "tiktok",
    name: "TikTok Shop Creator Hub",
    subtitle: "Quà tặng xu hướng & viral",
    desc: "Điểm hẹn của những món quà độc lạ viral: đèn ngủ 3D pha lê, hoa gấu vĩnh cửu, phụ kiện bắt trend giới trẻ.",
    rating: "4.8",
    reviewsCount: "32.1k",
    badge: "🎵 Xu Hướng & Độc Lạ",
    badgeColor: "#0284c7",
    badgeBg: "#f0f9ff",
    logoBg: "#e0f2fe",
    logoColor: "#0369a1",
    logoText: "TT",
    tags: ["Video thật 100%", "Hot Trend TikTok", "Giá học sinh"],
    searchQuery: "đèn ngủ pha lê",
  },
  {
    id: "select",
    name: "GiftFinder Select™",
    subtitle: "Bộ sưu tập quà tặng cao cấp",
    desc: "Dòng quà tặng thiết kế độc quyền GiftFinder: set quà sinh nhật, quà kỷ niệm, kèm hoa sáp và hộp quà thắt nơ lụa.",
    rating: "5.0",
    reviewsCount: "9.8k",
    badge: "✨ Tuyển Chọn Cao Cấp",
    badgeColor: "#e11d48",
    badgeBg: "#fff1f2",
    logoBg: "#ffe4e6",
    logoColor: "#be123c",
    logoText: "GF",
    tags: ["Thiết kế riêng", "Đóng gói chuẩn quà", "Đổi trả 30N"],
    searchQuery: "set quà",
  },
];

function getFlashSaleSlot() {
  const now = new Date();
  const midnight = new Date(
    now.getFullYear(),
    now.getMonth(),
    now.getDate(),
  ).getTime();
  const secondsSinceMidnight = Math.floor((now.getTime() - midnight) / 1000);
  const slotDuration = 4 * 3600; // 4 tiếng = 14400 giây

  const slotIndex = Math.floor(secondsSinceMidnight / slotDuration); // 0 đến 5 trong ngày
  const remainingSeconds =
    slotDuration - (secondsSinceMidnight % slotDuration);

  // Số ngày từ epoch để mỗi ngày trong tháng lại có các vòng phiên khác nhau
  const dayIndex = Math.floor(now.getTime() / (86400 * 1000));
  const globalSlot = dayIndex * 6 + slotIndex;

  return {
    remainingSeconds,
    globalSlot,
  };
}

function Home({
  go,
  openAffiliate,
  products = defaultProducts,
  onSelectProduct,
  onSelectCategory,
}: {
  go: (p: Page) => void;
  openAffiliate: (p?: ProductItem) => void;
  products?: ProductItem[];
  onSelectProduct?: (p: ProductItem) => void;
  onSelectCategory?: (
    title: string,
    slug?: string,
    parentCategory?: string,
    keyword?: string,
    minPrice?: number,
    maxPrice?: number,
  ) => void;
}) {
  const [quiz, setQuiz] = useState(0);
  const [quizAnswers, setQuizAnswers] = useState<string[]>([]);
  const quizData = [
    ["Bạn gái", "Bạn trai", "Bố mẹ", "Đồng nghiệp"],
    ["Sinh nhật", "Kỷ niệm", "Cảm ơn", "Bất ngờ"],
    ["Dưới 300k", "300k–500k", "500k–1tr", "Trên 1tr"],
  ];

  // Occasions Configuration & Filtering State
  const occasionConfigs = [
    {
      id: "all",
      label: "Tất cả",
      icon: "",
      heading: "Chọn Quà Đúng Dịp Kỷ Niệm",
      catalogTitle: "Tất cả quà tặng tuyển chọn",
      catalogSlug: "tat-ca",
      tags: [
        "Sinh nhật",
        "Kỷ niệm",
        "20/10",
        "8/3",
        "Valentine",
        "Giáng sinh",
        "Tân gia",
        "Tốt nghiệp",
      ],
      keywords: [
        "quà",
        "gift",
        "hộp quà",
        "set quà",
        "kỷ niệm",
        "sinh nhật",
        "pha lê",
        "đèn ngủ",
        "nến thơm",
        "bình giữ nhiệt",
      ],
      excludeKeywords: [
        "chuột gaming",
        "bàn phím cơ",
        "furycube",
        "tai nghe gaming",
      ],
    },
    {
      id: "sinh-nhat",
      label: "Sinh nhật",
      icon: "🎂",
      heading: "Gợi Ý Quà Tặng Sinh Nhật Ý Nghĩa",
      catalogTitle: "Quà Sinh Nhật",
      catalogSlug: "qua-sinh-nhat",
      tags: ["Sinh nhật"],
      keywords: [
        "sinh nhật",
        "birthday",
        "tuổi mới",
        "pha lê",
        "đèn ngủ",
        "hộp quà",
        "quà sinh nhật",
      ],
      excludeKeywords: ["chuột gaming", "bàn phím cơ"],
    },
    {
      id: "20-10-8-3",
      label: "20/10 & 8/3",
      icon: "💐",
      heading: "Quà Tặng Tôn Vinh Phái Đẹp 20/10 & 8/3",
      catalogTitle: "Quà 20/10 & 8/3",
      catalogSlug: "qua-20-10-8-3",
      tags: ["20/10", "8/3", "Mỹ phẩm & Làm đẹp"],
      keywords: [
        "20/10",
        "8/3",
        "phụ nữ",
        "hoa sáp",
        "bó hoa",
        "son",
        "nàng",
        "phái đẹp",
        "trang sức",
        "dây chuyền",
        "lắc tay",
        "gấu bông",
        "nến thơm",
      ],
      excludeKeywords: [
        "ví nam",
        "thắt lưng nam",
        "dây nịt nam",
        "cạo râu",
        "tông đơ",
        "chuột gaming",
      ],
    },
    {
      id: "ky-niem",
      label: "Kỷ niệm ngày yêu",
      icon: "💖",
      heading: "Quà Tặng Kỷ Niệm Ngày Yêu Lãng Mạn",
      catalogTitle: "Quà Kỷ Niệm",
      catalogSlug: "qua-ky-niem",
      tags: ["Kỷ niệm", "Valentine"],
      keywords: [
        "kỷ niệm",
        "kỉ niệm",
        "anniversary",
        "ngày yêu",
        "cặp đôi",
        "tình yêu",
        "valentine",
        "socola",
        "hoa hồng",
        "đèn ngủ 3d",
        "pha lê",
        "set quà",
      ],
      excludeKeywords: ["chuột gaming", "bàn phím cơ"],
    },
    {
      id: "giang-sinh",
      label: "Giáng sinh",
      icon: "🎄",
      heading: "Bộ Sưu Tập Quà Tặng Giáng Sinh Ấm Áp",
      catalogTitle: "Quà Giáng Sinh",
      catalogSlug: "qua-giang-sinh",
      tags: ["Giáng sinh"],
      keywords: [
        "giáng sinh",
        "noel",
        "christmas",
        "xmas",
        "cây thông",
        "ông già noel",
        "tuyết",
        "tất đỏ",
        "ly sứ",
        "nến thơm",
      ],
      excludeKeywords: ["chuột gaming", "bàn phím"],
    },
    {
      id: "tan-gia",
      label: "Tân gia",
      icon: "🏡",
      heading: "Quà Mừng Tân Gia Sang Trọng & Cát Tường",
      catalogTitle: "Quà Tân Gia",
      catalogSlug: "qua-tan-gia",
      tags: ["Tân gia", "Gia dụng & Bàn trà"],
      keywords: [
        "tân gia",
        "nhà mới",
        "ấm trà",
        "ấm chén",
        "bàn trà",
        "đèn ngủ",
        "pha lê",
        "decor",
        "trang trí",
        "phong thủy",
        "nến thơm",
        "đồng hồ",
      ],
      excludeKeywords: ["son môi", "chuột gaming", "bàn phím", "tai nghe"],
    },
    {
      id: "tot-nghiep",
      label: "Tốt nghiệp",
      icon: "🎓",
      heading: "Quà Tặng Tốt Nghiệp Chúc Mừng Thành Công",
      catalogTitle: "Quà Tốt Nghiệp",
      catalogSlug: "qua-tot-nghiep",
      tags: ["Tốt nghiệp"],
      keywords: [
        "tốt nghiệp",
        "cử nhân",
        "ra trường",
        "hướng dương",
        "bình giữ nhiệt",
        "sổ tay",
        "bút",
        "hoa tốt nghiệp",
        "gấu tốt nghiệp",
        "đồng hồ",
      ],
      excludeKeywords: ["son môi", "chuột gaming"],
    },
  ];

  const [selectedOccasionId, setSelectedOccasionId] = useState("all");
  const activeOccasion =
    occasionConfigs.find((c) => c.id === selectedOccasionId) ||
    occasionConfigs[0];

  const filteredOccasionProducts: ProductItem[] = useMemo(() => {
    // 1. Lọc sản phẩm thỏa tiêu chí của dịp
    const matched = products.filter((p: ProductItem) => {
      const pName = (p.name || "").toLowerCase();
      const pTags = (p.tags || []).map((t) => t.toLowerCase());

      // Loại trừ từ khóa không phù hợp
      if (
        activeOccasion.excludeKeywords &&
        activeOccasion.excludeKeywords.some((ex) =>
          pName.includes(ex.toLowerCase()),
        )
      ) {
        return false;
      }

      // Khớp theo Tags
      const matchTag = activeOccasion.tags.some((t) =>
        pTags.includes(t.toLowerCase()),
      );
      if (matchTag) return true;

      // Khớp theo Keywords trong tên sản phẩm
      const matchKeyword = activeOccasion.keywords.some((kw) =>
        pName.includes(kw.toLowerCase()),
      );
      return matchKeyword;
    });

    // 2. Nếu là "all" (Tất cả), lọc bỏ tech thô (chuột gaming, phím cơ) để ưu tiên quà tặng cảm xúc
    let candidateList = matched;
    if (activeOccasion.id === "all") {
      candidateList = candidateList.filter((p: ProductItem) => {
        const name = (p.name || "").toLowerCase();
        return (
          !name.includes("chuột gaming") &&
          !name.includes("furycube") &&
          !name.includes("bàn phím")
        );
      });
    }

    // 3. Fallback graceful: Đảm bảo hàng luôn hiển thị đủ 5 món sang xịn, không bao giờ bị cụt
    if (candidateList.length < 5) {
      const existingIds = new Set(candidateList.map((p: ProductItem) => p.id || p.name));
      const backupGifts = products.filter((p: ProductItem) => {
        const key = p.id || p.name;
        if (existingIds.has(key)) return false;
        const name = (p.name || "").toLowerCase();
        if (
          name.includes("chuột") ||
          name.includes("furycube") ||
          name.includes("bàn phím cơ")
        ) {
          return false;
        }
        return (
          name.includes("quà") ||
          name.includes("set") ||
          name.includes("đèn") ||
          name.includes("nến") ||
          name.includes("bình") ||
          name.includes("hoa") ||
          name.includes("gấu") ||
          (p.tags && p.tags.length > 0)
        );
      });
      return [...candidateList, ...backupGifts].slice(0, 5);
    }

    return candidateList.slice(0, 5);
  }, [products, activeOccasion]);

  // 1. Live Realtime 4-Hour Countdown Timer & Flash Sale Slot (Tự động chuyển phiên khi hết 4 tiếng)
  const [saleSlot, setSaleSlot] = useState(() => getFlashSaleSlot());

  useEffect(() => {
    const timer = setInterval(() => {
      setSaleSlot((prev) => {
        if (prev.remainingSeconds <= 1) {
          // Hết 4 tiếng -> Tự động sang phiên mới và reset đồng hồ đếm ngược
          return getFlashSaleSlot();
        }
        return {
          ...prev,
          remainingSeconds: prev.remainingSeconds - 1,
        };
      });
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const cHours = String(
    Math.floor(saleSlot.remainingSeconds / 3600),
  ).padStart(2, "0");
  const cMinutes = String(
    Math.floor((saleSlot.remainingSeconds % 3600) / 60),
  ).padStart(2, "0");
  const cSeconds = String(saleSlot.remainingSeconds % 60).padStart(2, "0");

  // Tuyển chọn 5 Set Quà / Giftbox đẹp & tự động đổi mới sau mỗi phiên 4 tiếng
  const hotGiftSets: ProductItem[] = useMemo(() => {
    // 1. Lọc tất cả các set quà / hộp quà / giftbox thực thụ trong kho
    const candidateSets = products.filter((p: ProductItem) => {
      const name = (p.name || "").toLowerCase();
      const tags = (p.tags || []).map((t) => t.toLowerCase());

      // Loại bỏ hoàn toàn đồ công nghệ, chuột gaming, linh kiện PC
      if (
        name.includes("chuột") ||
        name.includes("furycube") ||
        name.includes("lót chuột") ||
        name.includes("bàn phím") ||
        name.includes("tai nghe")
      ) {
        return false;
      }

      // Khớp từ khóa và tag dạng set quà
      const isSet =
        name.includes("set quà") ||
        name.includes("hộp quà") ||
        name.includes("giftbox") ||
        name.includes("combo quà") ||
        name.includes("hộp túi quà") ||
        name.includes("set hộp") ||
        name.includes("set túi") ||
        tags.some(
          (t) =>
            t.includes("giftbox") ||
            t.includes("set quà") ||
            t.includes("thủ công"),
        );

      return isSet;
    });

    // 2. Ưu tiên các món có giá quà tặng phổ biến (50k - 800k)
    const sortedSets = [...candidateSets].sort((a, b) => {
      const aInRange = a.price >= 50000 && a.price <= 800000 ? 1 : 0;
      const bInRange = b.price >= 50000 && b.price <= 800000 ? 1 : 0;
      return bInRange - aInRange;
    });

    const themes = [
      ["nến", "tinh dầu", "diffuser", "citta"], // 1. Nến thơm tinh dầu
      ["thắt lưng", "ví da", "cho nam", "dây nịt", "bóp"], // 2. Quà nam đóng hộp
      ["sweet", "túi quà", "gấu", "hoa lụa", "giáng sinh", "thiệp"], // 3. Hộp túi quà / Gấu bông / Hoa
      ["yến", "trà", "sâm", "sức khỏe", "ấm chén"], // 4. Sức khỏe / Yến sào / Trà quý
      ["cỏ mềm", "son", "mỹ phẩm", "dưỡng da", "tơ tằm"], // 5. Mỹ phẩm / Dưỡng da
    ];

    const selectedList: ProductItem[] = [];
    const usedIds = new Set<string>();
    const slotOffset = saleSlot.globalSlot;

    for (const keywords of themes) {
      const matches = sortedSets.filter((p) => {
        const name = (p.name || "").toLowerCase();
        return keywords.some((kw) => name.includes(kw));
      });

      if (matches.length > 0) {
        let picked: ProductItem | null = null;
        for (let i = 0; i < matches.length; i++) {
          const candidate = matches[(slotOffset + i) % matches.length];
          const id = candidate.id || candidate.name;
          if (!usedIds.has(id)) {
            picked = candidate;
            break;
          }
        }

        if (picked) {
          selectedList.push(picked);
          usedIds.add(picked.id || picked.name);
        }
      }
    }

    // Nếu chưa đủ 5 món, lấy bù từ các set quà đẹp khác xoay vòng theo slot
    if (selectedList.length < 5 && sortedSets.length > 0) {
      for (let i = 0; i < sortedSets.length; i++) {
        const candidate = sortedSets[(slotOffset + i) % sortedSets.length];
        const id = candidate.id || candidate.name;
        if (!usedIds.has(id)) {
          selectedList.push(candidate);
          usedIds.add(id);
          if (selectedList.length >= 5) break;
        }
      }
    }

    // Nếu kho chưa tải xong (cold start), fallback các món quà mặc định tinh tế
    if (selectedList.length < 5) {
      for (let i = 0; i < products.length; i++) {
        const p = products[(slotOffset + i) % products.length];
        const id = p.id || p.name;
        if (!usedIds.has(id)) {
          const name = (p.name || "").toLowerCase();
          if (
            !name.includes("chuột") &&
            !name.includes("furycube") &&
            !name.includes("bàn phím")
          ) {
            selectedList.push(p);
            usedIds.add(id);
            if (selectedList.length >= 5) break;
          }
        }
      }
    }

    return selectedList.slice(0, 5);
  }, [products, saleSlot.globalSlot]);

  // 2. Sản phẩm nổi bật được yêu thích (Top Bán Chạy vs Đánh Giá Cao)
  const [featuredTab, setFeaturedTab] = useState<"best_seller" | "top_rated">(
    "best_seller",
  );

  const featuredProducts: ProductItem[] = useMemo(() => {
    // Lọc bỏ phụ kiện máy tính, linh kiện PC thô
    const giftItems = products.filter((p: ProductItem) => {
      const name = (p.name || "").toLowerCase();
      const pPrice = p.price || 0;
      const notTech =
        !name.includes("chuột") &&
        !name.includes("furycube") &&
        !name.includes("lót chuột") &&
        !name.includes("bàn phím") &&
        !name.includes("tai nghe gaming") &&
        !name.includes("hoco w113");
      return notTech && pPrice >= 50000;
    });

    if (featuredTab === "best_seller") {
      // Top Bán Chạy: Lọc và sắp xếp theo lượt bán (reviews / sold) cao nhất
      const parseSold = (s?: string) => {
        if (!s) return 0;
        if (s.endsWith("k")) return parseFloat(s) * 1000;
        const num = parseInt(s.replace(/\D/g, ""), 10);
        return isNaN(num) ? 0 : num;
      };

      const sorted = [...giftItems].sort((a, b) => {
        const soldA = parseSold(a.sold);
        const soldB = parseSold(b.sold);
        if (soldB !== soldA) return soldB - soldA;
        return parseFloat(b.rating || "0") - parseFloat(a.rating || "0");
      });

      // Lấy 5 món đa dạng (tránh các món tên quá giống nhau)
      const result: ProductItem[] = [];
      const seenNames = new Set<string>();
      for (const p of sorted) {
        const shortName = p.name.slice(0, 15).toLowerCase();
        if (!seenNames.has(shortName)) {
          result.push(p);
          seenNames.add(shortName);
          if (result.length >= 5) break;
        }
      }
      return result.length >= 5 ? result : sorted.slice(0, 5);
    } else {
      // Đánh giá cao nhất (4.9★ - 5.0★)
      const topRated = [...giftItems]
        .filter((p) => parseFloat(p.rating || "0") >= 4.8)
        .sort((a, b) => {
          const rA = parseFloat(a.rating || "0");
          const rB = parseFloat(b.rating || "0");
          if (rB !== rA) return rB - rA;
          return (b.price || 0) - (a.price || 0);
        });

      const result: ProductItem[] = [];
      const seenNames = new Set<string>();
      for (const p of topRated) {
        const shortName = p.name.slice(0, 15).toLowerCase();
        if (!seenNames.has(shortName)) {
          result.push(p);
          seenNames.add(shortName);
          if (result.length >= 5) break;
        }
      }
      return result.length >= 5 ? result : topRated.slice(0, 5);
    }
  }, [products, featuredTab]);

  // 2. Animated Auto-Typing AI Search with interactive tags
  const samplePrompts = [
    "Quà sinh nhật tặng bạn gái 25 tuổi tầm 500k nhẹ nhàng...",
    "Set nến thơm & trà hoa tặng mẹ ngày 20/10 ấm áp...",
    "Món quà công nghệ bất ngờ kỷ niệm 1 năm ngày yêu...",
    "Set quà thủ công độc bản chúc mừng tân gia đồng nghiệp...",
  ];
  const [promptInput, setPromptInput] = useState("");
  const [isManualInput, setIsManualInput] = useState(false);
  const [promptIdx, setPromptIdx] = useState(0);
  const [charIdx, setCharIdx] = useState(0);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    if (isManualInput) return;

    const currentText = samplePrompts[promptIdx];
    let timeout: ReturnType<typeof setTimeout>;

    if (!isDeleting && charIdx < currentText.length) {
      timeout = setTimeout(() => {
        setPromptInput(currentText.slice(0, charIdx + 1));
        setCharIdx(charIdx + 1);
      }, 50);
    } else if (!isDeleting && charIdx === currentText.length) {
      timeout = setTimeout(() => setIsDeleting(true), 2500);
    } else if (isDeleting && charIdx > 0) {
      timeout = setTimeout(() => {
        setPromptInput(currentText.slice(0, charIdx - 1));
        setCharIdx(charIdx - 1);
      }, 25);
    } else if (isDeleting && charIdx === 0) {
      setIsDeleting(false);
      setPromptIdx((promptIdx + 1) % samplePrompts.length);
    }

    return () => clearTimeout(timeout);
  }, [charIdx, isDeleting, promptIdx, isManualInput]);

  // 3. Hero Visual Carousel with Auto-Rotate (3 ảnh Studio Bục Đá Quà Tặng AI cao cấp)
  const heroList = [
    images.heroLuxuryPodium,
    images.heroTechPodium,
    images.heroWellnessPodium,
  ];
  const heroBadges = [
    "🔥 Set Quà Nàng Thơ",
    "⚡ Công Nghệ & Gaming",
    "🌿 Ấm Áp Tình Thân",
  ];
  const [heroIdx, setHeroIdx] = useState(0);
  useEffect(() => {
    const timer = setInterval(() => {
      setHeroIdx((prev) => (prev + 1) % heroList.length);
    }, 4500);
    return () => clearInterval(timer);
  }, [heroIdx, heroList.length]);

  const handleQuickTag = (tag: string) => {
    setIsManualInput(true);
    setPromptInput(`Tìm quà ${tag} phù hợp nhất...`);
  };

  return (
    <div>
      {/* ========================================================
          1. POSTER HERO BANNER & AI CONCIERGE
          ======================================================== */}
      <section className="home-hero shell">
        <div className="hero-copy">
          <Badge tone="rose">
            <Icon name="sparkle" size={14} /> AI GIFT CONCIERGE
          </Badge>
          <h1>
            Chọn quà hoàn hảo
            <br />
            trong <em>3 giây</em> — Cá nhân hóa bằng AI
          </h1>
          <p>
            Kể cho GiftFinder về người bạn thương. AI sẽ chọn đúng món quà, đúng
            gu và đúng ngân sách từ những gian hàng đáng tin cậy.
          </p>
          <div className="ai-search">
            <div>
              <Icon name="sparkle" />
              <textarea
                placeholder="Ví dụ: Quà sinh nhật tặng bạn gái 25 tuổi tầm 500k..."
                value={promptInput}
                onChange={(e) => {
                  setIsManualInput(true);
                  setPromptInput(e.target.value);
                }}
              />
            </div>
            <div className="ai-search-bottom">
              <span>Thử nhanh</span>
              {[
                "200 cành",
                "5 lốp",
                "1 củ",
                "Cho nàng",
                "Game thủ",
                "Kỷ niệm",
              ].map((x) => (
                <button
                  key={x}
                  onClick={() => handleQuickTag(x)}
                  style={{ transition: "all 0.2s ease", cursor: "pointer" }}
                >
                  {x}
                </button>
              ))}
              <Button
                variant="rose"
                onClick={() => {
                  const q = promptInput.trim().replace(/\.\.\.$/, "");
                  if (onSelectCategory) {
                    const lower = q.toLowerCase();
                    let targetSlug = "tat-ca";
                    if (lower.includes("bạn gái") || lower.includes("cho nàng"))
                      targetSlug = "qua-cho-nang";
                    else if (
                      lower.includes("bạn trai") ||
                      lower.includes("cho chàng")
                    )
                      targetSlug = "qua-cho-chang";
                    else if (
                      lower.includes("mẹ") ||
                      lower.includes("20/10") ||
                      lower.includes("8/3")
                    )
                      targetSlug = "qua-cho-me";
                    else if (lower.includes("bố") || lower.includes("ba "))
                      targetSlug = "qua-cho-bo";
                    else if (
                      lower.includes("công nghệ") ||
                      lower.includes("game")
                    )
                      targetSlug = "do-cong-nghe-gaming";
                    else if (lower.includes("tân gia"))
                      targetSlug = "qua-tan-gia-gia-dinh";
                    else if (lower.includes("đồng nghiệp"))
                      targetSlug = "qua-cho-dong-nghiep";
                    else if (lower.includes("kỷ niệm"))
                      targetSlug = "qua-ky-niem";
                    else if (lower.includes("sinh nhật"))
                      targetSlug = "qua-sinh-nhat";

                    let maxP: number | undefined;
                    if (lower.includes("200 cành") || lower.includes("200k"))
                      maxP = 300000;
                    else if (lower.includes("5 lốp") || lower.includes("500k"))
                      maxP = 600000;
                    else if (lower.includes("1 củ") || lower.includes("1tr"))
                      maxP = 1200000;

                    onSelectCategory(
                      q ? `Khám phá: ${q}` : "Tất cả quà tặng tuyển chọn",
                      targetSlug,
                      "Khám phá quà tặng",
                      targetSlug === "tat-ca" ? q : "",
                      undefined,
                      maxP,
                    );
                  } else {
                    go("catalog");
                  }
                }}
              >
                Khám phá ngay <Icon name="arrow" />
              </Button>
            </div>
          </div>
          <div className="hero-trust">
            <span>
              <Icon name="check" size={15} /> 260+ quà thật 100%
            </span>
            <span>
              <Icon name="shield" size={15} /> Đối tác Shopee & TikTok Shop
            </span>
          </div>
        </div>
        <div className="hero-visual">
          <div className="hero-glow-layer" />
          <div className="hero-carousel-container">
            {heroList.map((src, i) => (
              <img
                key={src}
                src={src}
                alt="Hộp quà cao cấp GiftFinder"
                className={`hero-slide-img ${heroIdx === i ? "active" : ""}`}
              />
            ))}
          </div>
          <Badge tone="dark" className="hero-badge-animated">
            {heroBadges[heroIdx]}
          </Badge>

          {/* Nút điều hướng Carousel 2 bên */}
          <button
            type="button"
            className="carousel-arrow prev"
            onClick={() =>
              setHeroIdx(
                (prev) => (prev - 1 + heroList.length) % heroList.length,
              )
            }
            aria-label="Ảnh trước"
          >
            ‹
          </button>
          <button
            type="button"
            className="carousel-arrow next"
            onClick={() => setHeroIdx((prev) => (prev + 1) % heroList.length)}
            aria-label="Ảnh tiếp theo"
          >
            ›
          </button>

          <div className="carousel-dots">
            {heroList.map((_, i) => (
              <i
                key={i}
                className={heroIdx === i ? "active" : ""}
                onClick={() => setHeroIdx(i)}
                aria-label={`Chuyển đến ảnh ${i + 1}`}
              />
            ))}
          </div>
        </div>
      </section>

      {/* ========================================================
          2. HÔM NAY BẠN ĐANG TÌM QUÀ CHO AI? (Gift by Recipient)
          ======================================================== */}
      <section className="section cream">
        <div className="shell">
          <SectionTitle
            eyebrow="MÓN QUÀ ĐÚNG NGƯỜI"
            title="Hôm Nay Bạn Đang Tìm Quà Cho Ai?"
            action={
              <button
                className="text-link"
                onClick={() => {
                  if (onSelectCategory) {
                    onSelectCategory(
                      "Tất cả quà tặng tuyển chọn",
                      "tat-ca",
                      "Danh mục quà tặng",
                    );
                  } else {
                    go("catalog");
                  }
                }}
              >
                Xem tất cả đối tượng <Icon name="arrow" size={15} />
              </button>
            }
          />
          <div className="category-grid">
            {[
              ["Cho Nàng", images.flower, "Dịu dàng & tinh tế", "qua-cho-nang"],
              [
                "Cho Chàng",
                images.blue,
                "Lịch lãm & khác biệt",
                "qua-cho-chang",
              ],
              ["Quà Bố Mẹ", images.tray, "Ấm áp & ý nghĩa", "qua-bo-me"],
              [
                "Đồng Nghiệp",
                images.candle,
                "Trang nhã & vừa vặn",
                "qua-cho-dong-nghiep",
              ],
              [
                "Sinh Nhật Bạn Thân",
                images.macaron,
                "Rực rỡ & bất ngờ",
                "sinh-nhat-ban-than",
              ],
              [
                "Kỷ Niệm Ngày Yêu",
                images.rose,
                "Lãng mạn & đáng nhớ",
                "qua-ky-niem",
              ],
            ].map((c, i) => (
              <button
                className={`category-card c${i}`}
                key={c[0]}
                onClick={() => {
                  if (onSelectCategory) {
                    onSelectCategory(
                      `Quà Tặng ${c[0]}`,
                      c[3],
                      "Quà Theo Người Nhận",
                    );
                  } else {
                    go("catalog");
                  }
                }}
              >
                <img src={c[1]} alt="" />
                <span>
                  <b>{c[0]}</b>
                  <small>{c[2]}</small>
                </span>
                <Icon name="arrow" />
              </button>
            ))}
          </div>
        </div>
      </section>

      {/* ========================================================
          3. CHỌN QUÀ ĐÚNG DỊP (Gift by Occasion)
          ======================================================== */}
      <section className="section shell">
        <SectionTitle
          eyebrow="ĐÚNG DỊP - ĐÚNG Ý"
          title={activeOccasion.heading}
          action={
            <button
              className="text-link"
              onClick={() => {
                if (onSelectCategory) {
                  onSelectCategory(
                    activeOccasion.catalogTitle,
                    activeOccasion.catalogSlug,
                    "Quà Theo Dịp",
                  );
                } else {
                  go("catalog");
                }
              }}
            >
              {activeOccasion.id === "all"
                ? "Xem tất cả quà theo dịp"
                : `Xem tất cả quà ${activeOccasion.label}`}{" "}
              <Icon name="arrow" size={15} />
            </button>
          }
        />
        <div className="occasion-pills">
          {occasionConfigs.map((occ) => (
            <button
              key={occ.id}
              className={`occasion-pill ${selectedOccasionId === occ.id ? "active" : ""}`}
              onClick={() => setSelectedOccasionId(occ.id)}
            >
              {occ.icon ? `${occ.icon} ${occ.label}` : occ.label}
            </button>
          ))}
        </div>
        <div
          key={selectedOccasionId}
          className="product-row occasion-product-row"
        >
          {filteredOccasionProducts.map((p, i) => (
            <ProductCard
              key={p.id || p.name + i}
              product={p}
              onOpen={() => {
                onSelectProduct?.(p);
              }}
              onAffiliate={() => openAffiliate(p)}
            />
          ))}
        </div>
      </section>

      {/* ========================================================
          4. QUÀ HOT HÔM NAY (Flash Deals & Live Countdown)
          ======================================================== */}
      <section className="section cream">
        <div className="shell">
          <SectionTitle
            eyebrow="CHỈ CÒN HÔM NAY"
            title="⚡ Quà Hot Hôm Nay - Deal Giảm Đến 35%"
            action={
              <div
                className="countdown"
                style={{ cursor: "pointer" }}
                onClick={() =>
                  onSelectCategory?.(
                    "⚡ Flash Sale — Deal Quà Hot Hôm Nay",
                    "flash-sale",
                    "Flash Sale",
                  )
                }
                title="Bấm để xem tất cả deal quà hot hôm nay"
              >
                <span>Kết thúc sau</span>
                <b>{cHours}</b>:<b>{cMinutes}</b>:
                <b style={{ color: "#f43f5e" }}>{cSeconds}</b>
              </div>
            }
          />
          <div
            key={saleSlot.globalSlot}
            className="product-row occasion-product-row"
          >
            {hotGiftSets.map((p, i) => (
              <ProductCard
                key={p.id || p.name + i}
                product={p}
                onOpen={() => {
                  onSelectProduct?.(p);
                }}
                onAffiliate={() => openAffiliate(p)}
              />
            ))}
          </div>
        </div>
      </section>

      {/* ========================================================
          5. SẢN PHẨM NỔI BẬT (Featured Placement UC-17 / UC-24)
          ======================================================== */}
      <section className="section shell">
        <SectionTitle
          eyebrow="ĐƯỢC YÊU THÍCH NHẤT"
          title="Sản Phẩm Nổi Bật Được Yêu Thích"
          action={
            <div className="featured-toggle-tabs">
              <button
                className={`featured-tab-pill ${featuredTab === "best_seller" ? "active" : ""}`}
                onClick={() => setFeaturedTab("best_seller")}
              >
                🏆 Top Bán Chạy
              </button>
              <button
                className={`featured-tab-pill ${featuredTab === "top_rated" ? "active" : ""}`}
                onClick={() => setFeaturedTab("top_rated")}
              >
                ⭐ Đánh Giá Cao (4.9★)
              </button>
            </div>
          }
        />
        <div
          key={featuredTab}
          className="product-row occasion-product-row"
        >
          {featuredProducts.map((p, i) => (
            <ProductCard
              key={p.id || p.name + i}
              product={p}
              onOpen={() => {
                onSelectProduct?.(p);
              }}
              onAffiliate={() => openAffiliate(p)}
            />
          ))}
        </div>
      </section>

      {/* ========================================================
          6. SET QUÀ THEO DỊP (Curated Combo Gift Boxes)
          ======================================================== */}
      <section className="section cream">
        <div className="shell">
          <SectionTitle
            eyebrow="TRỌN VẸN YÊU THƯƠNG"
            title="Set Quà Theo Dịp Đóng Hộp Kèm Thiệp"
            action={
              <button className="text-link" onClick={() => go("catalog")}>
                Xem tất cả combo <Icon name="arrow" size={15} />
              </button>
            }
          />
          <div className="combos-grid">
            {combos.map((combo) => (
              <article key={combo.name} className="combo-card">
                <div className="combo-photo">
                  <img src={combo.image} alt={combo.name} />
                  <span
                    className="discount"
                    style={{ position: "absolute", top: 10, left: 10 }}
                  >
                    {combo.occasion}
                  </span>
                </div>
                <div className="combo-body">
                  <h3
                    style={{
                      fontSize: 13,
                      margin: "0 0 6px",
                      fontWeight: 700,
                      color: "var(--ink)",
                    }}
                  >
                    {combo.name}
                  </h3>
                  <div className="combo-items">
                    📦 <b>Combo gồm:</b> {combo.items}
                  </div>
                  <div
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                      marginTop: "auto",
                      paddingTop: 8,
                    }}
                  >
                    <div>
                      <b style={{ color: "var(--rose)", fontSize: 15 }}>
                        {money(combo.price)}
                      </b>
                      <del
                        style={{
                          color: "#94a3b8",
                          fontSize: 10,
                          marginLeft: 6,
                        }}
                      >
                        {money(combo.old)}
                      </del>
                    </div>
                    <Button variant="rose" onClick={() => go("product")}>
                      Xem set
                    </Button>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </div>
      </section>

      {/* ========================================================
          7. SẢN PHẨM LIÊN QUAN & PHỤ KIỆN TẶNG KÈM (Related Add-ons)
          ======================================================== */}
      <section className="section shell">
        <SectionTitle
          eyebrow="HOÀN HẢO TỪNG CHI TIẾT"
          title="Sản Phẩm Liên Quan & Phụ Kiện Quà Tặng"
          action={<Badge tone="green">Chỉ từ 12.000đ</Badge>}
        />
        <div className="addons-grid">
          {addons.map((addon) => (
            <div key={addon.name} className="addon-card">
              <img src={addon.image} alt={addon.name} />
              <b>{addon.name}</b>
              <span>+{money(addon.price)}</span>
              <button
                onClick={() => go("cart")}
                style={{
                  marginTop: 8,
                  border: "1px solid #e2e8f0",
                  background: "#f8fafc",
                  padding: "4px 8px",
                  borderRadius: 6,
                  fontSize: 9,
                  fontWeight: 600,
                  cursor: "pointer",
                  color: "var(--slate)",
                }}
              >
                + Thêm vào quà
              </button>
            </div>
          ))}
        </div>
      </section>

      {/* ========================================================
          8. AN TÂM MUA SẮM (4 Trụ Cột Tín Nhiệm & Bảo Mật VNPAY)
          ======================================================== */}
      <section className="section cream">
        <div className="shell">
          <SectionTitle
            eyebrow="CAM KẾT CHẤT LƯỢNG"
            title="An Tâm Mua Sắm Cùng GiftFinder"
          />
          <div className="trust-grid">
            <div className="trust-card">
              <div
                className="trust-icon-box"
                style={{ background: "#ecfdf5", color: "#16a34a" }}
              >
                <Icon name="shield" size={24} />
              </div>
              <h3>100% Quà Thật Chính Hãng</h3>
              <p>
                Mọi món quà đều được tuyển chọn từ Shopee Mall, TikTok Shop và
                xưởng sản xuất uy tín, kiểm định kỹ lưỡng.
              </p>
            </div>

            <div className="trust-card">
              <div
                className="trust-icon-box"
                style={{ background: "#eef2ff", color: "#4f46e5" }}
              >
                <span style={{ fontSize: 18, fontWeight: 900 }}>VN</span>
              </div>
              <h3>Thanh Toán Bảo Mật VNPAY</h3>
              <p>
                Cổng thanh toán Quốc gia VNPAY chuẩn an toàn PCI DSS. Quét mã
                VNPAY-QR, thẻ ATM 30+ ngân hàng và thẻ quốc tế.
              </p>
            </div>

            <div className="trust-card">
              <div
                className="trust-icon-box"
                style={{ background: "#fff1f2", color: "#e11d48" }}
              >
                <Icon name="check" size={24} />
              </div>
              <h3>Đổi Trả Trong 7 Ngày</h3>
              <p>
                Cam kết 1 đổi 1 hoặc hoàn tiền 100% nếu sản phẩm phát sinh lỗi,
                vỡ hỏng trong quá trình vận chuyển đến tay bạn.
              </p>
            </div>

            <div className="trust-card">
              <div
                className="trust-icon-box"
                style={{ background: "#fef3c7", color: "#d97706" }}
              >
                <Icon name="sparkle" size={24} />
              </div>
              <h3>Giao Hỏa Tốc & Thiệp Riêng</h3>
              <p>
                Giao nhanh trong 2h tại Hà Nội & TP.HCM. Tặng kèm thiệp viết tay
                nắn nót theo đúng lời nhắn gửi của bạn.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* ========================================================
          9. TRẮC NGHIỆM NHANH AI QUICK QUIZ
          ======================================================== */}
      <section className="section shell quiz-section">
        <div className="quiz-intro">
          <Badge tone="indigo">
            <Icon name="sparkle" size={14} /> AI QUICK QUIZ
          </Badge>
          <h2>
            Chưa biết chọn gì?
            <br />
            Để AI hiểu người bạn thương.
          </h2>
          <p>
            Chỉ 3 câu hỏi nhanh. GiftFinder sẽ lọc hàng trăm món quà xuống những
            lựa chọn phù hợp nhất.
          </p>
          <div className="quiz-progress">
            {[0, 1, 2].map((i) => (
              <i key={i} className={quiz >= i ? "active" : ""} />
            ))}
          </div>
        </div>
        <div className="quiz-card">
          <div key={quiz} className="quiz-step-anim">
            <small>BƯỚC {quiz + 1} / 3</small>
            <h3>
              {
                [
                  "Ai là người nhận?",
                  "Dịp tặng quà là gì?",
                  "Mức ngân sách của bạn?",
                ][quiz]
              }
            </h3>
            <div className="quiz-options">
              {quizData[quiz].map((x) => (
                <button
                  key={x}
                  onClick={() => {
                    const nextAnswers = [...quizAnswers];
                    nextAnswers[quiz] = x;
                    setQuizAnswers(nextAnswers);
                    if (quiz < 2) {
                      setQuiz(quiz + 1);
                    } else {
                      const rec = nextAnswers[0] || "Bạn gái";
                      const occ = nextAnswers[1] || "Sinh nhật";
                      const slugMap: Record<string, string> = {
                        "Bạn gái": "qua-cho-nang",
                        "Bạn trai": "qua-cho-chang",
                        "Bố mẹ": "qua-bo-me",
                        "Đồng nghiệp": "qua-cho-dong-nghiep",
                      };
                      let minP: number | undefined;
                      let maxP: number | undefined;
                      if (x === "Dưới 300k") maxP = 300000;
                      else if (x === "300k–500k") {
                        minP = 300000;
                        maxP = 500000;
                      } else if (x === "500k–1tr") {
                        minP = 500000;
                        maxP = 1000000;
                      } else if (x === "Trên 1tr") {
                        minP = 1000000;
                      }
                      const targetSlug = slugMap[rec] || "tat-ca";
                      if (onSelectCategory) {
                        onSelectCategory(
                          `Quà ${occ} cho ${rec} (${x})`,
                          targetSlug,
                          "Trắc Nghiệm Chọn Quà",
                          "",
                          minP,
                          maxP,
                        );
                      } else {
                        go("catalog");
                      }
                    }
                  }}
                >
                  {x}
                  <Icon name="chevron" size={16} />
                </button>
              ))}
            </div>
            {quiz === 2 && (
              <Button
                variant="rose"
                onClick={() => {
                  const rec = quizAnswers[0] || "Bạn gái";
                  const occ = quizAnswers[1] || "Sinh nhật";
                  const slugMap: Record<string, string> = {
                    "Bạn gái": "qua-cho-nang",
                    "Bạn trai": "qua-cho-chang",
                    "Bố mẹ": "qua-bo-me",
                    "Đồng nghiệp": "qua-cho-dong-nghiep",
                  };
                  const targetSlug = slugMap[rec] || "tat-ca";
                  if (onSelectCategory) {
                    onSelectCategory(
                      `Quà ${occ} cho ${rec}`,
                      targetSlug,
                      "Trắc Nghiệm Chọn Quà",
                    );
                  } else {
                    go("catalog");
                  }
                }}
              >
                Xem gợi ý phù hợp ngay
              </Button>
            )}
          </div>
        </div>
      </section>

      {/* ========================================================
          10. GIAN HÀNG ĐƯỢC YÊU THÍCH (PARTNER STORES)
          ======================================================== */}
      <section className="section shell stores-section">
        <SectionTitle
          eyebrow="ĐỐI TÁC CHÍNH HÃNG & UY TÍN"
          title="Gian Hàng Được Yêu Thích"
          action={
            <button className="text-link" onClick={() => go("catalog")}>
              Xem tất cả gian hàng <Icon name="arrow" size={15} />
            </button>
          }
        />
        <div className="stores-grid">
          {POPULAR_STORES.map((store) => (
            <article
              key={store.id}
              className="store-card"
              onClick={() => {
                if (onSelectCategory) {
                  onSelectCategory(
                    store.name,
                    "tat-ca",
                    "Gian Hàng Được Yêu Thích",
                    store.searchQuery,
                  );
                } else {
                  go("catalog");
                }
              }}
            >
              <div
                className="store-card-badge"
                style={{
                  backgroundColor: store.badgeBg,
                  color: store.badgeColor,
                }}
              >
                {store.badge}
              </div>

              <div className="store-card-header">
                <div
                  className="store-card-logo"
                  style={{
                    backgroundColor: store.logoBg,
                    color: store.logoColor,
                  }}
                >
                  {store.logoText}
                </div>
                <div className="store-card-info">
                  <h4 className="store-card-name">
                    {store.name}
                    <span
                      className="store-card-check"
                      title="Gian hàng đã xác minh"
                    >
                      ✓
                    </span>
                  </h4>
                  <span className="store-card-sub">{store.subtitle}</span>
                </div>
              </div>

              <p className="store-card-desc">{store.desc}</p>

              <div className="store-card-tags">
                {store.tags.map((t) => (
                  <span key={t} className="store-card-tag">
                    {t}
                  </span>
                ))}
              </div>

              <div className="store-card-footer">
                <span className="store-card-stats">
                  ⭐ <b>{store.rating}</b> ({store.reviewsCount})
                </span>
                <span className="store-card-action">
                  Khám phá quà <Icon name="chevron" size={14} />
                </span>
              </div>
            </article>
          ))}
        </div>
      </section>

      {/* ========================================================
          11. KHÁCH HÀNG HÀI LÒNG (INFINITE VERTICAL MARQUEE)
          ======================================================== */}
      <section className="section shell testimonials-section">
        <div className="testimonials-header">
          <h2 className="testimonials-title">KHÁCH HÀNG HÀI LÒNG</h2>
          <p className="testimonials-subtitle">
            Hơn 50,000+ món quà ý nghĩa đã được trao gửi. Cùng xem khách hàng
            nói gì về trải nghiệm tại GiftFinder!
          </p>
        </div>

        <div className="testimonials-marquee-wrapper">
          <div className="testimonials-grid">
            {/* Cột 1: Cuộn lên chậm */}
            <div className="marquee-column marquee-col-1">
              <div className="marquee-track-inner scroll-up">
                <div className="marquee-group">
                  {TESTIMONIALS_COL1.map((item) => (
                    <TestimonialCard key={`c1-a-${item.id}`} item={item} />
                  ))}
                </div>
                <div className="marquee-group" aria-hidden="true">
                  {TESTIMONIALS_COL1.map((item) => (
                    <TestimonialCard key={`c1-b-${item.id}`} item={item} />
                  ))}
                </div>
              </div>
            </div>

            {/* Cột 2: Cuộn xuống chậm */}
            <div className="marquee-column marquee-col-2">
              <div className="marquee-track-inner scroll-down">
                <div className="marquee-group">
                  {TESTIMONIALS_COL2.map((item) => (
                    <TestimonialCard key={`c2-a-${item.id}`} item={item} />
                  ))}
                </div>
                <div className="marquee-group" aria-hidden="true">
                  {TESTIMONIALS_COL2.map((item) => (
                    <TestimonialCard key={`c2-b-${item.id}`} item={item} />
                  ))}
                </div>
              </div>
            </div>

            {/* Cột 3: Cuộn lên chậm với tốc độ lệch nhẹ */}
            <div className="marquee-column marquee-col-3">
              <div className="marquee-track-inner scroll-up col3-speed">
                <div className="marquee-group">
                  {TESTIMONIALS_COL3.map((item) => (
                    <TestimonialCard key={`c3-a-${item.id}`} item={item} />
                  ))}
                </div>
                <div className="marquee-group" aria-hidden="true">
                  {TESTIMONIALS_COL3.map((item) => (
                    <TestimonialCard key={`c3-b-${item.id}`} item={item} />
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
}

export function getCategoryHeroMeta(
  slug: string,
  title: string,
  parent: string,
  keyword?: string,
  fallbackDesc?: string,
): { eyebrow: string; tagline: string } {
  if (keyword) {
    return {
      eyebrow: "BỘ SƯU TẬP • KẾT QUẢ TÌM KIẾM",
      tagline: `Tuyển chọn những món quà phù hợp nhất với từ khóa "${keyword}".`,
    };
  }

  const normalizedSlug = slug?.toLowerCase().trim() || "";

  const dict: Record<string, { eyebrow: string; tagline: string }> = {
    // Quà theo người nhận
    "qua-cho-me": {
      eyebrow: "BỘ SƯU TẬP • QUÀ TẶNG MẸ",
      tagline:
        "Trao gửi trọn vẹn lòng biết ơn và sự yêu thương sâu sắc đến người phụ nữ tuyệt vời nhất.",
    },
    "qua-ngay-cua-me": {
      eyebrow: "BỘ SƯU TẬP • NGÀY CỦA MẸ",
      tagline:
        "Trao gửi trọn vẹn lòng biết ơn và sự yêu thương sâu sắc đến người phụ nữ tuyệt vời nhất.",
    },
    "qua-cho-nang": {
      eyebrow: "BỘ SƯU TẬP • DÀNH CHO NÀNG",
      tagline:
        "Cho những khoảnh khắc kỷ niệm vô giá & yêu thương nồng nàn thêm trọn vẹn.",
    },
    "qua-cho-chang": {
      eyebrow: "BỘ SƯU TẬP • DÀNH CHO CHÀNG",
      tagline: "Thiết thực, phong cách và tinh tế dành riêng cho phái mạnh.",
    },
    "qua-cho-bo": {
      eyebrow: "BỘ SƯU TẬP • QUÀ TẶNG BỐ",
      tagline:
        "Món quà sức khỏe và sự kính trọng gửi trao đến người cha kính yêu.",
    },
    "qua-bo-me": {
      eyebrow: "BỘ SƯU TẬP • TRI ÂN ĐẤNG SINH THÀNH",
      tagline: "Gửi trao sức khỏe, an yên và sự hiếu kính trọn vẹn đến cha mẹ.",
    },
    "qua-cho-dong-nghiep": {
      eyebrow: "BỘ SƯU TẬP • ĐỒNG NGHIỆP & BẠN BÈ",
      tagline:
        "Món quà tinh tế, gắn kết tình đồng đội và lan tỏa năng lượng tích cực.",
    },
    "qua-cho-sep": {
      eyebrow: "BỘ SƯU TẬP • QUÀ BIẾU SẾP & LÃNH ĐẠO",
      tagline: "Đẳng cấp, chỉn chu và nâng tầm mối quan hệ đối tác bền chặt.",
    },
    "qua-cho-be": {
      eyebrow: "BỘ SƯU TẬP • THIÊN THẦN NHỎ",
      tagline:
        "Nuôi dưỡng tuổi thơ diệu kỳ bằng những món quà an toàn, đáng yêu và bổ ích.",
    },

    // Quà theo dịp
    "qua-sinh-nhat": {
      eyebrow: "BỘ SƯU TẬP • SINH NHẬT Ý NGHĨA",
      tagline:
        "Mở hộp là mở niềm vui, bất ngờ ngọt ngào mừng tuổi mới thêm rạng rỡ.",
    },
    "qua-ky-niem": {
      eyebrow: "BỘ SƯU TẬP • KỶ NIỆM NGÀY YÊU",
      tagline:
        "Ghi dấu những cột mốc thiêng liêng và ngọt ngào trên hành trình chung đôi.",
    },
    "qua-tot-nghiep": {
      eyebrow: "BỘ SƯU TẬP • CHÚC MỪNG TỐT NGHIỆP",
      tagline:
        "Mừng bước ngoặt thành công, chúc chặng đường tương lai luôn thênh thang rộng mở.",
    },
    "qua-20-10-8-3": {
      eyebrow: "BỘ SƯU TẬP • 20/10 & 8/3 Ý NGHĨA",
      tagline:
        "Tôn vinh và trao trọn yêu thương ngọt ngào đến một nửa thế giới tuyệt vời.",
    },
    "qua-tan-gia": {
      eyebrow: "BỘ SƯU TẬP • QUÀ MỪNG TÂN GIA",
      tagline:
        "Món quà tài lộc, bình an và khởi đầu may mắn cho tổ ấm khang trang.",
    },
    "qua-valentine": {
      eyebrow: "BỘ SƯU TẬP • VALENTINE LÃNG MẠN",
      tagline:
        "Thay ngàn lời muốn nói, gắn kết đôi tim bằng những món quà đong đầy vị ngọt.",
    },
    "qua-20-11": {
      eyebrow: "BỘ SƯU TẬP • TRI ÂN THẦY CÔ 20/11",
      tagline:
        "Thành kính tri ân công ơn người gieo mầm tri thức và nâng bước tương lai.",
    },
    "qua-20-11-thay-co": {
      eyebrow: "BỘ SƯU TẬP • TRI ÂN THẦY CÔ 20/11",
      tagline:
        "Thành kính tri ân công ơn người gieo mầm tri thức và nâng bước tương lai.",
    },
    "qua-tet": {
      eyebrow: "BỘ SƯU TẬP • QUÀ TẾT SUM VẦY",
      tagline:
        "Kính chúc vạn sự như ý, đắc tài đắc lộc và sum vầy ấm cúng bên gia đình.",
    },
    "qua-trung-thu": {
      eyebrow: "BỘ SƯU TẬP • TRUNG THU ĐOÀN VIÊN",
      tagline:
        "Trao gửi vị ngọt đoàn viên, thắp sáng đêm rằm gắn kết tình thân.",
    },
    "qua-giang-sinh": {
      eyebrow: "BỘ SƯU TẬP • GIÁNG SINH AN LÀNH",
      tagline:
        "Ấm áp giữa mùa đông, mang phép màu diệu kỳ đến người bạn thương quý.",
    },
    "qua-chia-tay-di-xa": {
      eyebrow: "BỘ SƯU TẬP • QUÀ TẠM BIỆT & ĐI XA",
      tagline:
        "Dù cách trở địa lý, kỷ niệm đẹp và tấm lòng vẫn mãi vẹn nguyên bên nhau.",
    },
    "qua-tang-khong-can-dip": {
      eyebrow: "BỘ SƯU TẬP • YÊU THƯƠNG BẤT NGỜ",
      tagline:
        "Bởi vì yêu thương không cần lý do — món quà bất ngờ mang nụ cười rạng rỡ.",
    },
    "qua-ngay-le": {
      eyebrow: "BỘ SƯU TẬP • NGÀY LỄ ĐẶC BIỆT",
      tagline:
        "Món quà trọn vẹn cho những ngày lễ sum họp và sẻ chia niềm vui.",
    },

    // Dòng sản phẩm
    "nen-thom-tinh-dau": {
      eyebrow: "BỘ SƯU TẬP • NẾN THƠM & WELLNESS",
      tagline:
        "Hương thơm dịu nhẹ xoa dịu tâm hồn sau những giờ làm việc mệt nhoài.",
    },
    "my-pham-cham-soc-da": {
      eyebrow: "BỘ SƯU TẬP • CHĂM SÓC DA & MỸ PHẨM",
      tagline:
        "Nâng niu vẻ đẹp rạng ngời cùng những sản phẩm chăm sóc dịu lành, chính hãng.",
    },
    "trang-suc-nu-cao-cap": {
      eyebrow: "BỘ SƯU TẬP • TRANG SỨC TINH TẾ",
      tagline:
        "Điểm xuyết vẻ đẹp thanh lịch, sang trọng và quý phái của phái đẹp.",
    },
    "do-cong-nghe-gaming": {
      eyebrow: "BỘ SƯU TẬP • CÔNG NGHỆ & GAMING",
      tagline:
        "Phụ kiện thông minh, hiện đại và tiện ích cho cuộc sống năng động.",
    },
    "set-qua-thu-cong-giftbox": {
      eyebrow: "BỘ SƯU TẬP • SET QUÀ THỦ CÔNG",
      tagline:
        "Hộp quà vintage thủ công tinh xảo, kèm hoa khô và thiệp viết tay ý nghĩa.",
    },
    "do-decor-ban-lam-viec": {
      eyebrow: "BỘ SƯU TẬP • DECOR & BÀN LÀM VIỆC",
      tagline:
        "Tô điểm góc làm việc thêm cảm hứng sáng tạo và năng lượng tích cực.",
    },
    "tat-ca": {
      eyebrow: "BỘ SƯU TẬP • QUÀ TẶNG TUYỂN CHỌN",
      tagline:
        "Khám phá hàng trăm món quà chính hãng được tuyển chọn khắt khe theo từng gu sở thích.",
    },
  };

  if (dict[normalizedSlug]) {
    return dict[normalizedSlug];
  }

  const parentUpper = parent ? parent.toUpperCase() : "BỘ SƯU TẬP";
  return {
    eyebrow: `BỘ SƯU TẬP • ${parentUpper}`,
    tagline:
      fallbackDesc ||
      `Những món quà ý nghĩa nhất thuộc danh mục ${title}, được tuyển chọn kỹ lưỡng và đóng gói chỉn chu.`,
  };
}

function Catalog({
  go,
  openAffiliate,
  products: _products = defaultProducts,
  onSelectProduct,
  catalogFilter,
  onSelectCategory,
}: {
  go: (p: Page) => void;
  openAffiliate: (p?: ProductItem) => void;
  products?: ProductItem[];
  onSelectProduct?: (p: ProductItem) => void;
  catalogFilter?: {
    title: string;
    slug: string;
    parentCategory: string;
    keyword?: string;
    minPrice?: number;
    maxPrice?: number;
  };
  onSelectCategory?: (
    title: string,
    slug?: string,
    parentCategory?: string,
    keyword?: string,
    minPrice?: number,
    maxPrice?: number,
  ) => void;
}) {
  const [minPrice, setMinPrice] = useState<number | undefined>(
    catalogFilter?.minPrice,
  );
  const [maxPrice, setMaxPrice] = useState<number | undefined>(
    catalogFilter?.maxPrice,
  );
  const [sort, setSort] = useState("Phù hợp nhất");
  const [page, setPage] = useState(1);
  const [sourceFilter, setSourceFilter] = useState<string | undefined>(
    undefined,
  );
  const [catalogItems, setCatalogItems] = useState<ProductItem[]>([]);
  const [loadingCatalog, setLoadingCatalog] = useState(true);
  const [totalMatchedCount, setTotalMatchedCount] = useState<number>(0);
  const [totalPages, setTotalPages] = useState<number>(0);

  const activeTitle = catalogFilter?.title || "Tất cả quà tặng tuyển chọn";
  const activeSlug = catalogFilter?.slug || "tat-ca";
  const activeKeyword = catalogFilter?.keyword || "";
  const activeParent = catalogFilter?.parentCategory || "Danh mục quà tặng";

  const heroMeta = getCategoryHeroMeta(
    activeSlug,
    activeTitle,
    activeParent,
    activeKeyword,
  );

  // Đồng bộ khoảng giá khi đổi danh mục có chỉ định giá từ bên ngoài (như Quiz)
  useEffect(() => {
    setPage(1);
    if (
      catalogFilter?.minPrice !== undefined ||
      catalogFilter?.maxPrice !== undefined
    ) {
      setMinPrice(catalogFilter?.minPrice);
      setMaxPrice(catalogFilter?.maxPrice);
    }
  }, [
    activeSlug,
    activeKeyword,
    catalogFilter?.minPrice,
    catalogFilter?.maxPrice,
  ]);

  useEffect(() => {
    let isCancelled = false;

    const sortParam =
      sort === "Giá thấp đến cao"
        ? "price_asc"
        : sort === "Giá cao đến thấp"
          ? "price_desc"
          : sort === "Đánh giá cao"
            ? "rating"
            : sort === "Bán chạy nhất"
              ? "popular"
              : "relevance";

    // Real-time 100%: Mỗi lần click đều gọi thẳng Database PostgreSQL
    // Giữ nguyên lưới sản phẩm cũ để tránh giật chiều cao trang (Zero Layout Shift)
    setLoadingCatalog(true);

    catalogService
      .getCatalogProducts({
        category: activeSlug,
        keyword: activeKeyword || undefined,
        page,
        pageSize: 24,
        minPrice,
        maxPrice,
        sort: sortParam,
        source: sourceFilter,
      })
      .then((res) => {
        if (isCancelled) return;
        const mapped: ProductItem[] = (res.items || []).map((item) => {
          const origPrice =
            item.originalPrice && item.originalPrice > item.price
              ? item.originalPrice
              : Math.round(item.price * 1.25);
          const disc = Math.max(
            5,
            Math.round((1 - item.price / origPrice) * 100),
          );
          const srcUpper = (item.source || "").toUpperCase();
          const urlLower = (item.originalUrl || "").toLowerCase();
          const platform =
            srcUpper.includes("TIKTOK") || urlLower.includes("tiktok")
              ? "TIKTOK"
              : srcUpper.includes("SHOPEE") || urlLower.includes("shopee")
                ? "SHOPEE"
                : "DIRECT";

          return {
            id: item.id,
            name: item.name,
            price: item.price,
            old: origPrice,
            rating: String(item.rating > 0 ? item.rating.toFixed(1) : "4.9"),
            sold: String(item.totalReviews > 0 ? item.totalReviews : 128),
            image: getHighResImageUrl(item.imageUrl),
            platform,
            discount: `-${disc}%`,
            originalUrl: item.originalUrl,
            affiliateUrl: item.affiliateUrl,
            tags: (item.tags || []).map((t: any) =>
              typeof t === "string" ? t : t.name,
            ),
          };
        });

        setCatalogItems(mapped);
        setTotalMatchedCount(res.total || 0);
        setTotalPages(res.totalPages || 0);
      })
      .catch((err) => {
        console.warn("Catalog API error:", err);
        if (!isCancelled) {
          setCatalogItems([]);
          setTotalMatchedCount(0);
          setTotalPages(0);
        }
      })
      .finally(() => {
        if (!isCancelled) setLoadingCatalog(false);
      });

    return () => {
      isCancelled = true;
    };
  }, [activeSlug, activeKeyword, page, maxPrice, minPrice, sort, sourceFilter]);

  const scrollToCatalogTop = () => {
    // Cuộn mượt mà lên đầu danh sách sản phẩm, trừ đi chiều cao của thanh sticky header (120px)
    const target =
      (document.querySelector(".catalog-layout") as HTMLElement | null) ||
      (document.querySelector(".catalog-slim-hero") as HTMLElement | null) ||
      (document.querySelector(".catalog-page") as HTMLElement | null);

    if (target) {
      const headerOffset = 130; // 120px sticky header + 10px đệm
      const elementPosition = target.getBoundingClientRect().top;
      const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

      window.scrollTo({
        top: Math.max(0, offsetPosition),
        behavior: "smooth",
      });
    } else {
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  };

  const handlePageChange = (nextPage: number) => {
    if (nextPage < 1 || nextPage > totalPages || nextPage === page) return;
    setPage(nextPage);
    scrollToCatalogTop();
  };

  const [collapsedSections, setCollapsedSections] = useState<{
    category: boolean;
    price: boolean;
    platform: boolean;
  }>({
    category: false,
    price: false,
    platform: false,
  });

  const toggleSection = (section: "category" | "price" | "platform") => {
    setCollapsedSections((prev) => ({ ...prev, [section]: !prev[section] }));
  };

  const quickCategories = [
    {
      label: "Tất cả sản phẩm",
      shortLabel: "Tất cả sản phẩm",
      slug: "tat-ca",
      parent: "Danh mục quà tặng",
      icon: "✨",
    },
    {
      label: "Quà Sinh Nhật",
      shortLabel: "Quà Sinh Nhật",
      slug: "qua-sinh-nhat",
      parent: "Quà Theo Dịp",
      icon: "🎂",
    },
    {
      label: "Quà Cho Nàng (Bạn Gái)",
      shortLabel: "Quà Cho Nàng",
      slug: "qua-cho-nang",
      parent: "Quà Theo Người Nhận",
      icon: "💖",
    },
    {
      label: "Quà Cho Chàng (Bạn Trai)",
      shortLabel: "Quà Cho Chàng",
      slug: "qua-cho-chang",
      parent: "Quà Theo Người Nhận",
      icon: "👔",
    },
    {
      label: "Quà Cho Mẹ",
      shortLabel: "Quà Cho Mẹ",
      slug: "qua-cho-me",
      parent: "Quà Theo Người Nhận",
      icon: "🌸",
    },
    {
      label: "Quà Cho Bố",
      shortLabel: "Quà Cho Bố",
      slug: "qua-cho-bo",
      parent: "Quà Theo Người Nhận",
      icon: "👑",
    },
    {
      label: "Nến Thơm & Tinh Dầu",
      shortLabel: "Nến Thơm & Tinh Dầu",
      slug: "nen-thom-tinh-dau",
      parent: "Dòng Sản Phẩm",
      icon: "🕯️",
    },
    {
      label: "Mỹ Phẩm & Chăm Sóc Da",
      shortLabel: "Mỹ Phẩm & Chăm Sóc Da",
      slug: "my-pham-cham-soc-da",
      parent: "Dòng Sản Phẩm",
      icon: "💄",
    },
    {
      label: "Trang Sức Nữ Cao Cấp",
      shortLabel: "Trang Sức Nữ Cao Cấp",
      slug: "trang-suc-nu-cao-cap",
      parent: "Dòng Sản Phẩm",
      icon: "💍",
    },
    {
      label: "Đồ Công Nghệ & Gaming",
      shortLabel: "Đồ Công Nghệ & Gaming",
      slug: "do-cong-nghe-gaming",
      parent: "Dòng Sản Phẩm",
      icon: "🎮",
    },
    {
      label: "Set Quà Thủ Công & Giftbox",
      shortLabel: "Set Quà Giftbox",
      slug: "set-qua-thu-cong-giftbox",
      parent: "Dòng Sản Phẩm",
      icon: "🎀",
    },
    {
      label: "Đồ Decor & Bàn Làm Việc",
      shortLabel: "Đồ Decor Bàn",
      slug: "do-decor-ban-lam-viec",
      parent: "Dòng Sản Phẩm",
      icon: "🪴",
    },
  ];

  const pricePresets = [
    { id: "under-200k", label: "Dưới 200k", max: 200000 },
    { id: "under-500k", label: "Dưới 500k", max: 500000 },
    { id: "under-1m", label: "Dưới 1 triệu", max: 1000000 },
    { id: "all", label: "Tất cả mức giá", max: undefined },
  ];

  const isPricePresetActive = (p: (typeof pricePresets)[0]) => {
    if (p.id === "all") return maxPrice === undefined;
    return maxPrice === p.max;
  };

  const handleSelectPricePreset = (p: (typeof pricePresets)[0]) => {
    setMinPrice(undefined);
    setMaxPrice(p.max);
    setPage(1);
    scrollToCatalogTop();
  };

  const getPriceLabel = () => {
    if (maxPrice === undefined) return "Tất cả mức giá";
    if (maxPrice === 200000) return "Dưới 200k";
    if (maxPrice === 500000) return "Dưới 500k";
    if (maxPrice === 1000000) return "Dưới 1 triệu";
    return `Dưới ${(maxPrice / 1000).toLocaleString("vi-VN")}k`;
  };

  const platformPresets = [
    { id: "all", label: "Tất cả sàn", value: undefined, icon: "✨" },
    { id: "shopee", label: "Shopee Mall", value: "Shopee", icon: "🛍️" },
    { id: "tiktok", label: "TikTok Shop", value: "TikTokShop", icon: "🎵" },
  ];

  const handleSelectPlatform = (val?: string) => {
    setPage(1);
    setSourceFilter(val);
    scrollToCatalogTop();
  };

  const hasActiveFilters = Boolean(
    minPrice !== undefined ||
    maxPrice !== undefined ||
    sourceFilter !== undefined ||
    (activeSlug && activeSlug !== "tat-ca") ||
    activeKeyword,
  );

  const handleResetFilters = () => {
    setMinPrice(undefined);
    setMaxPrice(undefined);
    setSourceFilter(undefined);
    setSort("Phù hợp nhất");
    setPage(1);
    scrollToCatalogTop();
    onSelectCategory?.(
      "Tất cả quà tặng tuyển chọn",
      "tat-ca",
      "Danh mục quà tặng",
    );
  };

  // Nếu đang xem một danh mục không thuộc 12 danh mục nhanh (ví dụ click từ menu Quà Valentine)
  const isCustomCategory =
    !quickCategories.some((c) => c.slug === activeSlug) &&
    activeSlug !== "tat-ca";

  const allDisplayCategories = isCustomCategory
    ? [
        ...quickCategories,
        {
          label: activeTitle,
          shortLabel: activeTitle,
          slug: activeSlug,
          parent: activeParent,
          icon: "🎁",
        },
      ]
    : quickCategories;

  const handleCategoryClick = (cat: {
    label: string;
    slug: string;
    parent?: string;
  }) => {
    if (cat.slug === activeSlug) return;
    setPage(1);
    scrollToCatalogTop();
    onSelectCategory?.(
      cat.label,
      cat.slug,
      cat.parent || "Danh mục quà tặng",
      activeKeyword,
      minPrice,
      maxPrice,
    );
  };

  // Tính toán danh sách số trang hiển thị
  const getPageNumbers = () => {
    const pages: number[] = [];
    for (let i = 1; i <= totalPages; i++) {
      pages.push(i);
    }
    return pages;
  };

  return (
    <main className="shell catalog-page">
      {/* 1. THANH ĐIỀU HƯỚNG BREADCRUMB GỌN GÀNG */}
      <div className="catalog-breadcrumb-bar">
        <div className="breadcrumb">
          <span onClick={() => go("home")} style={{ cursor: "pointer" }}>
            Trang chủ
          </span>{" "}
          <Icon name="chevron" size={12} /> {activeParent}{" "}
          <Icon name="chevron" size={12} /> <b>{activeTitle}</b>
        </div>
      </div>

      {/* 2. KHUNG BANNER TINH GỌN (SLIM LUXURY HERO - CAO ~76px, TRÙM TOÀN CHIỀU RỘNG TẠO ĐỐI XỨNG CÂN BẰNG HOÀN HẢO) */}
      <section className="catalog-slim-hero" aria-label="Bộ sưu tập quà tặng">
        <div className="slim-hero-left">
          <span className="slim-hero-eyebrow">{heroMeta.eyebrow}</span>
          <div className="slim-hero-heading-row">
            <h1 className="slim-hero-title">{activeTitle}</h1>
            <span className="slim-hero-tagline">— {heroMeta.tagline}</span>
          </div>
        </div>

        <div className="slim-hero-right">
          <span className="slim-hero-badge">
            ✨{" "}
            {totalMatchedCount > 0
              ? `${totalMatchedCount} món quà tuyển chọn`
              : "Món quà tuyển chọn"}
          </span>
          <button
            type="button"
            className="slim-hero-ai-btn"
            onClick={() => {
              const chatBtn = document.querySelector(
                ".chatbot-btn",
              ) as HTMLButtonElement | null;
              if (chatBtn) chatBtn.click();
            }}
            title="Nhờ AI Concierge tư vấn riêng"
          >
            <Icon name="sparkle" size={13} />
            Cần AI tư vấn riêng?
          </button>
        </div>
      </section>

      {/* 2. BỐ CỤC 2 CỘT CHUẨN MỰC NHƯ LÚC ĐẦU: BỘ LỌC BÊN TRÁI + SẢN PHẨM BÊN PHẢI */}
      <div className="catalog-layout">
        {/* SIDEBAR BỘ LỌC CỔ ĐIỂN TINH TẾ */}
        <aside className="catalog-sidebar">
          <div className="filter-card">
            <div className="filter-card-header">
              <h2 className="filter-card-title">Bộ lọc</h2>
              {hasActiveFilters && (
                <button
                  type="button"
                  className="filter-reset-btn"
                  onClick={handleResetFilters}
                >
                  Xóa tất cả
                </button>
              )}
            </div>

            {/* Nhóm 1: Danh mục nổi bật */}
            <div className="filter-section">
              <div
                className="filter-section-header"
                onClick={() => toggleSection("category")}
                role="button"
                tabIndex={0}
              >
                <span className="filter-section-title">Danh mục nổi bật</span>
                <span className="filter-section-toggle">
                  {collapsedSections.category ? "+" : "−"}
                </span>
              </div>
              {!collapsedSections.category && (
                <div className="filter-radio-list">
                  {allDisplayCategories.map((cat) => {
                    const isActive = activeSlug === cat.slug;
                    return (
                      <button
                        key={cat.slug}
                        type="button"
                        className={`filter-radio-item ${isActive ? "active" : ""}`}
                        onClick={() => handleCategoryClick(cat)}
                      >
                        <span
                          className="filter-radio-circle"
                          aria-hidden="true"
                        >
                          <span className="filter-radio-dot" />
                        </span>
                        <span className="filter-radio-label">{cat.label}</span>
                      </button>
                    );
                  })}
                </div>
              )}
            </div>

            {/* Nhóm 2: Khoảng giá */}
            <div className="filter-section">
              <div
                className="filter-section-header"
                onClick={() => toggleSection("price")}
                role="button"
                tabIndex={0}
              >
                <span className="filter-section-title">Khoảng giá</span>
                <span className="filter-section-toggle">
                  {collapsedSections.price ? "+" : "−"}
                </span>
              </div>
              {!collapsedSections.price && (
                <div className="filter-price-block">
                  <div className="price-slider-labels">
                    <span className="price-label-min">0đ</span>
                    <span className="price-label-max">{getPriceLabel()}</span>
                  </div>
                  <input
                    type="range"
                    min={100000}
                    max={3000000}
                    step={50000}
                    value={maxPrice ?? 3000000}
                    onChange={(e) => {
                      const val = Number(e.target.value);
                      setPage(1);
                      if (val >= 3000000) {
                        setMaxPrice(undefined);
                      } else {
                        setMaxPrice(val);
                      }
                    }}
                    className="price-range-slider"
                    aria-label="Thanh kéo chọn khoảng giá tối đa"
                  />
                  <div className="filter-radio-list">
                    {pricePresets.map((p) => {
                      const isActive = isPricePresetActive(p);
                      return (
                        <button
                          key={p.id}
                          type="button"
                          className={`filter-radio-item ${isActive ? "active" : ""}`}
                          onClick={() => handleSelectPricePreset(p)}
                        >
                          <span
                            className="filter-radio-circle"
                            aria-hidden="true"
                          >
                            <span className="filter-radio-dot" />
                          </span>
                          <span className="filter-radio-label">{p.label}</span>
                        </button>
                      );
                    })}
                  </div>
                </div>
              )}
            </div>

            {/* Nhóm 3: Nguồn gian hàng */}
            <div className="filter-section">
              <div
                className="filter-section-header"
                onClick={() => toggleSection("platform")}
                role="button"
                tabIndex={0}
              >
                <span className="filter-section-title">Nguồn gian hàng</span>
                <span className="filter-section-toggle">
                  {collapsedSections.platform ? "+" : "−"}
                </span>
              </div>
              {!collapsedSections.platform && (
                <div className="filter-radio-list">
                  {platformPresets.map((p) => {
                    const isActive = sourceFilter === p.value;
                    return (
                      <button
                        key={p.id}
                        type="button"
                        className={`filter-radio-item ${isActive ? "active" : ""}`}
                        onClick={() => handleSelectPlatform(p.value)}
                      >
                        <span
                          className="filter-radio-circle"
                          aria-hidden="true"
                        >
                          <span className="filter-radio-dot" />
                        </span>
                        <span className="filter-radio-label">
                          {p.icon} {p.label}
                        </span>
                      </button>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        </aside>

        {/* NỘI DUNG CHÍNH BÊN PHẢI: DÒNG THỐNG KÊ + Ô SẮP XẾP + LƯỚI SẢN PHẨM */}
        <div className="catalog-main-content">
          <div className="catalog-meta-bar">
            <span className="catalog-count-info">
              {totalMatchedCount > 0
                ? `Hiển thị ${(page - 1) * 24 + 1}–${Math.min(page * 24, totalMatchedCount)} trong tổng số ${totalMatchedCount} món quà phù hợp (Trang ${page}/${totalPages})`
                : loadingCatalog
                  ? ""
                  : `Không tìm thấy món quà nào trong mục "${activeTitle}"`}
            </span>

            {/* Ô chọn sắp xếp nằm ở góc bên phải */}
            <div className="catalog-sort-wrap">
              <label>
                <span className="sort-label-text">Sắp xếp:</span>
                <select
                  className="catalog-sort-select"
                  value={sort}
                  onChange={(e) => {
                    setPage(1);
                    setSort(e.target.value);
                  }}
                >
                  {[
                    "Phù hợp nhất",
                    "Bán chạy nhất",
                    "Giá thấp đến cao",
                    "Giá cao đến thấp",
                    "Đánh giá cao",
                  ].map((x) => (
                    <option key={x} value={x}>
                      {x}
                    </option>
                  ))}
                </select>
              </label>
            </div>
          </div>

          {/* LƯỚI SẢN PHẨM TOÀN DIỆN BẮT ĐẦU NGAY LẬP TỨC */}
          <div className="catalog-grid-wrapper">
            {loadingCatalog && (
              <div className="catalog-shimmer-bar">
                <div className="catalog-shimmer-progress" />
              </div>
            )}

            {loadingCatalog && catalogItems.length === 0 ? (
              <div className="catalog-loading-card">
                <div className="catalog-spinner" />
              </div>
            ) : catalogItems.length === 0 ? (
              <div className="catalog-empty-card">
                <div className="empty-sparkle-icon">🎁</div>
                <h3>Chưa có sản phẩm phù hợp trong mục "{activeTitle}"</h3>
                <p>
                  Hệ thống chỉ hiển thị đúng các sản phẩm thực sự thuộc danh mục
                  bạn chọn, không tự động chèn các sản phẩm không liên quan.
                </p>
                <div className="empty-btn-group">
                  {hasActiveFilters && (
                    <Button variant="secondary" onClick={handleResetFilters}>
                      ✕ Xóa bộ lọc
                    </Button>
                  )}
                  <Button
                    variant="rose"
                    onClick={() => {
                      onSelectCategory?.(
                        "Tất cả quà tặng tuyển chọn",
                        "tat-ca",
                        "Danh mục quà tặng",
                      );
                    }}
                  >
                    ✨ Xem tất cả quà tặng tuyển chọn
                  </Button>
                </div>
              </div>
            ) : (
              <div
                className={`catalog-grid ${loadingCatalog ? "is-updating" : ""}`}
              >
                {catalogItems.map((p, i) => (
                  <ProductCard
                    key={p.id || p.name + i}
                    product={p}
                    sponsored={page === 1 && (i === 2 || i === 6)}
                    onOpen={() => onSelectProduct?.(p)}
                    onAffiliate={() => openAffiliate(p)}
                  />
                ))}
              </div>
            )}
          </div>

          {/* PHÂN TRANG */}
          {totalPages > 1 && (
            <div className={`pagination ${loadingCatalog ? "is-loading" : ""}`}>
              <button
                disabled={page <= 1 || loadingCatalog}
                onClick={() => handlePageChange(page - 1)}
                style={{
                  opacity: page <= 1 ? 0.4 : 1,
                  cursor: page <= 1 ? "not-allowed" : "pointer",
                }}
                aria-label="Trang trước"
              >
                ‹
              </button>
              {getPageNumbers().map((num) => (
                <button
                  key={num}
                  className={page === num ? "active" : ""}
                  onClick={() => handlePageChange(num)}
                  disabled={loadingCatalog}
                >
                  {num}
                </button>
              ))}
              <button
                disabled={page >= totalPages || loadingCatalog}
                onClick={() => handlePageChange(page + 1)}
                style={{
                  opacity: page >= totalPages ? 0.4 : 1,
                  cursor: page >= totalPages ? "not-allowed" : "pointer",
                }}
                aria-label="Trang tiếp theo"
              >
                ›
              </button>
            </div>
          )}
        </div>
      </div>
    </main>
  );
}

function Wishlist({
  go,
  openAffiliate,
  onSelectProduct,
}: {
  go: (p: Page) => void;
  openAffiliate: (p?: ProductItem) => void;
  products?: ProductItem[];
  onSelectProduct?: (p: ProductItem) => void;
}) {
  const { wishlistItems, removeFromWishlist } = useWishlist();

  const saved: ProductItem[] = wishlistItems.map((item) => ({
    id: item.productId || item.id,
    name: item.productName,
    price: item.price,
    old: item.originalPrice || Math.round(item.price * 1.25),
    rating: "4.9",
    sold: "Đã lưu",
    image: item.imageUrl || images.candle,
    platform: (item.originalUrl || "").toLowerCase().includes("tiktok")
      ? "TIKTOK"
      : (item.originalUrl || "").toLowerCase().includes("shopee")
        ? "SHOPEE"
        : "DIRECT",
    discount:
      item.originalPrice && item.originalPrice > item.price
        ? `-${Math.round((1 - item.price / item.originalPrice) * 100)}%`
        : "-15%",
    originalUrl: item.originalUrl,
    affiliateUrl: item.affiliateUrl,
  }));

  return (
    <main className="shell header-page">
      <div className="breadcrumb">
        Trang chủ <Icon name="chevron" size={12} /> <b>Yêu thích</b>
      </div>
      <div className="header-page-title">
        <div>
          <Badge tone="rose">BỘ SƯU TẬP CỦA BẠN</Badge>
          <h1>Quà đã yêu thích</h1>
          <p>{saved.length} món quà đang chờ bạn lựa chọn.</p>
        </div>
        <button onClick={() => go("catalog")}>+ Khám phá thêm quà tặng</button>
      </div>
      {saved.length ? (
        <>
          <div className="wishlist-toolbar">
            <span>
              <Icon name="sparkle" /> Giá và ưu đãi được cập nhật tự động mỗi
              ngày
            </span>
            <select>
              <option>Mới lưu gần đây</option>
              <option>Giá thấp đến cao</option>
              <option>Đánh giá cao nhất</option>
            </select>
          </div>
          <div className="wishlist-grid">
            {saved.map((product) => (
              <div className="wishlist-item" key={product.id || product.name}>
                <ProductCard
                  product={product}
                  onOpen={() => {
                    onSelectProduct?.(product);
                  }}
                  onAffiliate={() => openAffiliate(product)}
                />
                <button
                  className="wishlist-remove"
                  onClick={() => removeFromWishlist(product.id || product.name)}
                >
                  <Icon name="trash" size={14} /> Xóa khỏi yêu thích
                </button>
              </div>
            ))}
          </div>
          <div className="wishlist-note">
            <div>
              <Icon name="bell" />
              <span>
                <b>Đừng bỏ lỡ khi giá giảm</b>
                <small>
                  Bật theo dõi giá để nhận thông báo cho những món quà này.
                </small>
              </span>
            </div>
            <Button variant="secondary">Bật thông báo cho tất cả</Button>
          </div>
        </>
      ) : (
        <div
          className="empty-state"
          style={{ padding: "60px 20px", textAlign: "center" }}
        >
          <div className="empty-gift" style={{ margin: "0 auto 16px" }}>
            <Icon name="heart" size={38} />
          </div>
          <h2
            style={{ fontSize: "20px", fontWeight: 700, marginBottom: "8px" }}
          >
            Danh sách yêu thích đang trống
          </h2>
          <p
            style={{
              color: "var(--muted)",
              maxWidth: "420px",
              margin: "0 auto 24px",
              lineHeight: 1.6,
            }}
          >
            Lưu những món quà bạn thích bằng cách bấm vào biểu tượng trái tim ♡
            trên các sản phẩm để dễ dàng quay lại sau!
          </p>
          <Button variant="rose" onClick={() => go("catalog")}>
            Khám phá quà tặng ngay
          </Button>
        </div>
      )}
    </main>
  );
}

function MetricCard({
  icon,
  label,
  value,
  detail,
  tone = "rose",
  badge,
}: {
  icon: IconName;
  label: string;
  value: string;
  detail: string;
  tone?: string;
  badge?: string;
}) {
  return (
    <article className="metric-card">
      <div className={`metric-icon metric-${tone}`}>
        <Icon name={icon} />
      </div>
      <div className="metric-label">{label}</div>
      <div className="metric-value">{value}</div>
      <div className="metric-detail">
        {badge ? <Badge tone="success">{badge}</Badge> : detail}
      </div>
    </article>
  );
}

function AccessDenied({
  title = "Truy cập bị từ chối (403 Forbidden)",
  desc = "Bạn không có quyền truy cập vào khu vực này.",
  actionLabel = "Về trang chủ",
  onAction,
  go,
}: {
  title?: string;
  desc?: string;
  actionLabel?: string;
  onAction?: () => void;
  go: (p: Page) => void;
}) {
  return (
    <div
      className="app-page"
      style={{
        padding: "80px 20px",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        minHeight: "65vh",
      }}
    >
      <div
        style={{
          maxWidth: 480,
          width: "100%",
          textAlign: "center",
          background: "#fff",
          border: "1px solid var(--line)",
          borderRadius: 24,
          padding: "44px 32px",
          boxShadow: "0 20px 40px rgba(0,0,0,0.06)",
        }}
      >
        <div
          style={{
            width: 70,
            height: 70,
            borderRadius: "50%",
            background: "#fee2e2",
            color: "#dc2626",
            display: "inline-flex",
            alignItems: "center",
            justifyContent: "center",
            fontSize: 32,
            marginBottom: 20,
          }}
        >
          🔒
        </div>
        <h2
          style={{
            fontSize: 22,
            fontWeight: 800,
            color: "var(--ink)",
            marginBottom: 12,
            letterSpacing: "-0.02em",
          }}
        >
          {title}
        </h2>
        <p
          style={{
            fontSize: 14,
            color: "var(--muted)",
            lineHeight: 1.6,
            marginBottom: 28,
          }}
        >
          {desc}
        </p>
        <div style={{ display: "flex", gap: 12, justifyContent: "center" }}>
          <Button variant="secondary" onClick={() => go("home")}>
            Về trang chủ
          </Button>
          <Button variant="primary" onClick={onAction || (() => go("home"))}>
            {actionLabel}
          </Button>
        </div>
      </div>
    </div>
  );
}

function AuthPage({ go }: { go: (p: Page) => void }) {
  const { login, register: registerUser, registerMerchant } = useAuth();
  const [isRegister, setRegister] = useState(false);
  const [registerType, setRegisterType] = useState<"member" | "merchant">(
    "member",
  );
  const [showPassword, setShowPassword] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fullName, setFullName] = useState("");
  const [shopName, setShopName] = useState("");
  const [contactPhone, setContactPhone] = useState("");
  const [rememberMe, setRememberMe] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    setLoading(true);
    try {
      if (isRegister) {
        if (!fullName.trim()) throw new Error("Vui lòng nhập họ và tên.");
        if (!email.trim()) throw new Error("Vui lòng nhập email.");
        if (!password || password.length < 6)
          throw new Error("Mật khẩu phải từ 6 ký tự trở lên.");

        if (registerType === "merchant") {
          if (!shopName.trim()) throw new Error("Vui lòng nhập tên gian hàng.");
          if (!contactPhone.trim())
            throw new Error("Vui lòng nhập số điện thoại liên hệ.");
          await registerMerchant(
            {
              email: email.trim(),
              password,
              fullName: fullName.trim(),
              shopName: shopName.trim(),
              contactPhone: contactPhone.trim(),
            },
            rememberMe,
          );
          go("merchant");
        } else {
          await registerUser(
            { email: email.trim(), password, fullName: fullName.trim() },
            rememberMe,
          );
          go("home");
        }
      } else {
        if (!email.trim() || !password)
          throw new Error("Vui lòng nhập email và mật khẩu.");
        const res = await login({ email: email.trim(), password }, rememberMe);
        if (res.role === "Admin") go("admin");
        else if (res.role === "Merchant") go("merchant");
        else go("home");
      }
    } catch (err: any) {
      const msg =
        err.response?.data?.message ||
        err.response?.data?.title ||
        err.message ||
        "Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="auth-shell">
      <section
        className="auth-visual"
        style={{ backgroundImage: `url("${images.hero}")` }}
      >
        <div className="auth-scrim" />
        <div className="auth-story">
          <Badge tone="glass">GIFT CONCIERGE</Badge>
          <h1>
            Mỗi món quà là một cách nói <em>“Tôi quan tâm”.</em>
          </h1>
          <p>
            Đăng nhập để lưu quà, theo dõi giá và không bỏ lỡ những ngày đặc
            biệt.
          </p>
          <div className="quote-card">
            <div className="stars">★★★★★</div>
            <blockquote>
              “GiftFinder giúp mình chọn đúng món quà chỉ trong vài phút.”
            </blockquote>
            <div className="quote-person">
              <span className="mini-avatar">MA</span>
              <span>
                <strong>Mai Anh</strong>
                <small>TP. Hồ Chí Minh</small>
              </span>
            </div>
          </div>
        </div>
        <span className="photo-credit">GiftFinder Studio</span>
      </section>
      <section className="auth-form-side">
        <Button
          variant="ghost"
          icon="arrow-left"
          className="back-button"
          onClick={() => go("home")}
        >
          Về trang chủ
        </Button>
        <div className="auth-panel">
          <Logo onClick={() => go("home")} />
          <div className="auth-heading">
            <Badge tone="rose-soft">
              {isRegister
                ? registerType === "merchant"
                  ? "ĐỐI TÁC GIAN HÀNG"
                  : "THÀNH VIÊN MỚI"
                : "RẤT VUI ĐƯỢC GẶP LẠI"}
            </Badge>
            <h2>
              {isRegister
                ? registerType === "merchant"
                  ? "Mở gian hàng mới"
                  : "Tạo tài khoản mới"
                : "Chào mừng trở lại"}
            </h2>
            <p>
              {isRegister
                ? registerType === "merchant"
                  ? "Bắt đầu kinh doanh và bán quà tặng cùng GiftFinder."
                  : "Tạo tài khoản để bắt đầu hành trình tìm quà cá nhân hóa."
                : "Đăng nhập để tiếp tục với GiftFinder."}
            </p>
          </div>

          {error && (
            <div
              style={{
                background: "#fef2f2",
                border: "1px solid #fecaca",
                color: "#b91c1c",
                padding: "10px 14px",
                borderRadius: "10px",
                fontSize: "12px",
                marginTop: "14px",
                lineHeight: 1.5,
                textAlign: "left",
              }}
            >
              ⚠️ {error}
            </div>
          )}

          <form onSubmit={submit}>
            {isRegister && (
              <div
                style={{
                  display: "flex",
                  gap: "6px",
                  margin: "14px 0",
                  background: "#f1f5f9",
                  padding: "4px",
                  borderRadius: "10px",
                }}
              >
                <button
                  type="button"
                  onClick={() => setRegisterType("member")}
                  style={{
                    flex: 1,
                    padding: "8px 10px",
                    borderRadius: "8px",
                    border: 0,
                    fontSize: "12px",
                    fontWeight: 600,
                    cursor: "pointer",
                    background:
                      registerType === "member" ? "#fff" : "transparent",
                    color:
                      registerType === "member" ? "var(--ink)" : "var(--muted)",
                    boxShadow:
                      registerType === "member"
                        ? "0 1px 3px rgba(0,0,0,0.1)"
                        : "none",
                    transition: "all 0.15s ease",
                  }}
                >
                  👤 Người mua quà
                </button>
                <button
                  type="button"
                  onClick={() => setRegisterType("merchant")}
                  style={{
                    flex: 1,
                    padding: "8px 10px",
                    borderRadius: "8px",
                    border: 0,
                    fontSize: "12px",
                    fontWeight: 600,
                    cursor: "pointer",
                    background:
                      registerType === "merchant" ? "#fff" : "transparent",
                    color:
                      registerType === "merchant" ? "#4f46e5" : "var(--muted)",
                    boxShadow:
                      registerType === "merchant"
                        ? "0 1px 3px rgba(0,0,0,0.1)"
                        : "none",
                    transition: "all 0.15s ease",
                  }}
                >
                  🏪 Chủ gian hàng
                </button>
              </div>
            )}

            {!isRegister && (
              <>
                <Button variant="secondary" className="google-button">
                  <span className="google-g">G</span>Tiếp tục với Google
                </Button>
                <div className="divider">
                  <span>hoặc dùng email</span>
                </div>
              </>
            )}

            <div className="form-stack">
              {isRegister && (
                <label className="field">
                  <span>Họ và tên người đại diện</span>
                  <div className="input-wrap">
                    <input
                      value={fullName}
                      onChange={(e) => setFullName(e.target.value)}
                      placeholder="Nguyễn Minh Anh"
                      required
                    />
                  </div>
                </label>
              )}
              {isRegister && registerType === "merchant" && (
                <>
                  <label className="field">
                    <span>Tên gian hàng / Thương hiệu *</span>
                    <div className="input-wrap">
                      <input
                        value={shopName}
                        onChange={(e) => setShopName(e.target.value)}
                        placeholder="VD: Tiệm Nến Thơm Mộc"
                        required
                      />
                    </div>
                  </label>
                  <label className="field">
                    <span>Số điện thoại liên hệ *</span>
                    <div className="input-wrap">
                      <input
                        value={contactPhone}
                        onChange={(e) => setContactPhone(e.target.value)}
                        placeholder="VD: 0901234567"
                        required
                      />
                    </div>
                  </label>
                </>
              )}
              <label className="field">
                <span>Email đăng nhập</span>
                <div className="input-wrap">
                  <input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="ban@email.com"
                    required
                  />
                </div>
              </label>
              <label className="field">
                <span>Mật khẩu</span>
                <div className="input-wrap">
                  <input
                    type={showPassword ? "text" : "password"}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="••••••••"
                    required
                  />
                  <button
                    type="button"
                    aria-label="Hiện hoặc ẩn mật khẩu"
                    className="input-icon"
                    onClick={() => setShowPassword(!showPassword)}
                  >
                    <Icon name={showPassword ? "eye-off" : "eye"} />
                  </button>
                </div>
              </label>
            </div>
            <div className="form-options">
              <label className="checkbox">
                <input
                  type="checkbox"
                  checked={rememberMe}
                  onChange={(e) => setRememberMe(e.target.checked)}
                />
                <span>Ghi nhớ đăng nhập</span>
              </label>
              <button type="button" className="text-link">
                Quên mật khẩu?
              </button>
            </div>
            <Button type="submit" className="submit-button" disabled={loading}>
              {loading
                ? "Đang xử lý..."
                : isRegister
                  ? registerType === "merchant"
                    ? "Đăng ký mở gian hàng"
                    : "Tạo tài khoản"
                  : "Đăng nhập"}
              <Icon name="arrow-right" />
            </Button>
          </form>

          <p className="auth-toggle">
            {isRegister ? "Đã có tài khoản?" : "Chưa có tài khoản?"}{" "}
            <button
              type="button"
              onClick={() => {
                setRegister(!isRegister);
                setError(null);
              }}
            >
              {isRegister ? "Đăng nhập" : "Đăng ký miễn phí"}
            </button>
          </p>
          <p className="legal">
            Bằng cách tiếp tục, bạn đồng ý với Điều khoản sử dụng và Chính sách
            riêng tư của GiftFinder.
          </p>
        </div>
      </section>
    </main>
  );
}

function RejectModal({
  name,
  onClose,
  onReject,
}: {
  name: string;
  onClose: () => void;
  onReject: (reason: string) => void;
}) {
  const [reason, setReason] = useState("");
  return (
    <div className="modal-layer" onMouseDown={onClose}>
      <div
        className="modal-card reject-modal"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <div className="modal-heading">
          <div className="modal-title-icon danger-icon">
            <Icon name="trash" />
          </div>
          <div>
            <h2>Từ chối sản phẩm</h2>
            <p>{name}</p>
          </div>
          <button aria-label="Đóng" className="close-button" onClick={onClose}>
            <Icon name="close" />
          </button>
        </div>
        <div className="modal-body">
          <label className="field">
            <span>Lý do từ chối</span>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Cho đối tác biết nội dung nào cần chỉnh sửa..."
              autoFocus
            />
          </label>
        </div>
        <div className="modal-footer">
          <Button variant="ghost" onClick={onClose}>
            Hủy
          </Button>
          <Button
            variant="danger"
            onClick={() =>
              onReject(
                reason ||
                  "Sản phẩm chưa đạt tiêu chuẩn chất lượng của GiftFinder.",
              )
            }
          >
            Xác nhận từ chối
          </Button>
        </div>
      </div>
    </div>
  );
}

function AdminConsole({ go }: { go: (p: Page) => void }) {
  const { user, isAdmin, isAuthenticated } = useAuth();
  const [pending, setPending] = useState<
    Array<{
      id: string;
      name: string;
      shop: string;
      price: string;
      category: string;
      image: string;
    }>
  >([]);
  const [rejecting, setRejecting] = useState<{
    id: string;
    name: string;
  } | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [totalProducts, setTotalProducts] = useState<number>(243);

  const loadPending = async () => {
    if (!isAdmin) return;
    try {
      const data = await adminService.getPendingProducts(1, 50);
      if (data?.items && Array.isArray(data.items)) {
        setPending(
          data.items.map((p) => ({
            id: p.id,
            name: p.name,
            shop: p.source || "Gian hàng đối tác",
            price: p.price.toLocaleString("vi-VN") + "đ",
            category: p.tags?.[0]?.name || "Quà tặng",
            image: p.imageUrl || images.hero,
          })),
        );
      } else {
        setPending([]);
      }
    } catch (e) {
      console.error("Error loading pending products:", e);
      setPending([]);
    }
  };

  useEffect(() => {
    if (isAdmin) {
      loadPending();
      productService
        .getProducts({ page: 1, pageSize: 1 })
        .then((res) => {
          if (res?.total) {
            setTotalProducts(res.total);
          }
        })
        .catch(() => {});
    }
  }, [isAdmin]);

  if (!isAuthenticated || !isAdmin) {
    return (
      <AccessDenied
        title={
          !isAuthenticated
            ? "Yêu cầu đăng nhập Quản trị viên"
            : "Truy cập bị từ chối (403 Forbidden)"
        }
        desc={
          !isAuthenticated
            ? "Vui lòng đăng nhập với tài khoản Quản trị viên (Admin) để tiếp tục."
            : `Tài khoản ${user?.email || ""} (Vai trò: ${user?.role || "Member"}) không có quyền Quản trị viên. Trang này chỉ dành riêng cho Admin hệ thống.`
        }
        actionLabel={!isAuthenticated ? "Đăng nhập Admin" : "Về trang chủ"}
        onAction={() => go(!isAuthenticated ? "auth" : "home")}
        go={go}
      />
    );
  }

  const handleApprove = async (id: string, name: string) => {
    try {
      await adminService.approveProduct(id);
      setNotice(`Đã phê duyệt thành công sản phẩm "${name}"!`);
      setPending((items) => items.filter((item) => item.id !== id));
    } catch (err: any) {
      alert(err.response?.data?.message || "Lỗi phê duyệt.");
    }
  };

  const handleRejectConfirm = async (reason: string) => {
    if (!rejecting) return;
    try {
      await adminService.rejectProduct(rejecting.id, reason);
      setNotice(`Đã từ chối "${rejecting.name}" với lý do: "${reason}"`);
      setPending((items) => items.filter((item) => item.id !== rejecting.id));
      setRejecting(null);
    } catch (err: any) {
      alert(err.response?.data?.message || "Lỗi từ chối.");
    }
  };

  return (
    <div className="app-page admin-page">
      <main className="dashboard-shell">
        <div className="page-heading">
          <div>
            <div style={{ marginBottom: "12px" }}>
              <Badge tone="dark">ADMIN CONSOLE</Badge>
            </div>
            <h1>Bảng điều khiển Quản trị viên</h1>
            <p>
              Quản trị toàn hệ thống GiftFinder <b>·</b>{" "}
              <span className="db-status">
                <i />
                PostgreSQL đã kết nối
              </span>{" "}
              <b>·</b> {totalProducts}+ sản phẩm thật
            </p>
          </div>
          <div className="heading-actions">
            <Button
              variant="primary"
              icon="arrow-left"
              onClick={() => go("home")}
            >
              Xem website
            </Button>
          </div>
        </div>

        {notice && (
          <div
            style={{
              background: "#ecfdf5",
              border: "1px solid #a7f3d0",
              color: "#065f46",
              padding: "12px 16px",
              borderRadius: "12px",
              margin: "16px 0",
              fontSize: "13px",
              fontWeight: 600,
            }}
          >
            ✓ {notice}
          </div>
        )}

        <section className="metric-grid admin-metrics">
          <MetricCard
            icon="gift"
            label="Tổng sản phẩm hệ thống"
            value={String(totalProducts)}
            detail="PostgreSQL Database"
            tone="indigo"
          />
          <MetricCard
            icon="clock"
            label="Chờ phê duyệt"
            value={String(pending.length)}
            detail="Cần xử lý hôm nay"
            tone="rose"
          />
          <MetricCard
            icon="shield"
            label="VNPAY Sandbox Gateway"
            value="Hoạt động"
            detail="Giao dịch bảo mật"
            tone="green"
          />
          <MetricCard
            icon="users"
            label="Tài khoản quản trị"
            value="System Admin"
            detail="Toàn quyền hệ thống"
            tone="blue"
          />
        </section>

        <section className="content-card moderation-card">
          <div className="section-heading moderation-heading">
            <div>
              <span className="eyebrow">PRODUCT MODERATION</span>
              <h2>Duyệt sản phẩm mới từ đối tác</h2>
              <p>
                Kiểm tra nội dung và chất lượng trước khi xuất bản lên
                GiftFinder.
              </p>
            </div>
            <Badge tone="rose-soft">{pending.length} chờ duyệt</Badge>
          </div>
          {pending.length ? (
            <div className="moderation-list">
              {pending.map((item) => (
                <article className="moderation-row" key={item.id}>
                  <img src={item.image} alt="" />
                  <div className="moderation-info">
                    <h3>{item.name}</h3>
                    <p>
                      <Icon name="store" size={14} />
                      {item.shop}
                    </p>
                    <div>
                      <strong>{item.price}</strong>
                      <Badge tone="neutral">{item.category}</Badge>
                    </div>
                  </div>
                  <button
                    className="external-link"
                    aria-label="Mở sản phẩm gốc"
                    onClick={() => window.open("https://shopee.vn", "_blank")}
                  >
                    <Icon name="external" />
                  </button>
                  <div className="moderation-actions">
                    <Button
                      variant="success"
                      icon="check"
                      onClick={() => handleApprove(item.id, item.name)}
                    >
                      Phê duyệt
                    </Button>
                    <Button
                      variant="danger"
                      icon="close"
                      onClick={() =>
                        setRejecting({ id: item.id, name: item.name })
                      }
                    >
                      Từ chối
                    </Button>
                  </div>
                </article>
              ))}
            </div>
          ) : (
            <div className="empty-state">
              <div className="empty-gift">
                <Icon name="gift" size={38} />
                <span>
                  <Icon name="check" />
                </span>
              </div>
              <h3>Toàn bộ quà tặng đã được kiểm duyệt an toàn!</h3>
              <p>
                Không còn sản phẩm nào đang chờ xử lý. Cơ sở dữ liệu PostgreSQL
                đã cập nhật.
              </p>
            </div>
          )}
        </section>
      </main>
      {rejecting && (
        <RejectModal
          name={rejecting.name}
          onClose={() => setRejecting(null)}
          onReject={handleRejectConfirm}
        />
      )}
    </div>
  );
}

function ProductDetail({
  go,
  openAffiliate,
  product,
  products = defaultProducts,
  onSelectProduct,
  catalogFilter,
  onNotify,
}: {
  go: (p: Page) => void;
  openAffiliate: (p?: ProductItem) => void;
  product?: ProductItem | null;
  products?: ProductItem[];
  onSelectProduct?: (p: ProductItem) => void;
  catalogFilter?: {
    title: string;
    slug: string;
    parentCategory: string;
  };
  onNotify?: (msg: string) => void;
}) {
  const { addToCart } = useCart();
  const { isWishlisted, toggleWishlist } = useWishlist();
  const { isAuthenticated } = useAuth();
  const currentProduct = product || products[0] || defaultProducts[0];

  const [photo, setPhoto] = useState(currentProduct.image || images.candle);
  const [qty, setQty] = useState(1);
  const [activeTab, setActiveTab] = useState<"desc" | "reviews" | "policy">(
    "desc",
  );
  const [descExpanded, setDescExpanded] = useState(false);
  const [detailDto, setDetailDto] = useState<ProductDto | null>(null);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [relatedProducts, setRelatedProducts] = useState<ProductItem[]>([]);
  const [relatedStartIdx, setRelatedStartIdx] = useState(0);
  const [priceHistory, setPriceHistory] = useState<{
    lowestPrice: number;
    highestPrice: number;
    history: Array<{ oldPrice: number; newPrice: number; changedAt: string }>;
  } | null>(null);
  const [trackingPrice, setTrackingPrice] = useState(false);

  useEffect(() => {
    if (currentProduct?.image) {
      setPhoto(currentProduct.image);
    }
    setActiveTab("desc");
    setDescExpanded(false);
    setRelatedStartIdx(0);
  }, [currentProduct?.id, currentProduct?.name, currentProduct?.image]);

  // Tải thông tin chi tiết thật từ DB (description, tags, rating, source), sản phẩm liên quan ("Xem thêm") và lịch sử giá
  useEffect(() => {
    let isCancelled = false;
    const pid = currentProduct?.id;
    if (!pid || pid.length < 20) {
      setDetailDto(null);
      setRelatedProducts([]);
      setPriceHistory(null);
      return;
    }

    setLoadingDetail(true);
    productService
      .getProductById(pid)
      .then((dto) => {
        if (!isCancelled) {
          setDetailDto(dto);
          if (dto.imageUrl) setPhoto(getHighResImageUrl(dto.imageUrl));
        }
      })
      .catch(() => {
        if (!isCancelled) setDetailDto(null);
      })
      .finally(() => {
        if (!isCancelled) setLoadingDetail(false);
      });

    catalogService
      .getRelatedProducts(pid, 10)
      .then((items) => {
        if (isCancelled) return;
        const mapped: ProductItem[] = (items || []).map((item) => {
          const origPrice =
            item.originalPrice && item.originalPrice > item.price
              ? item.originalPrice
              : Math.round(item.price * 1.25);
          const disc = Math.max(
            5,
            Math.round((1 - item.price / origPrice) * 100),
          );
          const srcUpper = (item.source || "").toUpperCase();
          const urlLower = (item.originalUrl || "").toLowerCase();
          const platform =
            srcUpper.includes("TIKTOK") || urlLower.includes("tiktok")
              ? "TIKTOK"
              : srcUpper.includes("SHOPEE") || urlLower.includes("shopee")
                ? "SHOPEE"
                : "DIRECT";
          return {
            id: item.id,
            name: item.name,
            price: item.price,
            old: origPrice,
            rating: String(item.rating > 0 ? item.rating.toFixed(1) : "4.9"),
            sold: String(item.totalReviews > 0 ? item.totalReviews : 128),
            image: getHighResImageUrl(item.imageUrl),
            platform,
            discount: `-${disc}%`,
            originalUrl: item.originalUrl,
            affiliateUrl: item.affiliateUrl,
            tags: (item.tags || []).map((t: any) =>
              typeof t === "string" ? t : t.name,
            ),
          };
        });
        setRelatedProducts(mapped);
      })
      .catch(() => {
        if (!isCancelled) setRelatedProducts([]);
      });

    productService
      .getPriceHistory(pid)
      .then((hist) => {
        if (
          !isCancelled &&
          hist &&
          Array.isArray(hist.history) &&
          hist.history.length >= 1
        ) {
          setPriceHistory({
            lowestPrice: hist.lowestPrice,
            highestPrice: hist.highestPrice,
            history: hist.history,
          });
        } else if (!isCancelled) {
          setPriceHistory(null);
        }
      })
      .catch(() => {
        if (!isCancelled) setPriceHistory(null);
      });

    return () => {
      isCancelled = true;
    };
  }, [currentProduct?.id]);

  const effectivePlatform = (() => {
    const src = (
      detailDto?.source ||
      currentProduct.platform ||
      ""
    ).toUpperCase();
    const url = (
      detailDto?.originalUrl ||
      currentProduct.originalUrl ||
      ""
    ).toLowerCase();
    if (src.includes("TIKTOK") || url.includes("tiktok")) return "TIKTOK";
    if (src.includes("SHOPEE") || url.includes("shopee")) return "SHOPEE";
    if (src.includes("DIRECT")) return "DIRECT";
    return currentProduct.platform === "DIRECT" ? "DIRECT" : "SHOPEE";
  })();

  const isDirectProduct = effectivePlatform === "DIRECT";
  const platformLabel =
    effectivePlatform === "TIKTOK"
      ? "TikTok Shop"
      : effectivePlatform === "SHOPEE"
        ? "Shopee"
        : "GiftFinder Direct";

  const effectivePrice = detailDto?.price || currentProduct.price;
  const effectiveOldPrice =
    detailDto?.originalPrice && detailDto.originalPrice > effectivePrice
      ? detailDto.originalPrice
      : currentProduct.old > effectivePrice
        ? currentProduct.old
        : Math.round(effectivePrice * 1.25);
  const discountPercent = Math.max(
    5,
    Math.round((1 - effectivePrice / effectiveOldPrice) * 100),
  );
  const effectiveRating =
    detailDto && detailDto.rating > 0
      ? detailDto.rating.toFixed(1)
      : currentProduct.rating || "4.9";
  const effectiveReviewsCount =
    detailDto && detailDto.totalReviews > 0
      ? detailDto.totalReviews
      : parseInt(String(currentProduct.sold).replace(/\D/g, ""), 10) || 128;

  const liked = isWishlisted(currentProduct.id || currentProduct.name);

  const handleToggleWishlist = async () => {
    const added = await toggleWishlist(currentProduct);
    onNotify?.(
      added
        ? "❤️ Đã lưu sản phẩm vào danh sách yêu thích!"
        : "Đã bỏ sản phẩm khỏi danh sách yêu thích.",
    );
  };

  const handleTrackPriceDrop = async () => {
    if (!currentProduct.id) {
      onNotify?.("🔔 Đã bật thông báo khi sản phẩm giảm giá!");
      return;
    }
    if (!isAuthenticated) {
      onNotify?.(
        "🔔 Vui lòng đăng nhập để nhận thông báo qua Email khi sản phẩm giảm giá!",
      );
      go("auth");
      return;
    }
    setTrackingPrice(true);
    try {
      const res = await productService.trackPriceDrop(currentProduct.id);
      onNotify?.(res?.message || "🔔 Đã bật chuông theo dõi giá thành công!");
    } catch (err: any) {
      const msg =
        err.response?.data?.message ||
        err.response?.data?.Message ||
        "🔔 Sản phẩm đã có trong danh sách theo dõi giá của bạn!";
      onNotify?.(msg);
    } finally {
      setTrackingPrice(false);
    }
  };

  // Phân loại Tag từ dữ liệu thật của sản phẩm
  const productTags = detailDto?.tags || [];
  const occasionTags = productTags
    .filter(
      (t) =>
        ["Occasion", "0"].includes(String(t.type)) ||
        /sinh nhật|valentine|20\/10|8\/3|20\/11|giáng sinh|noel|tết|kỷ niệm|tốt nghiệp|tân gia/i.test(
          t.name,
        ),
    )
    .map((t) => t.name);
  const recipientTags = productTags
    .filter(
      (t) =>
        ["Recipient", "1"].includes(String(t.type)) ||
        /bạn gái|bạn trai|mẹ|bố|người yêu|vợ|chồng|sếp|đồng nghiệp|thầy cô|bạn thân/i.test(
          t.name,
        ),
    )
    .map((t) => t.name);
  const allTagNames = productTags.map((t) => t.name);

  // Xử lý mô tả thật từ DB
  const rawDescription = (detailDto?.description || "").trim();
  const isLongDescription =
    rawDescription.length > 420 || rawDescription.split("\n").length > 8;

  // Vẽ đường SVG từ lịch sử giá thật (nếu có >= 1 lần biến động giá)
  const renderRealPriceChart = () => {
    if (!priceHistory || priceHistory.history.length === 0) return null;
    const allValues = [
      priceHistory.history[0].oldPrice,
      ...priceHistory.history.map((h) => h.newPrice),
    ];
    if (allValues.length < 2) return null;

    const minVal = Math.min(...allValues);
    const maxVal = Math.max(...allValues);
    const range = Math.max(1, maxVal - minVal);

    const coords = allValues.map((val, idx) => {
      const x = Math.round((idx / (allValues.length - 1)) * 400) + 10;
      const y = Math.round(62 - ((val - minVal) / range) * 46);
      return `${x},${y}`;
    });
    const polylinePoints = coords.join(" ");

    return (
      <div className="price-chart">
        <div>
          <span>📈 Biểu đồ lịch sử giá thực tế</span>
          <Badge tone="green">
            Thấp nhất: {money(priceHistory.lowestPrice)}
          </Badge>
        </div>
        <svg viewBox="0 0 420 76" preserveAspectRatio="none">
          <polyline
            fill="none"
            stroke="#16a34a"
            strokeWidth="3"
            points={polylinePoints}
          />
        </svg>
        <small>
          <span>Cao nhất: {money(priceHistory.highestPrice)}</span>
          <b>Hôm nay: {money(effectivePrice)}</b>
        </small>
      </div>
    );
  };

  const visibleRelated = relatedProducts.slice(
    relatedStartIdx,
    relatedStartIdx + 5,
  );

  return (
    <main className="shell pdp">
      <div
        className="breadcrumb"
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          flexWrap: "wrap",
          gap: 10,
        }}
      >
        <div>
          <span onClick={() => go("home")} style={{ cursor: "pointer" }}>
            Trang chủ
          </span>{" "}
          <Icon name="chevron" size={12} />{" "}
          <span onClick={() => go("catalog")} style={{ cursor: "pointer" }}>
            {catalogFilter?.title || "Danh mục quà tặng"}
          </span>{" "}
          <Icon name="chevron" size={12} /> <b>{currentProduct.name}</b>
        </div>
        <button
          type="button"
          onClick={() => {
            if (typeof window !== "undefined" && window.history.length > 1) {
              window.history.back();
            } else {
              go("home");
            }
          }}
          style={{
            background: "#fff",
            border: "1px solid var(--line)",
            borderRadius: "8px",
            padding: "5px 12px",
            fontSize: "12px",
            fontWeight: 600,
            color: "var(--ink)",
            cursor: "pointer",
            display: "inline-flex",
            alignItems: "center",
            gap: "5px",
            boxShadow: "0 1px 3px rgba(0,0,0,0.04)",
            transition: "all 0.15s ease",
          }}
          title="Quay lại vị trí trang trước"
        >
          ← Quay lại
        </button>
      </div>

      <div className="pdp-main">
        <section className="gallery">
          <div className="gallery-main">
            <img src={getHighResImageUrl(photo)} alt={currentProduct.name} />
          </div>
          {currentProduct.image && (
            <div className="thumbnails" style={{ gridTemplateColumns: "84px" }}>
              <button
                className="active"
                onClick={() => setPhoto(currentProduct.image)}
              >
                <img
                  src={getHighResImageUrl(currentProduct.image)}
                  alt={currentProduct.name}
                />
              </button>
            </div>
          )}
        </section>

        <section className="buy-panel">
          <Badge tone="rose">
            {isDirectProduct ? "GIFTFINDER DIRECT" : effectivePlatform}
          </Badge>
          <h1>{currentProduct.name}</h1>
          <button
            className="shop-name"
            onClick={() => {
              if (!isDirectProduct) openAffiliate(currentProduct);
            }}
            style={{ cursor: isDirectProduct ? "default" : "pointer" }}
          >
            {isDirectProduct
              ? "Gian hàng chính thức GiftFinder Direct"
              : `Bán trên ${platformLabel}`}{" "}
            <span>✓</span>
          </button>

          <div className="pdp-meta">
            <span className="stars">★★★★★</span> <b>{effectiveRating}</b> ·{" "}
            <u
              style={{ cursor: "pointer" }}
              onClick={() => {
                setActiveTab("reviews");
                document
                  .querySelector(".below-fold")
                  ?.scrollIntoView({ behavior: "smooth" });
              }}
            >
              {effectiveReviewsCount} đánh giá trên {platformLabel}
            </u>
          </div>

          <div className="pdp-price">
            <b>{money(effectivePrice)}</b>
            <del>{money(effectiveOldPrice)}</del>
            <Badge tone="green">
              Tiết kiệm {money(Math.max(0, effectiveOldPrice - effectivePrice))}{" "}
              (-{discountPercent}%)
            </Badge>
          </div>

          {renderRealPriceChart()}

          {allTagNames.length > 0 && (
            <div
              style={{
                display: "flex",
                flexWrap: "wrap",
                gap: 6,
                margin: "12px 0",
              }}
            >
              {allTagNames.map((t) => (
                <Badge key={t} tone="neutral">
                  #{t}
                </Badge>
              ))}
            </div>
          )}

          {isDirectProduct ? (
            <>
              <div className="quantity-row">
                <label>Số lượng</label>
                <div className="stepper">
                  <button onClick={() => setQty(Math.max(1, qty - 1))}>
                    <Icon name="minus" size={14} />
                  </button>
                  <b>{qty}</b>
                  <button onClick={() => setQty(qty + 1)}>
                    <Icon name="plus" size={14} />
                  </button>
                </div>
                <small>Còn hàng tại kho GiftFinder</small>
              </div>
              <div className="direct-actions">
                <Button
                  variant="secondary"
                  onClick={() => {
                    addToCart(
                      { ...currentProduct, platform: "DIRECT" },
                      qty,
                      "Tiêu chuẩn",
                    );
                    go("cart");
                  }}
                  icon="cart"
                >
                  Thêm vào giỏ hàng
                </Button>
                <Button
                  variant="rose"
                  onClick={() => {
                    addToCart(
                      { ...currentProduct, platform: "DIRECT" },
                      qty,
                      "Tiêu chuẩn",
                    );
                    go("checkout");
                  }}
                >
                  Mua ngay với VNPAY
                </Button>
              </div>
            </>
          ) : (
            <div className="pdp-affiliate-cta-box">
              <button
                type="button"
                className={`pdp-platform-buy-btn ${effectivePlatform === "TIKTOK" ? "tiktok" : "shopee"}`}
                onClick={() => openAffiliate(currentProduct)}
              >
                <span>Mua trên {platformLabel} ↗</span>
              </button>

              <div className="pdp-secondary-actions">
                <button
                  type="button"
                  className={`pdp-secondary-btn ${liked ? "active" : ""}`}
                  onClick={handleToggleWishlist}
                >
                  <Icon name="heart" size={15} />
                  {liked ? "Đã lưu yêu thích" : "Lưu vào yêu thích"}
                </button>
                <button
                  type="button"
                  className="pdp-secondary-btn"
                  onClick={handleTrackPriceDrop}
                  disabled={trackingPrice}
                >
                  <Icon name="bell" size={15} />
                  {trackingPrice ? "Đang bật..." : "Báo khi giảm giá"}
                </button>
              </div>

              <div className="pdp-affiliate-note">
                Sản phẩm do gian hàng đối tác trên <b>{platformLabel}</b> bán và
                vận chuyển trực tiếp. GiftFinder giúp bạn chọn đúng món quà phù
                hợp và theo dõi mức giá tốt nhất.
              </div>
            </div>
          )}

          <div className="trust-banner">
            {(isDirectProduct
              ? [
                  ["✓", "100% quà chính hãng"],
                  ["↻", "Đổi trả 7 ngày"],
                  ["⚡", "Thanh toán VNPAY bảo mật"],
                ]
              : [
                  ["✓", `Giá cập nhật từ ${platformLabel}`],
                  ["🔔", "Hỗ trợ báo khi giảm giá"],
                  ["🛡️", `Chuyển sang ${platformLabel} an toàn`],
                ]
            ).map((x) => (
              <span key={x[1]}>
                <i>{x[0]}</i>
                {x[1]}
              </span>
            ))}
          </div>
        </section>
      </div>

      {/* ========================================================
          KHU VỰC 3 TAB: MÔ TẢ SẢN PHẨM | ĐÁNH GIÁ | CHÍNH SÁCH ĐỔI TRẢ
          ======================================================== */}
      <section className="below-fold">
        <div className="tabs">
          <button
            type="button"
            className={activeTab === "desc" ? "active" : ""}
            onClick={() => setActiveTab("desc")}
          >
            Mô tả sản phẩm
          </button>
          <button
            type="button"
            className={activeTab === "reviews" ? "active" : ""}
            onClick={() => setActiveTab("reviews")}
          >
            Đánh giá ({effectiveReviewsCount})
          </button>
          <button
            type="button"
            className={activeTab === "policy" ? "active" : ""}
            onClick={() => setActiveTab("policy")}
          >
            Chính sách đổi trả
          </button>
        </div>

        {activeTab === "desc" && (
          <div className="pdp-tab-panel">
            <div className="pdp-specs-grid">
              <div className="pdp-spec-item">
                <small>Nguồn gian hàng</small>
                <b>{platformLabel}</b>
              </div>
              <div className="pdp-spec-item">
                <small>Mức giá hiện tại</small>
                <b>
                  {money(effectivePrice)} (Tiết kiệm {discountPercent}%)
                </b>
              </div>
              <div className="pdp-spec-item">
                <small>Phù hợp dịp tặng</small>
                <b>
                  {occasionTags.length > 0
                    ? occasionTags.join(", ")
                    : allTagNames.length > 0
                      ? allTagNames.slice(0, 3).join(", ")
                      : "Sinh nhật, Kỷ niệm, Quà tặng"}
                </b>
              </div>
              <div className="pdp-spec-item">
                <small>Đối tượng người nhận</small>
                <b>
                  {recipientTags.length > 0
                    ? recipientTags.join(", ")
                    : "Người thương, Bạn bè, Người thân"}
                </b>
              </div>
            </div>

            {loadingDetail ? (
              <div
                style={{
                  padding: "24px 0",
                  color: "var(--muted)",
                  fontSize: 14,
                }}
              >
                Đang tải mô tả chi tiết sản phẩm...
              </div>
            ) : rawDescription ? (
              <div>
                <h3
                  style={{
                    fontSize: 17,
                    fontWeight: 700,
                    marginBottom: 12,
                    color: "var(--ink)",
                  }}
                >
                  Chi tiết sản phẩm
                </h3>
                <div
                  className={`pdp-desc-content ${isLongDescription && !descExpanded ? "collapsed" : ""}`}
                >
                  {rawDescription}
                </div>
                {isLongDescription && (
                  <button
                    type="button"
                    className="pdp-desc-toggle"
                    onClick={() => setDescExpanded(!descExpanded)}
                  >
                    {descExpanded
                      ? "Thu gọn mô tả ▴"
                      : "Xem thêm mô tả đầy đủ ▾"}
                  </button>
                )}
              </div>
            ) : (
              <div
                style={{
                  padding: "24px",
                  background: "#f8fafc",
                  borderRadius: 12,
                  border: "1px dashed #cbd5e1",
                  textAlign: "center",
                }}
              >
                <p
                  style={{
                    fontSize: 14,
                    color: "var(--slate)",
                    marginBottom: 12,
                  }}
                >
                  Thông tin mô tả chi tiết, thông số phân loại và hình ảnh thực
                  tế của sản phẩm được cập nhật trực tiếp tại gian hàng{" "}
                  <b>{platformLabel}</b>.
                </p>
                {!isDirectProduct && (
                  <Button
                    variant="rose"
                    onClick={() => openAffiliate(currentProduct)}
                  >
                    Xem mô tả đầy đủ trên gian hàng {platformLabel} ↗
                  </Button>
                )}
              </div>
            )}
          </div>
        )}

        {activeTab === "reviews" && (
          <div className="pdp-tab-panel">
            {isDirectProduct ? (
              <div style={{ textAlign: "center", padding: "32px 16px" }}>
                <h3 style={{ fontSize: 17, fontWeight: 700, marginBottom: 8 }}>
                  Chưa có đánh giá trực tiếp
                </h3>
                <p
                  style={{
                    fontSize: 14,
                    color: "var(--muted)",
                    maxWidth: 460,
                    margin: "0 auto",
                  }}
                >
                  Đánh giá thực tế sẽ được hiển thị sau khi có khách hàng đặt
                  mua và hoàn tất đơn hàng qua cổng VNPAY.
                </p>
              </div>
            ) : (
              <div
                style={{
                  display: "flex",
                  flexWrap: "wrap",
                  alignItems: "center",
                  justifyContent: "space-between",
                  gap: 24,
                  padding: "24px 28px",
                  background: "#fff8f9",
                  border: "1px solid #fecdd3",
                  borderRadius: 16,
                }}
              >
                <div style={{ display: "flex", alignItems: "center", gap: 20 }}>
                  <div
                    style={{
                      textAlign: "center",
                      paddingRight: 20,
                      borderRight: "1px solid #fecdd3",
                    }}
                  >
                    <div
                      style={{
                        fontSize: 34,
                        fontWeight: 900,
                        color: "var(--rose)",
                        lineHeight: 1,
                      }}
                    >
                      {effectiveRating}
                      <span
                        style={{
                          fontSize: 16,
                          color: "var(--muted)",
                          fontWeight: 600,
                        }}
                      >
                        /5
                      </span>
                    </div>
                    <div
                      className="stars"
                      style={{ fontSize: 15, margin: "6px 0 4px" }}
                    >
                      ★★★★★
                    </div>
                    <small
                      style={{
                        fontSize: 11,
                        color: "var(--muted)",
                        fontWeight: 600,
                      }}
                    >
                      {effectiveReviewsCount} đánh giá
                    </small>
                  </div>
                  <div>
                    <h3
                      style={{
                        margin: "0 0 6px",
                        fontSize: 16,
                        fontWeight: 700,
                        color: "var(--ink)",
                      }}
                    >
                      Đánh giá xác thực từ người mua trên {platformLabel}
                    </h3>
                    <p
                      style={{
                        margin: 0,
                        fontSize: 13.5,
                        color: "var(--slate)",
                        lineHeight: 1.6,
                      }}
                    >
                      Sản phẩm hiện đạt điểm đánh giá <b>{effectiveRating}/5</b>{" "}
                      dựa trên{" "}
                      <b>{effectiveReviewsCount} lượt mua & nhận xét thực tế</b>{" "}
                      trên sàn {platformLabel}. Để đảm bảo tính minh bạch, bạn
                      có thể xem trực tiếp toàn bộ bình luận, video và ảnh chụp
                      thực tế từ khách hàng trên gian hàng gốc.
                    </p>
                  </div>
                </div>
                <Button
                  variant="rose"
                  onClick={() => openAffiliate(currentProduct)}
                >
                  Xem toàn bộ đánh giá & ảnh thực tế trên {platformLabel} ↗
                </Button>
              </div>
            )}
          </div>
        )}

        {activeTab === "policy" && (
          <div className="pdp-tab-panel">
            {isDirectProduct ? (
              <div>
                <h3
                  style={{ margin: "0 0 10px", fontSize: 17, fontWeight: 700 }}
                >
                  Chính sách đổi trả & bảo hành GiftFinder Direct
                </h3>
                <p
                  style={{
                    fontSize: 13.5,
                    color: "var(--slate)",
                    marginBottom: 16,
                  }}
                >
                  Áp dụng cho các sản phẩm do GiftFinder trực tiếp đóng gói, vận
                  chuyển và thu tiền qua cổng VNPAY:
                </p>
                <div className="pdp-policy-grid">
                  <div className="pdp-policy-card">
                    <h4>1. Đổi trả trong 7 ngày</h4>
                    <p>
                      Hỗ trợ 1 đổi 1 hoặc hoàn tiền 100% về tài khoản VNPAY nếu
                      sản phẩm bị lỗi từ nhà sản xuất, hư hỏng hoặc móp vỡ trong
                      quá trình vận chuyển.
                    </p>
                  </div>
                  <div className="pdp-policy-card">
                    <h4>2. Điều kiện áp dụng</h4>
                    <p>
                      Sản phẩm còn nguyên tem mác, hộp quà và phụ kiện đi kèm.
                      Quý khách vui lòng quay video mở hộp (unboxing) để được xử
                      lý nhanh nhất.
                    </p>
                  </div>
                  <div className="pdp-policy-card">
                    <h4>3. Quy trình 3 bước</h4>
                    <p>
                      Liên hệ Hotline/Email hỗ trợ → Gửi video/hình ảnh xác nhận
                      tình trạng → GiftFinder thu hồi tận nơi và gửi bù sản phẩm
                      mới hoặc hoàn tiền.
                    </p>
                  </div>
                </div>
              </div>
            ) : (
              <div>
                <div
                  style={{
                    padding: "14px 18px",
                    background: "#fffbeb",
                    border: "1px solid #fde68a",
                    borderRadius: 12,
                    color: "#92400e",
                    fontSize: 13.5,
                    fontWeight: 600,
                    marginBottom: 18,
                  }}
                >
                  ℹ️ Lưu ý quan trọng: Đơn hàng mua trên sàn{" "}
                  <b>{platformLabel}</b> sẽ do sàn {platformLabel} và nhà bán
                  hàng trực tiếp xử lý vận chuyển, bảo hành và đổi trả.
                  GiftFinder không thu tiền các đơn hàng này.
                </div>
                <div className="pdp-policy-grid">
                  <div className="pdp-policy-card">
                    <h4>🛡️ Quyền lợi người mua trên {platformLabel}</h4>
                    <p>
                      Khi đặt mua trên {platformLabel}, đơn hàng của bạn được
                      bảo vệ bởi chương trình đồng kiểm và Trả hàng / Hoàn tiền
                      theo quy định hiện hành của sàn {platformLabel} nếu sản
                      phẩm không đúng mô tả hoặc phát sinh lỗi.
                    </p>
                  </div>
                  <div className="pdp-policy-card">
                    <h4>📦 Hướng dẫn yêu cầu Đổi trả / Hoàn tiền</h4>
                    <p>
                      Bạn chỉ cần mở ứng dụng {platformLabel} → vào mục{" "}
                      <b>Đơn mua</b> → chọn đơn hàng tương ứng và bấm{" "}
                      <b>Yêu cầu Trả hàng/Hoàn tiền</b> kèm hình ảnh/video mở
                      hộp để được sàn hỗ trợ miễn phí.
                    </p>
                  </div>
                  <div className="pdp-policy-card">
                    <h4>🔗 Chính sách chính thức từ {platformLabel}</h4>
                    <p style={{ marginBottom: 12 }}>
                      Xem chi tiết thời hạn và điều kiện trả hàng mới nhất trên
                      trang hỗ trợ chính thức của {platformLabel}:
                    </p>
                    <a
                      href={
                        effectivePlatform === "TIKTOK"
                          ? "https://seller-vn.tiktok.com"
                          : "https://help.shopee.vn"
                      }
                      target="_blank"
                      rel="noopener noreferrer"
                      style={{
                        color: "var(--rose)",
                        fontWeight: 700,
                        fontSize: 13,
                        textDecoration: "underline",
                      }}
                    >
                      Mở Trung tâm trợ giúp {platformLabel} ↗
                    </a>
                  </div>
                </div>
              </div>
            )}
          </div>
        )}
      </section>

      {/* ========================================================
          MỤC "XEM THÊM" — SẢN PHẨM LIÊN QUAN THẬT (0 TOKEN AI, KHÔNG NHÉT BỪA)
          ======================================================== */}
      {relatedProducts.length > 0 && (
        <section className="section">
          <SectionTitle
            eyebrow="GỢI Ý CÙNG CHỦ ĐỀ"
            title="Xem thêm"
            action={
              relatedProducts.length > 5 ? (
                <div className="related-nav-btns">
                  <button
                    type="button"
                    disabled={relatedStartIdx === 0}
                    onClick={() =>
                      setRelatedStartIdx(Math.max(0, relatedStartIdx - 5))
                    }
                    aria-label="Xem trước"
                  >
                    ‹
                  </button>
                  <button
                    type="button"
                    disabled={relatedStartIdx + 5 >= relatedProducts.length}
                    onClick={() =>
                      setRelatedStartIdx(
                        Math.min(
                          Math.max(0, relatedProducts.length - 5),
                          relatedStartIdx + 5,
                        ),
                      )
                    }
                    aria-label="Xem tiếp"
                  >
                    ›
                  </button>
                </div>
              ) : undefined
            }
          />
          <div className="product-row">
            {visibleRelated.map((p, i) => (
              <ProductCard
                key={p.id || p.name + i}
                product={p}
                onOpen={() => {
                  onSelectProduct?.(p);
                  window.scrollTo({ top: 0, behavior: "smooth" });
                }}
                onAffiliate={() => openAffiliate(p)}
              />
            ))}
          </div>
        </section>
      )}
    </main>
  );
}

function Cart({ go }: { go: (p: Page) => void }) {
  const {
    items,
    updateQuantity,
    removeFromCart,
    subtotal,
    shippingFee,
    giftWrapFee,
    hasGiftWrap,
    setHasGiftWrap,
    total,
  } = useCart();
  const [voucher, setVoucher] = useState(false);

  const finalTotal = Math.max(0, total - (voucher ? 50000 : 0));

  return (
    <main className="shell flow-page">
      <div className="flow-header">
        <div>
          <span>GIỎ HÀNG CỦA BẠN</span>
          <h1>Sẵn sàng gửi yêu thương?</h1>
        </div>
        <button onClick={() => go("catalog")}>← Tiếp tục mua sắm</button>
      </div>

      {items.length === 0 ? (
        <div
          className="empty-state"
          style={{ padding: "60px 20px", textAlign: "center" }}
        >
          <div className="empty-gift" style={{ margin: "0 auto 16px" }}>
            <Icon name="cart" size={38} />
          </div>
          <h2
            style={{ fontSize: "20px", fontWeight: 700, marginBottom: "8px" }}
          >
            Giỏ hàng của bạn đang trống
          </h2>
          <p
            style={{
              color: "var(--muted)",
              maxWidth: "420px",
              margin: "0 auto 24px",
              lineHeight: 1.6,
            }}
          >
            Bạn chưa thêm món quà nào vào giỏ. Hãy dạo quanh GiftFinder để khám
            phá những món quà đặc biệt nhé!
          </p>
          <Button variant="rose" onClick={() => go("catalog")}>
            Khám phá quà tặng ngay
          </Button>
        </div>
      ) : (
        <div className="cart-layout">
          <section className="cart-items">
            <div className="cart-label">
              <b>Sản phẩm ({items.length})</b>
              <span>Giá</span>
              <span>Số lượng</span>
              <span>Tạm tính</span>
            </div>
            {items.map((item) => (
              <article className="cart-item" key={item.id}>
                <img src={item.image} alt={item.name} />
                <div>
                  <Badge tone={item.platform === "DIRECT" ? "indigo" : "dark"}>
                    {item.platform === "DIRECT"
                      ? "GIFTFINDER DIRECT"
                      : item.platform}
                  </Badge>
                  <h3>{item.name}</h3>
                  <p>Phân loại: {item.variant}</p>
                  <button onClick={() => removeFromCart(item.id)}>
                    <Icon name="trash" size={14} /> Xóa
                  </button>
                </div>
                <b>{money(item.price)}</b>
                <div className="stepper">
                  <button
                    onClick={() =>
                      updateQuantity(item.id, Math.max(1, item.quantity - 1))
                    }
                  >
                    <Icon name="minus" size={13} />
                  </button>
                  <b>{item.quantity}</b>
                  <button
                    onClick={() => updateQuantity(item.id, item.quantity + 1)}
                  >
                    <Icon name="plus" size={13} />
                  </button>
                </div>
                <strong>{money(item.price * item.quantity)}</strong>
              </article>
            ))}
            <div
              className="wrap-preview"
              style={{ cursor: "pointer" }}
              onClick={() => setHasGiftWrap(!hasGiftWrap)}
            >
              <img src={images.rose} alt="Gói quà" />
              <div>
                <Badge tone={hasGiftWrap ? "rose" : "neutral"}>
                  {hasGiftWrap
                    ? "GÓI QUÀ CAO CẤP (ĐÃ CHỌN)"
                    : "GÓI QUÀ CAO CẤP (CHƯA CHỌN)"}
                </Badge>
                <b>Hộp quà & thiệp viết tay cá nhân hóa</b>
                <span>“Chúc em luôn rạng rỡ như những đóa hoa...”</span>
              </div>
              <strong>{hasGiftWrap ? "+30.000đ" : "0đ"}</strong>
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  setHasGiftWrap(!hasGiftWrap);
                }}
              >
                {hasGiftWrap ? "Bỏ chọn" : "Thêm gói quà"}
              </button>
            </div>
            <div className="cart-assurance">
              <span>
                <Icon name="shield" /> Thanh toán bảo mật
              </span>
              <span>↻ Đổi trả trong 7 ngày</span>
              <span>⚡ Giao nhanh hỏa tốc 2h</span>
            </div>
          </section>
          <OrderSummary
            subtotal={subtotal}
            shippingFee={shippingFee}
            giftWrapFee={giftWrapFee}
            voucher={voucher}
            onVoucher={() => setVoucher(!voucher)}
            action={
              <Button variant="rose" onClick={() => go("checkout")}>
                Tiến hành đặt hàng ({money(finalTotal)}) <Icon name="arrow" />
              </Button>
            }
          />
        </div>
      )}
    </main>
  );
}

function OrderSummary({
  subtotal,
  shippingFee = 25000,
  giftWrapFee = 0,
  voucher,
  onVoucher,
  action,
}: {
  subtotal: number;
  shippingFee?: number;
  giftWrapFee?: number;
  voucher: boolean;
  onVoucher: () => void;
  action: ReactNode;
}) {
  const actualShipping = subtotal > 0 ? shippingFee : 0;
  const discount = voucher ? 50000 : 0;
  const total = Math.max(0, subtotal + actualShipping + giftWrapFee - discount);

  return (
    <aside className="order-summary">
      <h2>Tóm tắt đơn hàng</h2>
      <div className="voucher">
        <input placeholder="Mã voucher" />
        <button onClick={onVoucher}>
          {voucher ? "Đã áp dụng" : "Áp dụng"}
        </button>
      </div>
      {voucher && (
        <div className="voucher-ok">
          <Icon name="check" size={14} /> GIFTFINDER50 — Giảm 50.000đ
        </div>
      )}
      <dl>
        <div>
          <dt>Tạm tính</dt>
          <dd>{money(subtotal)}</dd>
        </div>
        <div>
          <dt>Phí vận chuyển</dt>
          <dd>{money(actualShipping)}</dd>
        </div>
        {giftWrapFee > 0 && (
          <div>
            <dt>Gói quà cao cấp</dt>
            <dd>+{money(giftWrapFee)}</dd>
          </div>
        )}
        <div className="discount-row">
          <dt>Giảm giá voucher</dt>
          <dd>-{money(discount)}</dd>
        </div>
      </dl>
      <div className="order-total">
        <span>
          Tổng cộng<small>Đã bao gồm VAT</small>
        </span>
        <b>{money(total)}</b>
      </div>
      {action}
      <p>
        <Icon name="shield" size={14} /> Thông tin thanh toán được mã hóa an
        toàn
      </p>
    </aside>
  );
}

function Checkout({ go }: { go: (p: Page) => void }) {
  const { items, subtotal, shippingFee, giftWrapFee, hasGiftWrap, clearCart } =
    useCart();
  const [payment, setPayment] = useState("vnpay");
  const [voucher, setVoucher] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  if (items.length === 0) {
    return (
      <main className="shell flow-page">
        <div
          className="empty-state"
          style={{ padding: "60px 20px", textAlign: "center" }}
        >
          <div className="empty-gift" style={{ margin: "0 auto 16px" }}>
            <Icon name="cart" size={38} />
          </div>
          <h2
            style={{ fontSize: "20px", fontWeight: 700, marginBottom: "8px" }}
          >
            Chưa có sản phẩm để thanh toán
          </h2>
          <p
            style={{
              color: "var(--muted)",
              maxWidth: "420px",
              margin: "0 auto 24px",
              lineHeight: 1.6,
            }}
          >
            Giỏ hàng của bạn đang trống. Vui lòng chọn món quà yêu thích trước
            khi tiến hành thanh toán nhé!
          </p>
          <Button variant="rose" onClick={() => go("catalog")}>
            Khám phá quà tặng ngay
          </Button>
        </div>
      </main>
    );
  }

  const handleConfirmOrder = () => {
    setSubmitting(true);
    setTimeout(() => {
      clearCart();
      setSubmitting(false);
      go("success");
    }, 600);
  };

  return (
    <main className="shell flow-page">
      <div className="checkout-top">
        <button onClick={() => go("cart")}>← Quay lại giỏ hàng</button>
        <div className="checkout-steps">
          <span className="done">1</span>
          <i />
          <span className="active">2</span>
          <i />
          <span>3</span>
        </div>
        <small>Giỏ hàng　 Thanh toán　 Hoàn tất</small>
      </div>
      <div className="checkout-layout">
        <section className="checkout-form">
          <div className="form-section">
            <div className="form-title">
              <i>1</i>
              <div>
                <h2>Thông tin người nhận</h2>
                <p>Chúng tôi sẽ liên hệ số điện thoại này khi giao quà.</p>
              </div>
            </div>
            <div className="form-grid">
              <label>
                Họ và tên
                <input placeholder="Nguyễn Minh Anh" />
              </label>
              <label>
                Số điện thoại
                <input placeholder="09xx xxx xxx" />
              </label>
              <label className="full">
                Địa chỉ chi tiết
                <input placeholder="Số nhà, tên đường, phường/xã..." />
              </label>
              <label>
                Tỉnh / Thành phố
                <select>
                  <option>TP. Hồ Chí Minh</option>
                  <option>Hà Nội</option>
                  <option>Đà Nẵng</option>
                </select>
              </label>
              <label>
                Quận / Huyện
                <select>
                  <option>Quận 1</option>
                  <option>Quận 3</option>
                  <option>Quận Cầu Giấy</option>
                </select>
              </label>
              <label className="full">
                Ghi chú giao hàng
                <textarea placeholder="VD: Gọi trước khi giao, không ghi giá trên gói hàng..." />
              </label>
            </div>
          </div>
          <div className="form-section">
            <div className="form-title">
              <i>2</i>
              <div>
                <h2>Phương thức thanh toán</h2>
                <p>Chọn phương thức phù hợp nhất với bạn.</p>
              </div>
            </div>
            <label
              className={`payment-option ${
                payment === "vnpay" ? "selected" : ""
              }`}
            >
              <input
                type="radio"
                checked={payment === "vnpay"}
                onChange={() => setPayment("vnpay")}
              />
              <span className="radio" />
              <div className="vnpay-logo">
                <b>VN</b>PAY
              </div>
              <div>
                <b>Cổng thanh toán Quốc gia VNPAY</b>
                <small>
                  Quét mã QR, thẻ ATM 30+ ngân hàng hoặc thẻ quốc tế
                </small>
                <p>
                  <Badge>VNPAY-QR</Badge>
                  <Badge>NAPAS</Badge>
                  <Badge>VISA</Badge>
                  <Badge>Mastercard</Badge>
                </p>
              </div>
              <Badge tone="green">Khuyên dùng</Badge>
            </label>
            <label
              className={`payment-option ${
                payment === "cod" ? "selected" : ""
              }`}
            >
              <input
                type="radio"
                checked={payment === "cod"}
                onChange={() => setPayment("cod")}
              />
              <span className="radio" />
              <i className="cod-icon">₫</i>
              <div>
                <b>Thanh toán khi nhận hàng (COD)</b>
                <small>Thanh toán tiền mặt cho nhân viên giao hàng.</small>
              </div>
            </label>
          </div>
          <div className="form-section review-order">
            <div className="form-title">
              <i>3</i>
              <div>
                <h2>Xem lại đơn hàng ({items.length} món)</h2>
                <p>Kiểm tra món quà lần cuối trước khi thanh toán.</p>
              </div>
            </div>
            {items.map((item) => (
              <article key={item.id} style={{ marginBottom: 12 }}>
                <img src={item.image} alt={item.name} />
                <div>
                  <b>{item.name}</b>
                  <small>
                    {item.variant} · Số lượng: {item.quantity}
                  </small>
                  {hasGiftWrap && <Badge tone="rose">Có gói quà & thiệp</Badge>}
                </div>
                <strong>{money(item.price * item.quantity)}</strong>
              </article>
            ))}
          </div>
        </section>
        <OrderSummary
          subtotal={subtotal}
          shippingFee={shippingFee}
          giftWrapFee={giftWrapFee}
          voucher={voucher}
          onVoucher={() => setVoucher(!voucher)}
          action={
            <Button
              variant="rose"
              onClick={handleConfirmOrder}
              disabled={submitting}
            >
              {submitting
                ? "Đang xử lý đơn hàng..."
                : payment === "vnpay"
                  ? "Xác nhận & Thanh toán qua VNPAY"
                  : "Xác nhận đặt hàng"}{" "}
              <Icon name="arrow" />
            </Button>
          }
        />
      </div>
    </main>
  );
}

function Success({ go }: { go: (p: Page) => void }) {
  return (
    <main className="success-page">
      <div className="success-card">
        <div className="success-icon">
          <Icon name="check" size={42} />
          <i />
          <i />
        </div>
        <Badge tone="green">THANH TOÁN THÀNH CÔNG</Badge>
        <h1>Cảm ơn bạn!</h1>
        <p>
          Đơn hàng <b>#GF-8892</b> đã được thanh toán thành công qua VNPAY.
          <br />
          Một món quà tuyệt vời đang được chuẩn bị.
        </p>
        <div className="timeline">
          <div className="done">
            <i>
              <Icon name="check" size={14} />
            </i>
            <b>Đã thanh toán</b>
            <small>Hôm nay, 10:32</small>
          </div>
          <span />
          <div className="active">
            <i>2</i>
            <b>Đang chuẩn bị quà</b>
            <small>Dự kiến 2–4 giờ</small>
          </div>
          <span />
          <div>
            <i>3</i>
            <b>Đang giao hàng</b>
            <small>Dự kiến 20/10</small>
          </div>
        </div>
        <div className="receipt">
          <div>
            <span>Mã đơn hàng</span>
            <b>#GF-8892</b>
          </div>
          <div>
            <span>Người nhận</span>
            <b>Nguyễn Minh Anh · 09xx xxx xxx</b>
          </div>
          <div>
            <span>Giao đến</span>
            <b>Quận 1, TP. Hồ Chí Minh</b>
          </div>
          <div>
            <span>Lời nhắn trên thiệp</span>
            <b>“Chúc em luôn rạng rỡ như những đóa hoa...”</b>
          </div>
          <div>
            <span>Thời gian giao dự kiến</span>
            <b className="green">Trước 18:00, ngày 20/10</b>
          </div>
          <div className="receipt-total">
            <span>Tổng thanh toán qua VNPAY</span>
            <b>325.000đ</b>
          </div>
        </div>
        <div className="success-actions">
          <Button variant="secondary">Xem đơn hàng của tôi</Button>
          <Button variant="rose" onClick={() => go("home")}>
            Tiếp tục mua sắm <Icon name="arrow" />
          </Button>
        </div>
        <small className="email-note">
          Biên nhận đã được gửi đến email của bạn.
        </small>
      </div>
    </main>
  );
}

function AffiliateModal({
  close,
  product,
  targetUrl,
}: {
  close: () => void;
  product?: ProductItem | null;
  targetUrl?: string;
}) {
  const [seconds, setSeconds] = useState(1);
  const platformName =
    product?.platform === "TIKTOK" ? "TikTok Shop" : "Shopee Việt Nam";
  const finalUrl =
    targetUrl ||
    product?.affiliateUrl ||
    product?.originalUrl ||
    "https://shopee.vn";

  useEffect(() => {
    const timer = setInterval(() => {
      setSeconds((s) => {
        if (s <= 1) {
          clearInterval(timer);
          close();
          return 0;
        }
        return s - 1;
      });
    }, 1000);
    return () => clearInterval(timer);
  }, [close]);

  return (
    <div className="modal-backdrop" onMouseDown={close}>
      <div className="affiliate-modal" onMouseDown={(e) => e.stopPropagation()}>
        <button className="modal-close" onClick={close}>
          <Icon name="close" />
        </button>
        <div className="flying-gift">
          <div>🎁</div>
          <i />
          <span>🛍️</span>
        </div>
        <Badge tone="rose">ĐỐI TÁC CHÍNH HÃNG</Badge>
        <h2>Đang chuyển bạn đến {platformName}...</h2>
        <p>
          GiftFinder sẽ mở gian hàng chính hãng và ghi nhận ưu đãi dành riêng
          cho bạn.
        </p>
        <div className="coupon">
          <Icon name="check" />
          <span>
            <small>ĐÃ ÁP DỤNG MÃ ĐỘC QUYỀN</small>
            <b>GIFTFINDER10</b>
          </span>
          <Badge tone="green">-10%</Badge>
        </div>
        <div className="redirect-progress">
          <i style={{ width: `${((3 - seconds) / 3) * 100}%` }} />
        </div>
        <span className="redirect-text">
          Tự động chuyển sau <b>{seconds}s</b>...
        </span>
        <Button
          variant="rose"
          onClick={() => {
            window.open(finalUrl, "_blank");
            close();
          }}
        >
          Bấm vào đây nếu không tự chuyển trang <Icon name="arrow" />
        </Button>
        <small>
          <Icon name="shield" size={13} /> Liên kết an toàn · Affiliate tracking
          minh bạch
        </small>
      </div>
    </div>
  );
}

function Footer() {
  return (
    <footer>
      {/* 1. KẾT NỐI VỚI CHÚNG TÔI */}
      <div className="shell">
        <div className="footer-connect">
          <span className="footer-connect-title">Kết nối với chúng tôi</span>
          <div className="footer-social-links">
            <a
              href="#facebook"
              onClick={(e) => e.preventDefault()}
              aria-label="Facebook"
              className="social-btn"
              title="Theo dõi GiftFinder trên Facebook"
            >
              <svg
                width="15"
                height="15"
                viewBox="0 0 24 24"
                fill="currentColor"
              >
                <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
              </svg>
            </a>
            <a
              href="#youtube"
              onClick={(e) => e.preventDefault()}
              aria-label="YouTube"
              className="social-btn"
              title="Kênh YouTube GiftFinder"
            >
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="currentColor"
              >
                <path d="M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z" />
              </svg>
            </a>
            <a
              href="#tiktok"
              onClick={(e) => e.preventDefault()}
              aria-label="TikTok"
              className="social-btn"
              title="TikTok GiftFinder"
            >
              <svg
                width="15"
                height="15"
                viewBox="0 0 24 24"
                fill="currentColor"
              >
                <path d="M19.59 6.69a4.83 4.83 0 0 1-3.77-4.25V2h-3.45v13.67a2.89 2.89 0 0 1-5.2 1.74 2.89 2.89 0 0 1 2.31-4.64c.29 0 .58.04.86.12V9.32a6.34 6.34 0 0 0-1-.08 6.34 6.34 0 0 0-6.34 6.34 6.34 6.34 0 0 0 6.34 6.34 6.34 6.34 0 0 0 6.34-6.34V9.89a8.28 8.28 0 0 0 4.91 1.6V8.04a4.85 4.85 0 0 1-1-.05z" />
              </svg>
            </a>
          </div>
        </div>
      </div>

      {/* 2. KHỐI THÔNG TIN CHÍNH (4 CỘT) */}
      <div className="footer-main shell">
        {/* CỘT 1: THÔNG TIN DOANH NGHIỆP & PHÁP LÝ */}
        <div className="footer-company">
          <h3 className="footer-company-name">GIFTFINDER</h3>
          <h4 className="footer-company-legal">
            CÔNG TY TNHH GIẢI PHÁP QUÀ TẶNG TRƯỜNG THÀNH GIFTFINDER
          </h4>
          <p className="footer-company-desc">
            Đơn vị tiên phong cung cấp giải pháp quà tặng cá nhân hóa và quà
            tặng doanh nghiệp toàn diện từ thiết kế độc bản, chế tác thủ công
            tinh xảo đến đóng gói & trao gửi trọn vẹn.
          </p>

          <div className="footer-legal-list">
            <div className="legal-row">
              <span className="legal-icon">📄</span>
              <span>
                <b>ĐKKD / MST:</b> 0111161703 do Sở Kế hoạch và Đầu tư TP. Hà
                Nội cấp lần đầu ngày 10/02/2026.
              </span>
            </div>
            <div className="legal-row">
              <span className="legal-icon">👤</span>
              <span>
                <b>Đại diện pháp luật:</b> Ông Trần Đức Cường
              </span>
            </div>
            <div className="legal-row">
              <span className="legal-icon">📍</span>
              <span>
                <b>Địa Chỉ:</b> D02- L02, Khu A - Khu đô thị mới Dương Nội,
                Phường Dương Nội, Thành phố Hà Nội
              </span>
            </div>
            <div className="legal-row">
              <span className="legal-icon">📞</span>
              <span>
                <b>Hotline:</b> 0975941131
              </span>
            </div>
            <div className="legal-row">
              <span className="legal-icon">✉️</span>
              <span>
                <b>Email:</b> quatangtruongthanh@gmail.com
              </span>
            </div>
          </div>

          {/* HUY HIỆU ĐÃ THÔNG BÁO BỘ CÔNG THƯƠNG */}
          <div
            className="bct-badge"
            title="Đã thông báo với Bộ Công Thương theo quy định TMĐT"
          >
            <div className="bct-circle">
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="#0284c7"
                strokeWidth="3"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="20 6 9 17 4 12" />
              </svg>
            </div>
            <div className="bct-content">
              <span className="bct-top">ĐÃ THÔNG BÁO</span>
              <span className="bct-bottom">BỘ CÔNG THƯƠNG</span>
            </div>
          </div>
        </div>

        {/* CỘT 2: VỀ CHÚNG TÔI */}
        <div className="footer-col">
          <h4 className="footer-col-title">VỀ CHÚNG TÔI</h4>
          <ul className="footer-menu">
            {[
              "Giới thiệu",
              "Sản phẩm",
              "Khách hàng doanh nghiệp",
              "Đồng hành quảng bá",
            ].map((item) => (
              <li key={item}>
                <a href={`#${item}`} onClick={(e) => e.preventDefault()}>
                  {item}
                </a>
              </li>
            ))}
          </ul>
        </div>

        {/* CỘT 3: HƯỚNG DẪN */}
        <div className="footer-col">
          <h4 className="footer-col-title">HƯỚNG DẪN</h4>
          <ul className="footer-menu">
            {[
              "Hình thức giao hàng",
              "Hình thức thanh toán",
              "Tra cứu đơn hàng",
            ].map((item) => (
              <li key={item}>
                <a href={`#${item}`} onClick={(e) => e.preventDefault()}>
                  {item}
                </a>
              </li>
            ))}
          </ul>
        </div>

        {/* CỘT 4: CHÍNH SÁCH */}
        <div className="footer-col">
          <h4 className="footer-col-title">CHÍNH SÁCH</h4>
          <ul className="footer-menu">
            {[
              "Chính sách bảo hành",
              "Chính sách bảo mật",
              "Chính sách giá",
              "Chính sách đổi trả",
              "Điều khoản dịch vụ",
            ].map((item) => (
              <li key={item}>
                <a href={`#${item}`} onClick={(e) => e.preventDefault()}>
                  {item}
                </a>
              </li>
            ))}
          </ul>
        </div>
      </div>

      {/* 3. ĐỐI TÁC THANH TOÁN & BẢN QUYỀN */}
      <div className="footer-bottom shell">
        <div>
          <span>Thanh toán an toàn</span>
          <b>
            VN<span>PAY</span>
          </b>
          <b>NAPAS</b>
          <b>VISA</b>
          <b>●● Mastercard</b>
        </div>
        <div>
          <span>Đối tác liên kết</span>
          <b className="shopee">Shopee</b>
          <b>TikTok Shop</b>
        </div>
        <small>© 2026 GiftFinder. Tất cả quyền được bảo lưu.</small>
      </div>
    </footer>
  );
}

export default function App() {
  const { isAuthenticated, isAdmin, isMerchant } = useAuth();

  const validPages: Page[] = [
    "home",
    "catalog",
    "product",
    "wishlist",
    "merchant",
    "admin",
    "auth",
    "cart",
    "checkout",
    "success",
  ];

  const [page, setPage] = useState<Page>(() => {
    if (typeof window !== "undefined") {
      const rawPath = window.location.pathname.replace(/^\/+/, "");
      if (rawPath.startsWith("product/")) {
        return "product";
      }
      const cleanPath = rawPath as Page;
      if (cleanPath && validPages.includes(cleanPath)) {
        return cleanPath;
      }
    }
    return "home";
  });

  const [affiliate, setAffiliate] = useState(false);
  const [affiliateProduct, setAffiliateProduct] = useState<ProductItem | null>(
    null,
  );
  const [affiliateUrl, setAffiliateUrl] = useState<string>("");
  const [toastMsg, setToastMsg] = useState<string | null>(null);

  // Tự động tắt mọi thông báo toast sau 3.5 giây (chuẩn trải nghiệm 3s - 4s)
  useEffect(() => {
    if (!toastMsg) return;
    const timer = setTimeout(() => {
      setToastMsg(null);
    }, 3500);
    return () => clearTimeout(timer);
  }, [toastMsg]);
  const [selectedProduct, setSelectedProduct] = useState<ProductItem | null>(
    null,
  );
  const [productsList, setProductsList] =
    useState<ProductItem[]>(defaultProducts);
  const [_loadingProducts, setLoadingProducts] = useState(false);
  const [catalogFilter, setCatalogFilter] = useState<{
    title: string;
    slug: string;
    parentCategory: string;
    keyword?: string;
    minPrice?: number;
    maxPrice?: number;
  }>({
    title: "Tất cả quà tặng tuyển chọn",
    slug: "tat-ca",
    parentCategory: "Danh mục quà tặng",
    keyword: "",
    minPrice: undefined,
    maxPrice: undefined,
  });

  const saveCurrentScroll = () => {
    if (typeof window !== "undefined") {
      const currentState = window.history.state || {};
      window.history.replaceState(
        { ...currentState, scrollY: window.scrollY },
        "",
        window.location.pathname,
      );
    }
  };

  const handleSelectCategory = (
    title: string,
    slug: string = "tat-ca",
    parentCategory: string = "Danh mục quà tặng",
    keyword: string = "",
    minPrice?: number,
    maxPrice?: number,
  ) => {
    setCatalogFilter({
      title,
      slug,
      parentCategory,
      keyword,
      minPrice,
      maxPrice,
    });
    if (page !== "catalog") {
      saveCurrentScroll();
      setPage("catalog");
      window.scrollTo(0, 0);
      if (
        typeof window !== "undefined" &&
        window.location.pathname !== "/catalog"
      ) {
        window.history.pushState({ page: "catalog" }, "", "/catalog");
      }
    } else {
      if (
        typeof window !== "undefined" &&
        window.location.pathname !== "/catalog"
      ) {
        window.history.replaceState({ page: "catalog" }, "", "/catalog");
      }
    }
  };

  // Role-based active page guards during render to prevent unauthorized page mounts
  useEffect(() => {
    if (page === "admin" && !isAdmin) {
      if (!isAuthenticated) {
        setToastMsg("⚠️ Vui lòng đăng nhập tài khoản Quản trị viên.");
        setPage("auth");
      } else {
        setToastMsg("⛔ Bạn không có quyền truy cập trang Quản trị viên.");
        setPage("home");
      }
    }
  }, [page, isAuthenticated, isAdmin, isMerchant]);

  const handleOpenProduct = (p: ProductItem) => {
    saveCurrentScroll();
    setSelectedProduct(p);
    setPage("product");
    window.scrollTo(0, 0);
    const targetPath = p.id ? `/product/${p.id}` : "/product";
    if (
      typeof window !== "undefined" &&
      window.location.pathname !== targetPath
    ) {
      window.history.pushState(
        { page: "product", productId: p.id },
        "",
        targetPath,
      );
    }
  };

  const go = (next: Page) => {
    if (next === "admin" && !isAdmin) {
      if (!isAuthenticated) {
        setToastMsg("⚠️ Vui lòng đăng nhập tài khoản Quản trị viên.");
        setPage("auth");
        window.history.pushState({ page: "auth" }, "", "/auth");
        return;
      } else {
        setToastMsg(
          "⛔ Bạn không có quyền truy cập trang Quản trị viên (Chỉ dành cho Admin).",
        );
        return;
      }
    }

    if (next === "product") {
      const currentPath =
        typeof window !== "undefined" ? window.location.pathname : "";
      if (currentPath.startsWith("/product")) {
        setPage("product");
        window.scrollTo(0, 0);
        return;
      }
    }

    saveCurrentScroll();

    setPage(next);
    window.scrollTo(0, 0);
    const path =
      next === "home"
        ? "/"
        : next === "product" && selectedProduct?.id
          ? `/product/${selectedProduct.id}`
          : `/${next}`;
    if (typeof window !== "undefined" && window.location.pathname !== path) {
      window.history.pushState(
        { page: next, productId: selectedProduct?.id },
        "",
        path,
      );
    }
  };

  // Hỗ trợ mở trực tiếp URL /product/{id} khi F5 hoặc chia sẻ link
  useEffect(() => {
    if (typeof window === "undefined") return;
    const match = window.location.pathname.match(
      /^\/product\/([0-9a-fA-F-]{20,})/,
    );
    if (match && match[1]) {
      const pid = match[1];
      if (!selectedProduct || selectedProduct.id !== pid) {
        productService
          .getProductById(pid)
          .then((dto) => {
            setSelectedProduct(mapDtoToProductItem(dto));
          })
          .catch(() => {});
      }
    }
  }, []);

  // Tự động dọn sạch toàn bộ localStorage & sessionStorage 1 lần duy nhất theo yêu cầu
  useEffect(() => {
    try {
      if (!localStorage.getItem("giftfinder_clean_storage_v1")) {
        localStorage.clear();
        sessionStorage.clear();
        localStorage.setItem("giftfinder_clean_storage_v1", "done");
      }
    } catch {}
  }, []);

  // Quản lý nút Quay lại (Back) và Tiếp tục (Forward) trên trình duyệt Edge / Chrome
  useEffect(() => {
    if (
      typeof window !== "undefined" &&
      "scrollRestoration" in window.history
    ) {
      window.history.scrollRestoration = "manual";
    }

    const handlePopState = (e: PopStateEvent) => {
      let targetPage: Page = "home";
      const rawPath = window.location.pathname.replace(/^\/+/, "");

      if (e.state?.page && validPages.includes(e.state.page)) {
        targetPage = e.state.page;
      } else if (rawPath.startsWith("product/")) {
        targetPage = "product";
      } else {
        const cleanPath = rawPath as Page;
        targetPage = validPages.includes(cleanPath) ? cleanPath : "home";
      }

      setPage(targetPage);

      // Nếu quay lại trang product, đồng bộ lại sản phẩm theo productId
      if (targetPage === "product") {
        const match = window.location.pathname.match(
          /^\/product\/([0-9a-fA-F-]{20,})/,
        );
        const targetId = e.state?.productId || (match ? match[1] : null);
        if (targetId) {
          const found = productsList.find((p) => p.id === targetId);
          if (found) {
            setSelectedProduct(found);
          } else {
            productService
              .getProductById(targetId)
              .then((dto) => setSelectedProduct(mapDtoToProductItem(dto)))
              .catch(() => {});
          }
        }
      }

      // Khôi phục chính xác vị trí cuộn trang trước đó (Scroll Restoration chuẩn sàn TMĐT)
      const targetScrollY = e.state?.scrollY;
      if (typeof targetScrollY === "number" && targetScrollY > 0) {
        requestAnimationFrame(() => {
          window.scrollTo({ top: targetScrollY, behavior: "instant" });
          setTimeout(() => {
            window.scrollTo({ top: targetScrollY, behavior: "instant" });
          }, 40);
          setTimeout(() => {
            window.scrollTo({ top: targetScrollY, behavior: "instant" });
          }, 120);
        });
      } else {
        window.scrollTo({ top: 0, behavior: "instant" });
      }
    };

    // Đánh dấu state ban đầu để khi bấm Back không bao giờ bị thoát hẳn ra khỏi Edge
    const initialPath = window.location.pathname.replace(/^\/+/, "") || "home";
    if (!window.history.state) {
      window.history.replaceState(
        { page: initialPath, scrollY: 0 },
        "",
        window.location.pathname,
      );
    }

    window.addEventListener("popstate", handlePopState);
    return () => window.removeEventListener("popstate", handlePopState);
  }, [productsList]);

  // Tải 244 sản phẩm thật từ Database PostgreSQL qua Backend API (cân bằng cả Shopee và TikTok Shop)
  useEffect(() => {
    let isCancelled = false;
    setLoadingProducts(true);

    Promise.all([
      productService.getProducts({ source: 0, pageSize: 120 }), // 116 sản phẩm Shopee trong DB
      productService.getProducts({ source: 1, pageSize: 120 }), // 124 sản phẩm TikTok Shop trong DB
    ])
      .then(([shopeeRes, tiktokRes]) => {
        if (isCancelled) return;

        const shopeeItems = (shopeeRes?.items || []).map(mapDtoToProductItem);
        const tiktokItems = (tiktokRes?.items || []).map(mapDtoToProductItem);

        // Trộn xen kẽ đều đặn: 1 sản phẩm Shopee, 1 sản phẩm TikTok Shop
        const mixed: ProductItem[] = [];
        const maxLen = Math.max(shopeeItems.length, tiktokItems.length);
        for (let i = 0; i < maxLen; i++) {
          if (shopeeItems[i]) mixed.push(shopeeItems[i]);
          if (tiktokItems[i]) mixed.push(tiktokItems[i]);
        }

        if (mixed.length > 0) {
          setProductsList(mixed);
        }
      })
      .catch((err) => {
        console.warn("API /Products fallback to preset catalog:", err);
      })
      .finally(() => {
        if (!isCancelled) setLoadingProducts(false);
      });

    return () => {
      isCancelled = true;
    };
  }, []);

  const handleOpenAffiliate = (item?: ProductItem) => {
    const target = item || productsList[0] || defaultProducts[0];
    const p = (target.platform || "").toUpperCase();

    let platformName = "Shopee";
    let defaultStoreUrl = "https://shopee.vn";

    if (p.includes("TIKTOK")) {
      platformName = "TikTok Shop";
      defaultStoreUrl = "https://shop.tiktok.com";
    } else if (p.includes("LAZADA")) {
      platformName = "Lazada";
      defaultStoreUrl = "https://www.lazada.vn";
    } else if (p.includes("TIKI")) {
      platformName = "Tiki";
      defaultStoreUrl = "https://tiki.vn";
    } else if (p.includes("SHOPEE")) {
      platformName = "Shopee";
      defaultStoreUrl = "https://shopee.vn";
    } else {
      platformName = "đối tác";
    }

    const targetUrl =
      target.affiliateUrl || target.originalUrl || defaultStoreUrl;

    // Mở trực tiếp sang sàn Shopee / TikTok Shop / Lazada trong tab mới ngay khi click
    try {
      window.open(targetUrl, "_blank", "noopener,noreferrer");
    } catch (e) {
      console.warn("Popup error:", e);
    }

    // Ghi nhận click tracking ngầm trong backend
    if (target.id) {
      productService.trackAffiliateClick(target.id).catch(console.warn);
    }

    setAffiliateProduct(target);
    setAffiliateUrl(targetUrl);
    setAffiliate(false);
    setToastMsg(`Đã mở gian hàng ${platformName} trong tab mới!`);
    setTimeout(() => setToastMsg(null), 3000);
  };

  return (
    <>
      {page !== "auth" && (
        <Header
          go={go}
          currentPage={page}
          onSelectCategory={handleSelectCategory}
        />
      )}
      {page === "home" && (
        <Home
          go={go}
          products={productsList}
          openAffiliate={handleOpenAffiliate}
          onSelectCategory={handleSelectCategory}
          onSelectProduct={handleOpenProduct}
        />
      )}{" "}
      {page === "catalog" && (
        <Catalog
          go={go}
          products={productsList}
          openAffiliate={handleOpenAffiliate}
          catalogFilter={catalogFilter}
          onSelectCategory={handleSelectCategory}
          onSelectProduct={handleOpenProduct}
        />
      )}{" "}
      {page === "product" && (
        <ProductDetail
          go={go}
          product={selectedProduct}
          products={productsList}
          openAffiliate={handleOpenAffiliate}
          catalogFilter={catalogFilter}
          onNotify={(msg) => {
            setToastMsg(msg);
            setTimeout(() => setToastMsg(null), 3500);
          }}
          onSelectProduct={handleOpenProduct}
        />
      )}{" "}
      {page === "wishlist" && (
        <Wishlist
          go={go}
          products={productsList}
          openAffiliate={handleOpenAffiliate}
          onSelectProduct={handleOpenProduct}
        />
      )}{" "}
      {page === "merchant" && (
        <MerchantPortal go={go} onNotify={(msg) => setToastMsg(msg)} />
      )}
      {page === "admin" && <AdminConsole go={go} />}
      {page === "auth" && <AuthPage go={go} />}{" "}
      {page === "cart" && <Cart go={go} />}{" "}
      {page === "checkout" && <Checkout go={go} />}{" "}
      {page === "success" && <Success go={go} />}{" "}
      {affiliate && (
        <AffiliateModal
          close={() => {
            setAffiliate(false);
            setAffiliateUrl("");
            setAffiliateProduct(null);
          }}
          product={affiliateProduct}
          targetUrl={affiliateUrl}
        />
      )}
      {toastMsg && (
        <div className="app-toast-alert" role="alert">
          <span>{toastMsg}</span>
          <button
            type="button"
            className="toast-close-btn"
            onClick={() => setToastMsg(null)}
            title="Đóng thông báo"
            aria-label="Đóng thông báo"
          >
            ✕
          </button>
        </div>
      )}
      {page !== "auth" && (
        <AiChatbotWidget
          go={go}
          products={productsList}
          openAffiliate={handleOpenAffiliate}
          onSelectProduct={handleOpenProduct}
        />
      )}
    </>
  );
}
