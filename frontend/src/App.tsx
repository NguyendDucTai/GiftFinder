import { useEffect, useState, type ReactNode } from "react"
import { productService, type ProductDto } from "./features/products"
import { useAuth } from "./features/auth"
import { useWishlist } from "./features/wishlist"
import { useCart } from "./features/cart"
import { recommendationService } from "./features/recommendations"
import { adminService } from "./features/admin/services/adminService"
import {
  merchantService,
  type MerchantDashboardStats,
  type MerchantProductItem,
} from "./features/merchants/services/merchantService"
import heroPink from "./assets/hero-pink.png"
import heroLuxury from "./assets/hero-luxury.png"
import heroTea from "./assets/hero-tea.png"

export interface ProductItem {
  id?: string
  name: string
  price: number
  old: number
  rating: string
  sold: string
  image: string
  platform: "SHOPEE" | "TIKTOK" | "DIRECT" | string
  discount: string
  originalUrl?: string
  affiliateUrl?: string
}

function mapDtoToProductItem(dto: ProductDto): ProductItem {
  const origPrice =
    dto.originalPrice > dto.price ? dto.originalPrice : Math.round(dto.price * 1.25)
  const discountPercent = Math.max(5, Math.round((1 - dto.price / origPrice) * 100))

  let platform = "SHOPEE"
  const src = (dto.source || "").toLowerCase()
  if (src.includes("tiktok")) {
    platform = "TIKTOK"
  } else if (src.includes("direct")) {
    platform = "DIRECT"
  } else if (src.includes("shopee")) {
    platform = "SHOPEE"
  }

  const reviewsCount = dto.totalReviews || 0
  const soldStr =
    reviewsCount > 1000
      ? `${(reviewsCount / 1000).toFixed(1)}k`
      : reviewsCount > 0
        ? `${reviewsCount}`
        : "1.2k"

  return {
    id: dto.id,
    name: dto.name,
    price: dto.price,
    old: origPrice,
    rating: dto.rating > 0 ? dto.rating.toFixed(1) : "4.9",
    sold: soldStr,
    image: dto.imageUrl || images.candle,
    platform: platform,
    discount: `-${discountPercent}%`,
    originalUrl: dto.originalUrl,
    affiliateUrl: dto.affiliateUrl,
  }
}

type Page = "home" | "catalog" | "product" | "wishlist" | "merchant" | "auth" | "cart" | "checkout" | "success" | "admin"
type IconName = "search" | "sparkle" | "heart" | "cart" | "user" | "chevron" | "chevron-down" | "arrow" | "arrow-left" | "arrow-right" | "bell" | "check" | "trash" | "minus" | "plus" | "menu" | "close" | "play" | "shield" | "logout" | "clock" | "eye" | "eye-off" | "external" | "gift" | "store" | "tag" | "trend" | "users" | "refresh"

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
}

function Icon({ name, size = 19 }: { name: IconName; size?: number }) {
  return (
    <svg
      aria-hidden="true"
      width={size}
      height={size}
      viewBox="0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      {iconPaths[name]}
    </svg>
  )
}

const images = {
  hero: heroPink,
  heroPink,
  heroLuxury,
  heroTea,
  giftbox: heroLuxury,
  rose: heroPink,
  candle: heroTea,
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
}

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
]

const money = (value: number) => `${value.toLocaleString("vi-VN")}đ`

function Logo({ onClick }: { onClick?: () => void }) {
  return (
    <button className="brand" onClick={onClick}>
      <span>🎁</span>
      <b>GiftFinder</b>
    </button>
  )
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
  children: ReactNode
  variant?: "primary" | "secondary" | "rose" | "dark" | "danger" | "success" | "ghost" | "indigo"
  icon?: IconName
  className?: string
  onClick?: () => void
  type?: "button" | "submit" | "reset"
  disabled?: boolean
  style?: React.CSSProperties
}) {
  const variantClass = variant === "rose" ? "button-primary" : `button-${variant}`;
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
  )
}

function Badge({
  children,
  tone = "neutral",
  className = "",
}: {
  children: ReactNode
  tone?: string
  className?: string
}) {
  const toneClass = tone.startsWith("badge-") ? tone : `badge-${tone}`;
  return <span className={`badge ${toneClass} ${className}`.trim()}>{children}</span>
}

function Header({
  go,
}: {
  go: (page: Page) => void
  cartCount?: number
}) {
  const { user, isAuthenticated, isAdmin, isMerchant, logout } = useAuth()
  const { wishlistCount } = useWishlist()
  const { cartCount } = useCart()
  const [menu, setMenu] = useState(false)
  const [mega, setMega] = useState(false)
  const [userDropdown, setUserDropdown] = useState(false)
  return (
    <>
      <div className="promo">
        🎉 Mừng ngày 20/10 — Nhận voucher giảm 20% khi mua quà qua VNPAY{" "}
        <span>|</span> Miễn phí gói quà cao cấp
      </div>
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
            <input placeholder="Tìm quà bằng AI: người nhận, dịp, ngân sách..." />
            <button onClick={() => go("catalog")}>
              <Icon name="search" />
            </button>
          </label>
          <button className="merchant-link" onClick={() => go("merchant")}>
            Bán hàng cùng GiftFinder
          </button>
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
                          go("admin")
                          setUserDropdown(false)
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
                          go("merchant")
                          setUserDropdown(false)
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
                    <button
                      onClick={() => {
                        go("wishlist")
                        setUserDropdown(false)
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
                        logout()
                        setUserDropdown(false)
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
        <div className={`category-nav ${menu ? "open" : ""}`}>
          <button className="all-categories" onClick={() => setMega(!mega)}>
            <Icon name="menu" size={16} /> Danh mục quà tặng
          </button>
          {[
            "Mỹ phẩm",
            "Nến thơm",
            "Thời trang",
            "Đồ công nghệ",
            "Set quà thủ công",
          ].map((x) => (
            <button key={x} onClick={() => go("catalog")}>
              {x}
            </button>
          ))}
          <span />
          <button onClick={() => go("catalog")}>Quà theo cung hoàng đạo</button>
          <button className="hot">Flash Sale</button>
          <div className="mobile-nav-links">
            <button onClick={() => go("wishlist")}>
              ♡ Quà đã yêu thích {wishlistCount > 0 ? `(${wishlistCount})` : ""}
            </button>
            <button onClick={() => go("merchant")}>
              Bán hàng cùng GiftFinder
            </button>
            {isAdmin && (
              <button onClick={() => go("admin")}>🛡️ Quản trị Admin</button>
            )}
            {!isAuthenticated ? (
              <button onClick={() => go("auth")}>Đăng nhập / Đăng ký</button>
            ) : (
              <button
                onClick={() => logout()}
                style={{ color: "var(--rose)" }}
              >
                Đăng xuất ({user?.fullName})
              </button>
            )}
          </div>
        </div>
        {mega && (
          <div className="mega-menu">
            <div className="mega-sidebar">
              <span>KHÁM PHÁ DANH MỤC</span>
              {[
                "Quà tặng phổ biến",
                "Cho nàng",
                "Cho chàng",
                "Quà bố mẹ",
                "Quà doanh nghiệp",
              ].map((item, index) => (
                <button
                  className={index === 0 ? "active" : ""}
                  key={item}
                  onClick={() => {
                    go("catalog")
                    setMega(false)
                  }}
                >
                  {item} <Icon name="chevron" size={14} />
                </button>
              ))}
            </div>
            <div className="mega-content">
              <div>
                <b>Theo loại quà</b>
                {[
                  "Nến thơm & tinh dầu",
                  "Mỹ phẩm & chăm sóc",
                  "Thời trang & phụ kiện",
                  "Đồ công nghệ",
                  "Set quà thủ công",
                ].map((item) => (
                  <button key={item} onClick={() => go("catalog")}>
                    {item}
                  </button>
                ))}
              </div>
              <div>
                <b>Theo dịp</b>
                {[
                  "Sinh nhật",
                  "Kỷ niệm ngày yêu",
                  "20/10 & 8/3",
                  "Giáng sinh",
                  "Tân gia",
                ].map((item) => (
                  <button key={item} onClick={() => go("catalog")}>
                    {item}
                  </button>
                ))}
              </div>
              <button className="mega-feature" onClick={() => go("catalog")}>
                <img src={images.rose} alt="" />
                <span>
                  <Badge tone="rose">BỘ SƯU TẬP MỚI</Badge>
                  <b>Quà cho nàng</b>
                  <small>Khám phá ngay →</small>
                </span>
              </button>
            </div>
          </div>
        )}
      </header>
    </>
  )
}

function SectionTitle({
  eyebrow,
  title,
  action,
}: {
  eyebrow?: string
  title: string
  action?: ReactNode
}) {
  return (
    <div className="section-title">
      <div>
        {eyebrow && <span>{eyebrow}</span>}
        <h2>{title}</h2>
      </div>
      {action}
    </div>
  )
}

function ProductCard({
  product,
  onOpen,
  onAffiliate,
  deal = false,
  sponsored = false,
}: {
  product: ProductItem
  onOpen: (p?: ProductItem) => void
  onAffiliate: () => void
  deal?: boolean
  sponsored?: boolean
}) {
  const { isWishlisted, toggleWishlist } = useWishlist()
  const liked = product.id ? isWishlisted(product.id) : isWishlisted(product.name)
  const [tracked, setTracked] = useState(false)

  return (
    <article className="product-card">
      <button className="product-photo" onClick={() => onOpen(product)}>
        <img src={product.image} alt={product.name} />
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
          className={`wish ${liked ? 'liked' : ''}`} 
          aria-label="Yêu thích"
          onClick={(e) => {
            e.stopPropagation()
            toggleWishlist(product)
          }}
        >
          <Icon name="heart" size={17} />
        </button>
      </button>
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
            style={tracked ? { borderColor: '#e11d48', color: '#e11d48', background: '#fff1f2' } : {}}
            onClick={() => setTracked(!tracked)}
            title={tracked ? "Đang theo dõi giảm giá" : "Bật theo dõi giá"}
          >
            <Icon name="bell" size={17} />
          </button>
          <Button
            variant="primary"
            onClick={() => {
              if (product.platform === "DIRECT") {
                onOpen(product)
              } else {
                onAffiliate()
              }
            }}
          >
            Mua ngay <Icon name="arrow" size={15} />
          </Button>
        </div>
      </div>
    </article>
  )
}

const addons = [
  { name: "Túi quà ruy băng ép nhũ", price: 15000, image: images.rose },
  { name: "Thiệp hoa khô viết tay", price: 20000, image: images.flower },
  { name: "Hộp nhạc gỗ quay tay", price: 85000, image: images.tray },
  { name: "Đèn Led đom đóm lung linh", price: 12000, image: images.candle },
  { name: "Bó hoa baby mini vĩnh cửu", price: 25000, image: images.blue },
]

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
]

function AiChatbotWidget({
  go,
  products = defaultProducts,
  openAffiliate,
  onSelectProduct,
}: {
  go: (p: Page) => void
  products?: ProductItem[]
  openAffiliate?: (p?: ProductItem) => void
  onSelectProduct?: (p: ProductItem) => void
}) {
  const [open, setOpen] = useState(false)
  const [input, setInput] = useState("")
  const [loading, setLoading] = useState(false)
  const [messages, setMessages] = useState<Array<{ sender: "bot" | "user"; text: string; product?: ProductItem }>>([
    {
      sender: "bot",
      text: "👋 Chào bạn! Mình là AI Gift Concierge. Hôm nay bạn muốn tìm quà cho ai, vào dịp nào và ngân sách khoảng bao nhiêu?",
    },
  ])

  const quickPrompts = [
    "🎁 Quà 20/10 cho bạn gái tầm 500k",
    "🎂 Sinh nhật đồng nghiệp nam 300k",
    "🏡 Quà tân gia nhà mới sang trọng",
  ]

  const handleSend = async (textToSend?: string) => {
    const q = (textToSend || input).trim()
    if (!q || loading) return

    const newMsgs = [...messages, { sender: "user" as const, text: q }]
    setMessages(newMsgs)
    if (!textToSend) setInput("")
    setLoading(true)

    try {
      const res = await recommendationService.getRecommendations(q, 6)
      let replyText = res.message || "Dựa trên yêu cầu của bạn, AI đã phân tích và gợi ý món quà phù hợp nhất từ hệ thống:"
      let matchedProduct: ProductItem | undefined = undefined

      if (res.items && res.items.length > 0) {
        const top = res.items[0]
        if (top.aiAdvice) {
          replyText = (res.interpretedIntent?.summary ? `🎯 ${res.interpretedIntent.summary}\n\n💡 ` : '') + top.aiAdvice
        } else if (res.interpretedIntent?.summary) {
          replyText = `🎯 ${res.interpretedIntent.summary}`
        }

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
        }
      } else {
        replyText = res.message || "AI chưa tìm thấy sản phẩm hoàn toàn khớp trong kho quà. Bạn hãy thử gợi ý mức ngân sách khác hoặc sở thích cụ thể hơn nhé!"
      }

      setMessages((prev) => [
        ...prev,
        {
          sender: "bot",
          text: replyText,
          product: matchedProduct,
        },
      ])
    } catch (err) {
      console.warn("AI recommendation API error, fallback:", err)
      let replyText = "Dựa trên yêu cầu của bạn, AI đã chọn lọc set quà được yêu thích nhất từ gian hàng chính hãng:"
      let matchedProduct = products[0] || defaultProducts[0]

      if (q.includes("nữ") || q.includes("gái") || q.includes("20/10") || q.includes("nàng")) {
        replyText = "Gợi ý ngọt ngào nhất cho nàng: Set quà nến thơm & hoa khô kèm thiệp viết tay cá nhân hóa:"
        matchedProduct = products[1] || products[0] || defaultProducts[1]
      } else if (q.includes("nam") || q.includes("chàng") || q.includes("công nghệ")) {
        replyText = "Gợi ý lịch lãm và thiết thực cho chàng:"
        matchedProduct = products[3] || products[0] || defaultProducts[3]
      } else if (q.includes("tân gia") || q.includes("nhà")) {
        replyText = "Gợi ý tinh tế mang lại bình an, vượng khí cho gia chủ:"
        matchedProduct = products[2] || products[0] || defaultProducts[2]
      }

      setMessages((prev) => [
        ...prev,
        {
          sender: "bot",
          text: replyText,
          product: matchedProduct,
        },
      ])
    } finally {
      setLoading(false)
    }
  }

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
        <div className="chatbot-window">
          <div className="chatbot-header">
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <div style={{ width: 34, height: 34, borderRadius: 10, background: 'linear-gradient(135deg,#f43f5e,#6366f1)', display: 'grid', placeItems: 'center', color: '#fff' }}>
                <Icon name="sparkle" size={18} />
              </div>
              <div>
                <b style={{ fontSize: 13, display: 'block' }}>GiftFinder AI Concierge</b>
                <small style={{ fontSize: 9, color: '#4ade80' }}>● Đang trực tuyến sẵn sàng</small>
              </div>
            </div>
            <button
              onClick={() => setOpen(false)}
              style={{ border: 0, background: 'none', color: '#94a3b8', cursor: 'pointer', padding: 4 }}
            >
              <Icon name="close" size={18} />
            </button>
          </div>

          <div className="chatbot-body">
            {messages.map((m, idx) => (
              <div key={idx} className={`chat-msg ${m.sender}`}>
                {m.sender === "bot" && (
                  <div style={{ width: 24, height: 24, borderRadius: '50%', background: '#fff1f2', color: '#e11d48', display: 'grid', placeItems: 'center', flexShrink: 0, marginTop: 2 }}>
                    🎁
                  </div>
                )}
                <div>
                  <div className="chat-bubble">{m.text}</div>
                  {m.product && (
                    <div style={{ marginTop: 8, padding: 10, background: '#fff', borderRadius: 12, border: '1px solid #e2e8f0', display: 'flex', gap: 10, alignItems: 'center' }}>
                      <img src={m.product.image} alt={m.product.name} style={{ width: 50, height: 50, borderRadius: 8, objectFit: 'cover' }} />
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <b style={{ fontSize: 10, display: 'block', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{m.product.name}</b>
                        <span style={{ fontSize: 11, color: '#e11d48', fontWeight: 700 }}>{money(m.product.price)}</span>
                      </div>
                      <button
                        onClick={() => {
                          setOpen(false)
                          if (m.product) {
                            if (openAffiliate && m.product.platform !== "DIRECT") {
                              openAffiliate(m.product)
                            } else {
                              onSelectProduct?.(m.product)
                              go("product")
                            }
                          }
                        }}
                        style={{ border: 0, background: '#111827', color: '#fff', padding: '6px 10px', borderRadius: 8, fontSize: 9, fontWeight: 700, cursor: 'pointer', whiteSpace: 'nowrap' }}
                      >
                        {m.product.platform === "DIRECT" ? "Xem chi tiết" : "Mua ngay"}
                      </button>
                    </div>
                  )}
                </div>
              </div>
            ))}

            {loading && (
              <div className="chat-msg bot">
                <div style={{ width: 24, height: 24, borderRadius: '50%', background: '#fff1f2', color: '#e11d48', display: 'grid', placeItems: 'center', flexShrink: 0, marginTop: 2 }}>
                  ✨
                </div>
                <div>
                  <div className="chat-bubble" style={{ background: '#f8fafc', color: '#64748b', fontStyle: 'italic' }}>
                    <span>AI đang phân tích và tìm quà phù hợp từ database...</span>
                  </div>
                </div>
              </div>
            )}

            <div className="chat-suggestions">
              {quickPrompts.map((p) => (
                <button key={p} onClick={() => handleSend(p)}>
                  {p}
                </button>
              ))}
            </div>
          </div>

          <form
            className="chat-input-bar"
            onSubmit={(e) => {
              e.preventDefault()
              handleSend()
            }}
          >
            <input
              placeholder="Nhập yêu cầu: người nhận, dịp, giá..."
              value={input}
              onChange={(e) => setInput(e.target.value)}
            />
            <button
              type="submit"
              style={{
                width: 36,
                height: 36,
                borderRadius: 10,
                border: 0,
                background: 'var(--ink)',
                color: '#fff',
                display: 'grid',
                placeItems: 'center',
                cursor: 'pointer',
              }}
            >
              <Icon name="arrow" size={16} />
            </button>
          </form>
        </div>
      )}
    </>
  )
}

function Home({
  go,
  openAffiliate,
  products = defaultProducts,
  onSelectProduct,
}: {
  go: (p: Page) => void
  openAffiliate: (p?: ProductItem) => void
  products?: ProductItem[]
  onSelectProduct?: (p: ProductItem) => void
}) {
  const [quiz, setQuiz] = useState(0)
  const quizData = [
    ["Bạn gái", "Bạn trai", "Bố mẹ", "Đồng nghiệp"],
    ["Sinh nhật", "Kỷ niệm", "Cảm ơn", "Bất ngờ"],
    ["Dưới 300k", "300k–500k", "500k–1tr", "Trên 1tr"],
  ]

  // Occasions State
  const occasions = [
    "Tất cả",
    "🎂 Sinh nhật",
    "💐 20/10 & 8/3",
    "💖 Kỷ niệm ngày yêu",
    "🎄 Giáng sinh",
    "🏡 Tân gia",
    "🎓 Tốt nghiệp",
  ]
  const [selectedOccasion, setSelectedOccasion] = useState("Tất cả")

  // 1. Live Realtime Countdown Timer
  const [countdownSeconds, setCountdownSeconds] = useState(15332)
  useEffect(() => {
    const timer = setInterval(() => {
      setCountdownSeconds((s) => (s > 0 ? s - 1 : 15332))
    }, 1000)
    return () => clearInterval(timer)
  }, [])

  const cHours = String(Math.floor(countdownSeconds / 3600)).padStart(2, "0")
  const cMinutes = String(Math.floor((countdownSeconds % 3600) / 60)).padStart(2, "0")
  const cSeconds = String(countdownSeconds % 60).padStart(2, "0")

  // 2. Animated Auto-Typing AI Search with interactive tags
  const samplePrompts = [
    "Quà sinh nhật tặng bạn gái 25 tuổi tầm 500k nhẹ nhàng...",
    "Set nến thơm & trà hoa tặng mẹ ngày 20/10 ấm áp...",
    "Món quà công nghệ bất ngờ kỷ niệm 1 năm ngày yêu...",
    "Set quà thủ công độc bản chúc mừng tân gia đồng nghiệp...",
  ]
  const [promptInput, setPromptInput] = useState("")
  const [isManualInput, setIsManualInput] = useState(false)
  const [promptIdx, setPromptIdx] = useState(0)
  const [charIdx, setCharIdx] = useState(0)
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    if (isManualInput) return

    const currentText = samplePrompts[promptIdx]
    let timeout: ReturnType<typeof setTimeout>

    if (!isDeleting && charIdx < currentText.length) {
      timeout = setTimeout(() => {
        setPromptInput(currentText.slice(0, charIdx + 1))
        setCharIdx(charIdx + 1)
      }, 50)
    } else if (!isDeleting && charIdx === currentText.length) {
      timeout = setTimeout(() => setIsDeleting(true), 2500)
    } else if (isDeleting && charIdx > 0) {
      timeout = setTimeout(() => {
        setPromptInput(currentText.slice(0, charIdx - 1))
        setCharIdx(charIdx - 1)
      }, 25)
    } else if (isDeleting && charIdx === 0) {
      setIsDeleting(false)
      setPromptIdx((promptIdx + 1) % samplePrompts.length)
    }

    return () => clearTimeout(timeout)
  }, [charIdx, isDeleting, promptIdx, isManualInput])

  // 3. Hero Visual Carousel with Auto-Rotate (3 ảnh AI độc quyền vừa tạo)
  const heroList = [images.heroPink, images.heroLuxury, images.heroTea]
  const heroBadges = [
    "🔥 Set quà Nàng Thơ",
    "⭐ Sang trọng & Lịch lãm",
    "🌿 Ấm áp tình thân",
  ]
  const [heroIdx, setHeroIdx] = useState(0)
  useEffect(() => {
    const timer = setInterval(() => {
      setHeroIdx((prev) => (prev + 1) % heroList.length)
    }, 4500)
    return () => clearInterval(timer)
  }, [heroList.length])

  const handleQuickTag = (tag: string) => {
    setIsManualInput(true)
    setPromptInput(`Tìm quà ${tag} phù hợp nhất...`)
  }

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
                  setIsManualInput(true)
                  setPromptInput(e.target.value)
                }}
              />
            </div>
            <div className="ai-search-bottom">
              <span>Thử nhanh</span>
              {["200 cành", "5 lốp", "1 củ", "Cho nàng", "Kỷ niệm"].map((x) => (
                <button 
                  key={x} 
                  onClick={() => handleQuickTag(x)}
                  style={{ transition: 'all 0.2s ease', cursor: 'pointer' }}
                >
                  {x}
                </button>
              ))}
              <Button variant="rose" onClick={() => go("catalog")}>
                Khám phá ngay <Icon name="arrow" />
              </Button>
            </div>
          </div>
          <div className="hero-trust">
            <span>
              <Icon name="check" size={15} /> 244+ quà thật 100%
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
          <Badge tone="dark" className="hero-badge-animated">{heroBadges[heroIdx]}</Badge>
          <div className="floating-rating">
            ⭐ <b>4.9/5</b>
            <small>428 đánh giá</small>
          </div>
          <div className="floating-sale">
            <b>-25%</b>
            <span>Hôm nay</span>
          </div>
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
              <button className="text-link" onClick={() => go("catalog")}>
                Xem tất cả đối tượng <Icon name="arrow" size={15} />
              </button>
            }
          />
          <div className="category-grid">
            {[
              ["Cho Nàng", images.flower, "Dịu dàng & tinh tế"],
              ["Cho Chàng", images.blue, "Lịch lãm & khác biệt"],
              ["Quà Bố Mẹ", images.tray, "Ấm áp & ý nghĩa"],
              ["Đồng Nghiệp", images.candle, "Trang nhã & vừa vặn"],
              ["Sinh Nhật Bạn Thân", images.macaron, "Rực rỡ & bất ngờ"],
              ["Kỷ Niệm Ngày Yêu", images.rose, "Lãng mạn & đáng nhớ"],
            ].map((c, i) => (
              <button
                className={`category-card c${i}`}
                key={c[0]}
                onClick={() => go("catalog")}
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
          title="Chọn Quà Đúng Dịp Kỷ Niệm"
          action={
            <button className="text-link" onClick={() => go("catalog")}>
              Lọc theo dịp <Icon name="arrow" size={15} />
            </button>
          }
        />
        <div className="occasion-pills">
          {occasions.map((occ) => (
            <button
              key={occ}
              className={`occasion-pill ${selectedOccasion === occ ? "active" : ""}`}
              onClick={() => setSelectedOccasion(occ)}
            >
              {occ}
            </button>
          ))}
        </div>
        <div className="product-row">
          {(products.length >= 15 ? products.slice(0, 5) : products.slice(2, 7)).map((p, i) => (
            <ProductCard
              key={p.id || p.name + i}
              product={p}
              onOpen={() => {
                onSelectProduct?.(p)
                go("product")
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
              <div className="countdown">
                <span>Kết thúc sau</span>
                <b>{cHours}</b>:<b>{cMinutes}</b>:<b style={{ color: '#f43f5e' }}>{cSeconds}</b>
              </div>
            }
          />
          <div className="product-row">
            {(products.length >= 15 ? products.slice(5, 10) : products.slice(0, 5)).map((p, i) => (
              <ProductCard
                key={p.id || p.name + i}
                product={p}
                deal
                sponsored={i === 2}
                onOpen={() => {
                  onSelectProduct?.(p)
                  go("product")
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
          eyebrow="ĐƯỢC TÀI TRỢ & TUYỂN CHỌN"
          title="Sản Phẩm Nổi Bật Được Yêu Thích"
          action={
            <div style={{ display: 'flex', gap: 6 }}>
              <Badge tone="rose">Top Bán Chạy</Badge>
              <Badge tone="indigo">Direct Tuyển Chọn</Badge>
            </div>
          }
        />
        <div className="product-row">
          {(products.length >= 15 ? products.slice(10, 15) : products.slice(3, 8)).map((p, i) => (
            <ProductCard
              key={p.id || p.name + i}
              product={p}
              sponsored={i % 2 === 0}
              onOpen={() => {
                onSelectProduct?.(p)
                go("product")
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
                  <span className="discount" style={{ position: 'absolute', top: 10, left: 10 }}>
                    {combo.occasion}
                  </span>
                </div>
                <div className="combo-body">
                  <h3 style={{ fontSize: 13, margin: '0 0 6px', fontWeight: 700, color: 'var(--ink)' }}>
                    {combo.name}
                  </h3>
                  <div className="combo-items">
                    📦 <b>Combo gồm:</b> {combo.items}
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 'auto', paddingTop: 8 }}>
                    <div>
                      <b style={{ color: 'var(--rose)', fontSize: 15 }}>{money(combo.price)}</b>
                      <del style={{ color: '#94a3b8', fontSize: 10, marginLeft: 6 }}>{money(combo.old)}</del>
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
          action={
            <Badge tone="green">Chỉ từ 12.000đ</Badge>
          }
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
                  border: '1px solid #e2e8f0',
                  background: '#f8fafc',
                  padding: '4px 8px',
                  borderRadius: 6,
                  fontSize: 9,
                  fontWeight: 600,
                  cursor: 'pointer',
                  color: 'var(--slate)'
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
              <div className="trust-icon-box" style={{ background: '#ecfdf5', color: '#16a34a' }}>
                <Icon name="shield" size={24} />
              </div>
              <h3>100% Quà Thật Chính Hãng</h3>
              <p>Mọi món quà đều được tuyển chọn từ Shopee Mall, TikTok Shop và xưởng sản xuất uy tín, kiểm định kỹ lưỡng.</p>
            </div>

            <div className="trust-card">
              <div className="trust-icon-box" style={{ background: '#eef2ff', color: '#4f46e5' }}>
                <span style={{ fontSize: 18, fontWeight: 900 }}>VN</span>
              </div>
              <h3>Thanh Toán Bảo Mật VNPAY</h3>
              <p>Cổng thanh toán Quốc gia VNPAY chuẩn an toàn PCI DSS. Quét mã VNPAY-QR, thẻ ATM 30+ ngân hàng và thẻ quốc tế.</p>
            </div>

            <div className="trust-card">
              <div className="trust-icon-box" style={{ background: '#fff1f2', color: '#e11d48' }}>
                <Icon name="check" size={24} />
              </div>
              <h3>Đổi Trả Trong 7 Ngày</h3>
              <p>Cam kết 1 đổi 1 hoặc hoàn tiền 100% nếu sản phẩm phát sinh lỗi, vỡ hỏng trong quá trình vận chuyển đến tay bạn.</p>
            </div>

            <div className="trust-card">
              <div className="trust-icon-box" style={{ background: '#fef3c7', color: '#d97706' }}>
                <Icon name="sparkle" size={24} />
              </div>
              <h3>Giao Hỏa Tốc & Thiệp Riêng</h3>
              <p>Giao nhanh trong 2h tại Hà Nội & TP.HCM. Tặng kèm thiệp viết tay nắn nót theo đúng lời nhắn gửi của bạn.</p>
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
                  onClick={() => (quiz < 2 ? setQuiz(quiz + 1) : go("catalog"))}
                >
                  {x}
                  <Icon name="chevron" size={16} />
                </button>
              ))}
            </div>
            {quiz === 2 && (
              <Button variant="rose" onClick={() => go("catalog")}>
                Xem gợi ý phù hợp ngay
              </Button>
            )}
          </div>
        </div>
      </section>

      {/* ========================================================
          10. GIAN HÀNG ĐƯỢC YÊU THÍCH
          ======================================================== */}
      <section className="section stores-section">
        <div className="shell">
          <SectionTitle
            eyebrow="ĐÃ XÁC MINH"
            title="Gian hàng được yêu thích"
          />
          <div className="stores">
            {[
              ["TM", "Tiệm Quà Thảo Mộc", "Vintage Gift Studio"],
              ["S", "Shopee Mall Official", "Gian hàng chính hãng"],
              ["T", "TikTok Shop Creator", "Top Creator 2025"],
              ["GF", "GiftFinder Select", "Quà tuyển chọn trực tiếp"],
            ].map((s, i) => (
              <article key={s[1]}>
                <i className={`store-logo s${i}`}>{s[0]}</i>
                <div>
                  <b>
                    {s[1]} <span>✓</span>
                  </b>
                  <small>{s[2]}</small>
                </div>
                <Icon name="chevron" />
              </article>
            ))}
          </div>
        </div>
      </section>

      {/* ========================================================
          11. KHÁCH HÀNG HÀI LÒNG & VIDEO UNBOXING
          ======================================================== */}
      <section className="section shell">
        <SectionTitle
          eyebrow="HƠN 12,000 NGƯỜI ĐÃ TÌM THẤY MÓN QUÀ"
          title="Khách Hàng Hài Lòng — Mở Hộp Là Mở Niềm Vui"
        />
        <div className="reviews">
          <article className="review-image">
            <img src={images.review1} alt="Khách hàng mở hộp quà" />
            <span className="play-ripple">
              <Icon name="play" /> Xem unboxing
            </span>
          </article>
          {[
            [
              "Mai Anh",
              "★★★★★",
              "“Mình chỉ mô tả vài câu, AI chọn đúng set nến cô bạn mê. Hộp gói đẹp hơn ảnh rất nhiều!”",
            ],
            [
              "Tuấn Minh",
              "★★★★★",
              "“Từ lúc chọn đến thanh toán chỉ 5 phút. Quà giao đúng ngày, thiệp viết tay cực kỳ chỉn chu.”",
            ],
          ].map((r) => (
            <article className="review-card" key={r[0]}>
              <div className="quote">“</div>
              <div className="stars">{r[1]}</div>
              <p>{r[2]}</p>
              <div className="reviewer">
                <i>{r[0][0]}</i>
                <span>
                  <b>{r[0]}</b>
                  <small>Khách hàng đã mua ✓</small>
                </span>
              </div>
            </article>
          ))}
        </div>
      </section>

      <Footer />
    </div>
  )
}

function Catalog({
  go,
  openAffiliate,
  products = defaultProducts,
  onSelectProduct,
}: {
  go: (p: Page) => void
  openAffiliate: (p?: ProductItem) => void
  products?: ProductItem[]
  onSelectProduct?: (p: ProductItem) => void
}) {
  const [maxPrice, setMaxPrice] = useState(2000)
  const [sort, setSort] = useState("AI đề xuất tốt nhất")
  return (
    <main className="shell catalog-page">
      <div className="breadcrumb">
        Trang chủ <Icon name="chevron" size={12} /> Quà tặng Bạn gái{" "}
        <Icon name="chevron" size={12} /> <b>Nến thơm cao cấp</b>
      </div>
      <div className="catalog-heading">
        <div>
          <Badge tone="rose">244+ LỰA CHỌN</Badge>
          <h1>Quà tặng dành cho bạn gái</h1>
          <p>Những món quà tinh tế được AI và đội ngũ GiftFinder tuyển chọn.</p>
        </div>
        <div className="catalog-promo">
          <Icon name="sparkle" />
          <span>
            <b>Để AI chọn giúp bạn</b>
            <small>Nhận gợi ý cá nhân hóa trong 3 giây</small>
          </span>
          <Icon name="arrow" />
        </div>
      </div>
      <div className="catalog-layout">
        <aside className="filters">
          <div className="filter-title">
            <b>Bộ lọc</b>
            <button>Xóa tất cả</button>
          </div>
          <Filter title="Danh mục">
            {[
              "Nến thơm (82)",
              "Mỹ phẩm (46)",
              "Set quà thủ công (38)",
              "Nước hoa (31)",
            ].map((x, i) => (
              <label key={x}>
                <input type="checkbox" defaultChecked={i === 0} />
                {x}
              </label>
            ))}
          </Filter>
          <Filter title="Khoảng giá">
            <div className="range-values">
              <span>50.000đ</span>
              <b>{money(maxPrice * 1000)}</b>
            </div>
            <input
              type="range"
              min="100"
              max="2000"
              step="100"
              value={maxPrice}
              onChange={(e) => setMaxPrice(Number(e.target.value))}
            />
            {["Dưới 100k", "100k – 300k", "300k – 700k", "Trên 1 củ"].map(
              (x) => (
                <label key={x}>
                  <input type="radio" name="price" />
                  {x}
                </label>
              ),
            )}
          </Filter>
          <Filter title="Nguồn hàng">
            {["Shopee", "TikTok Shop", "Lazada", "GiftFinder Direct"].map(
              (x, i) => (
                <label key={x}>
                  <input type="checkbox" defaultChecked={i < 2} />
                  {x}
                </label>
              ),
            )}
          </Filter>
          <Filter title="Cung hoàng đạo">
            <div className="zodiacs">
              {[
                "♈",
                "♉",
                "♊",
                "♋",
                "♌",
                "♍",
                "♎",
                "♏",
                "♐",
                "♑",
                "♒",
                "♓",
              ].map((x) => (
                <button key={x}>{x}</button>
              ))}
            </div>
          </Filter>
          <Filter title="Đánh giá">
            <label>
              <input type="checkbox" /> <span className="stars">★★★★</span> trở
              lên
            </label>
          </Filter>
        </aside>
        <section className="listing">
          <div className="listing-tools">
            <b>Hiển thị 24/244 món quà phù hợp</b>
            <label>
              Sắp xếp{" "}
              <select value={sort} onChange={(e) => setSort(e.target.value)}>
                {[
                  "AI đề xuất tốt nhất",
                  "Bán chạy nhất",
                  "Giá thấp đến cao",
                  "Giá cao đến thấp",
                  "Đánh giá cao",
                ].map((x) => (
                  <option key={x}>{x}</option>
                ))}
              </select>
            </label>
          </div>
          <div className="catalog-grid">
            {products.map((p, i) => (
              <ProductCard
                key={p.id || p.name + i}
                product={p}
                sponsored={i === 2 || i === 6}
                onOpen={() => {
                  onSelectProduct?.(p)
                  go("product")
                }}
                onAffiliate={() => openAffiliate(p)}
              />
            ))}
          </div>
          <div className="pagination">
            <button>‹</button>
            <button className="active">1</button>
            <button>2</button>
            <button>3</button>
            <span>…</span>
            <button>12</button>
            <button>›</button>
          </div>
        </section>
      </div>
    </main>
  )
}

function Filter({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="filter-group">
      <h3>
        {title}
        <span>−</span>
      </h3>
      <div>{children}</div>
    </div>
  )
}

function Wishlist({
  go,
  openAffiliate,
  onSelectProduct,
}: {
  go: (p: Page) => void
  openAffiliate: (p?: ProductItem) => void
  products?: ProductItem[]
  onSelectProduct?: (p: ProductItem) => void
}) {
  const { wishlistItems, removeFromWishlist } = useWishlist()

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
  }))

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
                    onSelectProduct?.(product)
                    go("product")
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
        <div className="empty-state" style={{ padding: "60px 20px", textAlign: "center" }}>
          <div className="empty-gift" style={{ margin: "0 auto 16px" }}>
            <Icon name="heart" size={38} />
          </div>
          <h2 style={{ fontSize: "20px", fontWeight: 700, marginBottom: "8px" }}>
            Danh sách yêu thích đang trống
          </h2>
          <p style={{ color: "var(--muted)", maxWidth: "420px", margin: "0 auto 24px", lineHeight: 1.6 }}>
            Lưu những món quà bạn thích bằng cách bấm vào biểu tượng trái tim ♡ trên các sản phẩm để dễ dàng quay lại sau!
          </p>
          <Button variant="rose" onClick={() => go("catalog")}>
            Khám phá quà tặng ngay
          </Button>
        </div>
      )}
    </main>
  )
}

function MetricCard({
  icon,
  label,
  value,
  detail,
  tone = "rose",
  badge,
}: {
  icon: IconName
  label: string
  value: string
  detail: string
  tone?: string
  badge?: string
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
  )
}

function ProductModal({
  onClose,
  onSuccess,
}: {
  onClose: () => void
  onSuccess: () => void
}) {
  const [name, setName] = useState("")
  const [price, setPrice] = useState("")
  const [origPrice, setOrigPrice] = useState("")
  const [url, setUrl] = useState("")
  const [img, setImg] = useState("")
  const [desc, setDesc] = useState("")
  const [loading, setLoading] = useState(false)

  const handleSubmit = async () => {
    const priceNum = parseInt(price.replace(/\D/g, ""), 10)
    const origPriceNum = origPrice ? parseInt(origPrice.replace(/\D/g, ""), 10) : undefined
    if (!name.trim() || !priceNum || !url.trim()) {
      alert("Vui lòng nhập tên món quà, giá bán và đường dẫn sản phẩm!")
      return
    }
    setLoading(true)
    try {
      await merchantService.createProduct({
        name: name.trim(),
        price: priceNum,
        originalPrice: origPriceNum,
        originalUrl: url.trim(),
        imageUrl: img.trim() || images.hero,
        description: desc.trim(),
      })
      onSuccess()
    } catch (e: any) {
      alert(e.response?.data?.message || "Đăng quà thất bại.")
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="modal-layer" onMouseDown={onClose}>
      <div
        className="modal-card"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <div className="modal-heading">
          <div className="modal-title-icon">
            <Icon name="gift" />
          </div>
          <div>
            <h2>Đăng sản phẩm quà tặng mới</h2>
            <p>Thông tin sẽ được Admin kiểm duyệt trước khi hiển thị.</p>
          </div>
          <button aria-label="Đóng" className="close-button" onClick={onClose}>
            <Icon name="close" />
          </button>
        </div>
        <div className="modal-body">
          <label className="field">
            <span>Tên món quà</span>
            <div className="input-wrap">
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="VD: Set nến thơm & hoa khô 20/10"
              />
            </div>
          </label>
          <div className="field-row">
            <label className="field">
              <span>Giá bán</span>
              <div className="input-wrap">
                <input
                  value={price}
                  onChange={(e) => setPrice(e.target.value)}
                  placeholder="489.000đ"
                />
              </div>
            </label>
            <label className="field">
              <span>Giá gốc</span>
              <div className="input-wrap">
                <input
                  value={origPrice}
                  onChange={(e) => setOrigPrice(e.target.value)}
                  placeholder="599.000đ"
                />
              </div>
            </label>
          </div>
          <label className="field">
            <span>Đường dẫn Shopee / TikTok Shop</span>
            <div className="input-wrap">
              <input
                value={url}
                onChange={(e) => setUrl(e.target.value)}
                placeholder="https://shopee.vn/..."
              />
            </div>
          </label>
          <label className="field">
            <span>Link ảnh sản phẩm</span>
            <div className="input-wrap">
              <input
                value={img}
                onChange={(e) => setImg(e.target.value)}
                placeholder="Dán đường dẫn hình ảnh chất lượng cao"
              />
            </div>
          </label>
          <label className="field">
            <span>Mô tả ngắn</span>
            <textarea
              value={desc}
              onChange={(e) => setDesc(e.target.value)}
              placeholder="Điểm nổi bật và câu chuyện của món quà..."
            />
          </label>
          <div className="ai-tags">
            <div>
              <Icon name="sparkle" />
              <span>
                <strong>Thẻ tag đề xuất bởi AI</strong>
                <small>Dựa trên tên và mô tả sản phẩm</small>
              </span>
            </div>
            <div className="tag-list">
              <Badge tone="indigo-soft">Nến thơm</Badge>
              <Badge tone="indigo-soft">20/10</Badge>
              <Badge tone="indigo-soft">Bạn gái</Badge>
            </div>
          </div>
        </div>
        <div className="modal-footer">
          <Button variant="ghost" onClick={onClose}>
            Hủy
          </Button>
          <Button icon="arrow-right" onClick={handleSubmit} disabled={loading}>
            {loading ? "Đang gửi..." : "Gửi quà tặng duyệt"}
          </Button>
        </div>
      </div>
    </div>
  )
}

function MerchantPage({ go }: { go: (p: Page) => void }) {
  const { user } = useAuth()
  const [modalOpen, setModalOpen] = useState(false)
  const [filter, setFilter] = useState("Tất cả")
  const [stats, setStats] = useState<MerchantDashboardStats | null>(null)
  const [myProducts, setMyProducts] = useState<MerchantProductItem[]>([])
  const [notice, setNotice] = useState<string | null>(null)

  const loadData = async () => {
    try {
      const [s, p] = await Promise.all([
        merchantService.getDashboardStats().catch(() => null),
        merchantService.getMyProducts().catch(() => ({ items: [], totalCount: 0 })),
      ])
      if (s) setStats(s)
      if (p?.items) setMyProducts(p.items)
    } catch (e) {
      console.error(e)
    }
  }

  useEffect(() => {
    loadData()
  }, [])

  const displayProducts = myProducts.map((p) => ({
    id: p.id,
    name: p.name,
    price: p.price.toLocaleString("vi-VN") + "đ",
    clicks: (p.clickCount || 0) + " lượt click",
    status: p.status === "Approved" ? "Đã duyệt" : p.status === "Rejected" ? "Từ chối" : "Chờ duyệt",
    tone: p.status === "Approved" ? "success" : p.status === "Rejected" ? "danger" : "warning",
    image: p.imageUrl || images.hero,
  }))

  const filtered = filter === "Tất cả" ? displayProducts : displayProducts.filter((product) => product.status === filter)

  const handleBoost = async (prodId: string) => {
    try {
      const res = await merchantService.buyFeatured(prodId, 0)
      if (res?.paymentUrl) {
        window.open(res.paymentUrl, "_blank", "noopener,noreferrer")
        setNotice("Đã mở cổng thanh toán VNPAY Sandbox cho gói Featured 49.000đ/tuần!")
      }
    } catch (err: any) {
      alert(err.response?.data?.message || "Lỗi tạo link thanh toán VNPAY.")
    }
  }

  return (
    <div className="app-page">
      <main className="dashboard-shell">
        <div className="page-heading">
          <div>
            <div className="breadcrumb">
              Trang chủ <span>/</span> Kênh Người Bán
            </div>
            <div className="shop-status">
              <Badge tone="outline">ĐỐI TÁC CHÍNH THỨC</Badge>
              <span>
                <i />
                Đang hoạt động
              </span>
            </div>
            <h1>Gian hàng: {stats?.shopName || user?.shopName || "Tiệm Gốm & Nến Mộc"}</h1>
            <p>Quản lý sản phẩm, hiệu suất và tăng trưởng gian hàng của bạn.</p>
          </div>
          <div className="heading-actions">
            <Button
              variant="secondary"
              icon="arrow-left"
              onClick={() => go("home")}
            >
              Xem website
            </Button>
            <Button icon="plus" onClick={() => setModalOpen(true)}>
              Đăng quà mới
            </Button>
          </div>
        </div>

        {notice && (
          <div style={{ background: "#ecfdf5", border: "1px solid #a7f3d0", color: "#065f46", padding: "12px 16px", borderRadius: "12px", margin: "16px 0", fontSize: "13px", fontWeight: 600 }}>
            ✓ {notice}
          </div>
        )}

        <section className="metric-grid">
          <MetricCard
            icon="gift"
            label="Tổng quà tặng"
            value={String(stats?.totalProducts ?? myProducts.length)}
            detail="Sản phẩm trên hệ thống"
            tone="rose"
          />
          <MetricCard
            icon="trend"
            label="Lượt click khách mua"
            value={stats?.totalClicks != null ? String(stats.totalClicks) : "0"}
            detail="Tổng lượt truy cập"
            tone="green"
            badge={stats?.totalClicks ? "+18% tuần này" : undefined}
          />
          <MetricCard
            icon="sparkle"
            label="Quà đẩy Top Featured"
            value={String(stats?.featuredProducts ?? 0)}
            detail="Gói VNPAY đang kích hoạt"
            tone="indigo"
          />
          <MetricCard
            icon="clock"
            label="Chờ Admin duyệt"
            value={String(stats?.pendingProducts ?? myProducts.filter((p) => p.status === "Pending").length)}
            detail="Đang kiểm duyệt"
            tone="amber"
          />
        </section>

        <section className="content-card products-card">
          <div className="section-heading">
            <div>
              <h2>Danh sách quà tặng</h2>
              <p>Quản lý tất cả sản phẩm trong gian hàng của bạn</p>
            </div>
            <div className="filter-tabs">
              {[
                ["Tất cả", displayProducts.length],
                ["Đã duyệt", displayProducts.filter((p) => p.status === "Đã duyệt").length],
                ["Chờ duyệt", displayProducts.filter((p) => p.status === "Chờ duyệt").length],
                ["Từ chối", displayProducts.filter((p) => p.status === "Từ chối").length],
              ].map(([name, count]) => (
                <button
                  className={filter === name ? "active" : ""}
                  key={name}
                  onClick={() => setFilter(String(name))}
                >
                  {name} <span>{count}</span>
                </button>
              ))}
            </div>
          </div>
          <div className="table-head">
            <span>Sản phẩm</span>
            <span>Giá bán</span>
            <span>Hiệu suất</span>
            <span>Trạng thái</span>
            <span>Thao tác</span>
          </div>
          <div className="product-list">
            {filtered.length > 0 ? (
              filtered.map((product) => (
                <article className="product-row" key={product.id || product.name}>
                  <div className="product-main">
                    <img src={product.image} alt="" />
                    <div>
                      <h3>{product.name}</h3>
                      <span>
                        <Icon name="tag" size={14} /> {stats?.shopName || user?.shopName || "Gian hàng đối tác"}
                      </span>
                    </div>
                  </div>
                  <strong className="product-price">{product.price}</strong>
                  <span className="click-count">
                    <Icon name="trend" size={16} />
                    {product.clicks}
                  </span>
                  <Badge tone={product.tone}>
                    <i />
                    {product.status}
                  </Badge>
                  <Button
                    variant="indigo"
                    icon="sparkle"
                    className="boost-button"
                    onClick={() => handleBoost(product.id)}
                  >
                    Đẩy Top <span>49k</span>
                  </Button>
                </article>
              ))
            ) : (
              <div className="empty-state" style={{ padding: "48px 24px", textAlign: "center" }}>
                <div className="empty-gift" style={{ margin: "0 auto 16px" }}>
                  <Icon name="store" size={38} />
                </div>
                <h3 style={{ fontSize: "18px", fontWeight: 700, marginBottom: "8px" }}>
                  Chưa có sản phẩm nào trong mục này
                </h3>
                <p style={{ color: "var(--muted)", maxWidth: "420px", margin: "0 auto 20px" }}>
                  Đăng sản phẩm quà tặng đầu tiên của bạn để tiếp cận khách hàng tiềm năng!
                </p>
                <Button variant="primary" icon="plus" onClick={() => setModalOpen(true)}>
                  Đăng quà mới ngay
                </Button>
              </div>
            )}
          </div>
        </section>
      </main>
      {modalOpen && (
        <ProductModal
          onClose={() => setModalOpen(false)}
          onSuccess={() => {
            setModalOpen(false)
            setNotice("Đã gửi quà tặng thành công! Sản phẩm đang chờ Admin kiểm duyệt.")
            loadData()
          }}
        />
      )}
    </div>
  )
}

function AuthPage({ go }: { go: (p: Page) => void }) {
  const { login, register: registerUser } = useAuth()
  const [isRegister, setRegister] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [fullName, setFullName] = useState("")
  const [rememberMe, setRememberMe] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    setLoading(true)
    try {
      if (isRegister) {
        if (!fullName.trim()) throw new Error("Vui lòng nhập họ và tên.")
        if (!email.trim()) throw new Error("Vui lòng nhập email.")
        if (!password || password.length < 6)
          throw new Error("Mật khẩu phải từ 6 ký tự trở lên.")
        const res = await registerUser(
          { email: email.trim(), password, fullName: fullName.trim() },
          rememberMe
        )
        if (res.role === "Merchant") go("merchant")
        else go("home")
      } else {
        if (!email.trim() || !password)
          throw new Error("Vui lòng nhập email và mật khẩu.")
        const res = await login({ email: email.trim(), password }, rememberMe)
        if (res.role === "Admin") go("admin")
        else if (res.role === "Merchant") go("merchant")
        else go("home")
      }
    } catch (err: any) {
      const msg =
        err.response?.data?.message ||
        err.response?.data?.title ||
        err.message ||
        "Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin."
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  const handleQuickLogin = async (quickEmail: string, quickPass: string) => {
    setError(null)
    setLoading(true)
    try {
      const res = await login({ email: quickEmail, password: quickPass }, true)
      if (res.role === "Admin") go("admin")
      else if (res.role === "Merchant") go("merchant")
      else go("home")
    } catch (err: any) {
      setError(err.response?.data?.message || err.message)
    } finally {
      setLoading(false)
    }
  }

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
              {isRegister ? "THÀNH VIÊN MỚI" : "RẤT VUI ĐƯỢC GẶP LẠI"}
            </Badge>
            <h2>{isRegister ? "Tạo tài khoản mới" : "Chào mừng trở lại"}</h2>
            <p>
              {isRegister
                ? "Tạo tài khoản để bắt đầu hành trình tìm quà."
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
            <Button variant="secondary" className="google-button">
              <span className="google-g">G</span>Tiếp tục với Google
            </Button>
            <div className="divider">
              <span>hoặc dùng email</span>
            </div>
            <div className="form-stack">
              {isRegister && (
                <label className="field">
                  <span>Họ và tên</span>
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
              <label className="field">
                <span>Email</span>
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
            <Button
              type="submit"
              className="submit-button"
              disabled={loading}
            >
              {loading
                ? "Đang xử lý..."
                : isRegister
                ? "Tạo tài khoản"
                : "Đăng nhập"}
              <Icon name="arrow-right" />
            </Button>
          </form>

          {/* Quick Login Test Accounts */}
          <div
            style={{
              marginTop: "16px",
              padding: "12px",
              background: "#f8fafc",
              borderRadius: "12px",
              border: "1px dashed #cbd5e1",
              textAlign: "left",
            }}
          >
            <div
              style={{
                fontSize: "11px",
                fontWeight: 700,
                color: "var(--slate)",
                marginBottom: "8px",
              }}
            >
              ⚡ ĐĂNG NHẬP NHANH (MÔI TRƯỜNG TEST):
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: "6px" }}>
              <button
                type="button"
                onClick={() =>
                  handleQuickLogin("admin@giftfinder.vn", "Admin@123456")
                }
                style={{
                  padding: "7px 10px",
                  fontSize: "11px",
                  borderRadius: "8px",
                  border: "1px solid #111827",
                  background: "#111827",
                  color: "#fff",
                  cursor: "pointer",
                  fontWeight: 600,
                  textAlign: "left",
                }}
              >
                🛡️ Quản trị viên (Admin) — admin@giftfinder.vn
              </button>
              <button
                type="button"
                onClick={() =>
                  handleQuickLogin("shopdemo@giftfinder.vn", "Password@123")
                }
                style={{
                  padding: "7px 10px",
                  fontSize: "11px",
                  borderRadius: "8px",
                  border: "1px solid #4f46e5",
                  background: "#eef2ff",
                  color: "#4f46e5",
                  cursor: "pointer",
                  fontWeight: 600,
                  textAlign: "left",
                }}
              >
                🏪 Chủ Gian hàng (Merchant) — shopdemo@giftfinder.vn
              </button>
              <button
                type="button"
                onClick={() =>
                  handleQuickLogin("memberdemo@giftfinder.vn", "Password@123")
                }
                style={{
                  padding: "7px 10px",
                  fontSize: "11px",
                  borderRadius: "8px",
                  border: "1px solid #e11d48",
                  background: "#fff1f2",
                  color: "#e11d48",
                  cursor: "pointer",
                  fontWeight: 600,
                  textAlign: "left",
                }}
              >
                👤 Người mua quà (Member) — memberdemo@giftfinder.vn
              </button>
            </div>
          </div>

          <p className="auth-toggle">
            {isRegister ? "Đã có tài khoản?" : "Chưa có tài khoản?"}{" "}
            <button
              type="button"
              onClick={() => {
                setRegister(!isRegister)
                setError(null)
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
  )
}

function RejectModal({
  name,
  onClose,
  onReject,
}: {
  name: string
  onClose: () => void
  onReject: (reason: string) => void
}) {
  const [reason, setReason] = useState("")
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
            onClick={() => onReject(reason || "Sản phẩm chưa đạt tiêu chuẩn chất lượng của GiftFinder.")}
          >
            Xác nhận từ chối
          </Button>
        </div>
      </div>
    </div>
  )
}

function AdminConsole({ go }: { go: (p: Page) => void }) {
  const [pending, setPending] = useState<Array<{ id: string; name: string; shop: string; price: string; category: string; image: string }>>([])
  const [rejecting, setRejecting] = useState<{ id: string; name: string } | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [totalProducts, setTotalProducts] = useState<number>(243)

  const loadPending = async () => {
    try {
      const data = await adminService.getPendingProducts(1, 50)
      if (data?.items && Array.isArray(data.items)) {
        setPending(
          data.items.map((p) => ({
            id: p.id,
            name: p.name,
            shop: p.source || "Gian hàng đối tác",
            price: p.price.toLocaleString("vi-VN") + "đ",
            category: p.tags?.[0]?.name || "Quà tặng",
            image: p.imageUrl || images.hero,
          }))
        )
      } else {
        setPending([])
      }
    } catch (e) {
      console.error("Error loading pending products:", e)
      setPending([])
    }
  }

  useEffect(() => {
    loadPending()
    productService.getProducts({ page: 1, pageSize: 1 }).then((res) => {
      if (res?.total) {
        setTotalProducts(res.total)
      }
    }).catch(() => {})
  }, [])

  const handleApprove = async (id: string, name: string) => {
    try {
      await adminService.approveProduct(id)
      setNotice(`Đã phê duyệt thành công sản phẩm "${name}"!`)
      setPending((items) => items.filter((item) => item.id !== id))
    } catch (err: any) {
      alert(err.response?.data?.message || "Lỗi phê duyệt.")
    }
  }

  const handleRejectConfirm = async (reason: string) => {
    if (!rejecting) return
    try {
      await adminService.rejectProduct(rejecting.id, reason)
      setNotice(`Đã từ chối "${rejecting.name}" với lý do: "${reason}"`)
      setPending((items) => items.filter((item) => item.id !== rejecting.id))
      setRejecting(null)
    } catch (err: any) {
      alert(err.response?.data?.message || "Lỗi từ chối.")
    }
  }

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
            <Button variant="primary" icon="arrow-left" onClick={() => go("home")}>
              Xem website
            </Button>
          </div>
        </div>

        {notice && (
          <div style={{ background: "#ecfdf5", border: "1px solid #a7f3d0", color: "#065f46", padding: "12px 16px", borderRadius: "12px", margin: "16px 0", fontSize: "13px", fontWeight: 600 }}>
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
                      onClick={() => setRejecting({ id: item.id, name: item.name })}
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
              <p>Không còn sản phẩm nào đang chờ xử lý. Cơ sở dữ liệu PostgreSQL đã cập nhật.</p>
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
  )
}

function ProductDetail({
  go,
  openAffiliate,
  product,
  products = defaultProducts,
  onSelectProduct,
}: {
  go: (p: Page) => void
  openAffiliate: (p?: ProductItem) => void
  product?: ProductItem | null
  products?: ProductItem[]
  onSelectProduct?: (p: ProductItem) => void
}) {
  const { addToCart } = useCart()
  const currentProduct = product || products[0] || defaultProducts[0]

  const [photo, setPhoto] = useState(currentProduct.image || images.candle)
  const [variant, setVariant] = useState("Hương Lavender")
  const [qty, setQty] = useState(1)

  useEffect(() => {
    if (currentProduct?.image) {
      setPhoto(currentProduct.image)
    }
  }, [currentProduct])

  const thumbs = [
    currentProduct.image || images.candle,
    images.tray,
    images.flower,
    images.rose,
    images.macaron,
  ]
  return (
    <main className="shell pdp">
      <div className="breadcrumb">
        Trang chủ <Icon name="chevron" size={12} /> Quà tặng{" "}
        <Icon name="chevron" size={12} /> <b>{currentProduct.name}</b>
      </div>
      <div className="pdp-main">
        <section className="gallery">
          <div className="gallery-main">
            <img src={photo} alt={currentProduct.name} />
            <button className="video-badge">
              <Icon name="play" /> Video thực tế từ khách hàng
            </button>
            <span>Di chuột để phóng to</span>
          </div>
          <div className="thumbnails">
            {thumbs.map((x, i) => (
              <button
                className={photo === x ? "active" : ""}
                key={x + i}
                onClick={() => setPhoto(x)}
              >
                <img src={x} alt={`Ảnh sản phẩm ${i + 1}`} />
                {i === 4 && <Icon name="play" />}
              </button>
            ))}
          </div>
        </section>
        <section className="buy-panel">
          <Badge tone="rose">{currentProduct.platform === "DIRECT" ? "GIFTFINDER DIRECT" : currentProduct.platform}</Badge>
          <h1>{currentProduct.name}</h1>
          <button className="shop-name">
            Gian hàng đối tác chính thức <span>✓</span>
          </button>
          <div className="pdp-meta">
            <span className="stars">★★★★★</span> <b>{currentProduct.rating || "4.9"}</b> ·{" "}
            <u>{currentProduct.sold || "120"} đánh giá</u>
            <i />
            Đã bán {currentProduct.sold || "500+"}
          </div>
          <div className="pdp-price">
            <b>{money(currentProduct.price)}</b>
            <del>{money(currentProduct.old)}</del>
            <Badge tone="green">Tiết kiệm {money(Math.max(0, currentProduct.old - currentProduct.price))}</Badge>
          </div>
          <div className="price-chart">
            <div>
              <span>📈 Biểu đồ lịch sử giá 30 ngày qua</span>
              <Badge tone="green">Giá tốt nhất</Badge>
            </div>
            <svg viewBox="0 0 420 76" preserveAspectRatio="none">
              <path
                d="M0 12 C40 12 50 25 90 24 S145 28 180 34 S240 30 275 52 S345 50 420 65"
                fill="none"
                stroke="#16a34a"
                strokeWidth="3"
              />
              <path
                d="M0 12 C40 12 50 25 90 24 S145 28 180 34 S240 30 275 52 S345 50 420 65 L420 76 L0 76Z"
                fill="url(#fade)"
              />
              <defs>
                <linearGradient id="fade" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0" stopColor="#16a34a" stopOpacity=".2" />
                  <stop offset="1" stopColor="#16a34a" stopOpacity="0" />
                </linearGradient>
              </defs>
            </svg>
            <small>
              {money(currentProduct.old)} <span>30 ngày trước</span>
              <b>{money(currentProduct.price)} hôm nay</b>
            </small>
          </div>
          <div className="option-block">
            <label>
              Chọn phân loại: <b>{variant}</b>
            </label>
            <div className="variants">
              {["Hương Lavender", "Hương Gỗ Thông", "Set Quà Fullbox"].map(
                (x) => (
                  <button
                    className={variant === x ? "active" : ""}
                    onClick={() => setVariant(x)}
                    key={x}
                  >
                    {variant === x && <Icon name="check" size={14} />} {x}
                  </button>
                ),
              )}
            </div>
          </div>
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
            <small>Còn hàng trong kho</small>
          </div>
          <div className="direct-actions">
            <Button
              variant="secondary"
              onClick={() => {
                addToCart(currentProduct, qty, variant)
                go("cart")
              }}
              icon="cart"
            >
              Thêm vào giỏ hàng
            </Button>
            <Button
              variant="rose"
              onClick={() => {
                addToCart(currentProduct, qty, variant)
                go("checkout")
              }}
            >
              Mua ngay với VNPAY
            </Button>
          </div>
          {currentProduct.platform !== "DIRECT" && (
            <div className="affiliate-actions">
              <Button onClick={() => openAffiliate(currentProduct)}>
                Mua ngay trên sàn đối tác ({currentProduct.platform}) <Icon name="arrow" />
              </Button>
              <Button variant="ghost" icon="bell">
                Bật chuông theo dõi khi giảm giá
              </Button>
            </div>
          )}
          <div className="trust-banner">
            {[
              ["✓", "100% quà tuyển chọn"],
              ["↻", "Đổi trả 7 ngày"],
              ["⚡", "Hỏa tốc 2h"],
            ].map((x) => (
              <span key={x[1]}>
                <i>{x[0]}</i>
                {x[1]}
              </span>
            ))}
          </div>
        </section>
      </div>
      <section className="below-fold">
        <div className="tabs">
          <button className="active">Mô tả sản phẩm</button>
          <button>Đánh giá</button>
          <button>Chính sách đổi trả</button>
        </div>
        <div className="description">
          <div>
            <h2>Món quà mang trọn tình cảm & sự tinh tế</h2>
            <p>
              {currentProduct.name} được lựa chọn kỹ lưỡng, kiểm duyệt chất lượng nghiêm ngặt trước khi đến tay người nhận. Mỗi hộp quà được gói riêng theo đơn, kèm thiệp viết tay để lời chúc của bạn trở nên thật đặc biệt.
            </p>
            <ul>
              <li>Sản phẩm cam kết đúng mô tả, xuất xứ rõ ràng</li>
              <li>Hộp quà sang trọng, bảo vệ an toàn khi vận chuyển</li>
              <li>Hỗ trợ viết thiệp chúc mừng theo yêu cầu</li>
            </ul>
          </div>
          <img src={currentProduct.image || images.hero} alt={currentProduct.name} />
        </div>
      </section>
      <section className="section">
        <SectionTitle title="Khách hàng cũng xem thêm" />
        <div className="product-row">
          {products.slice(1, 5).map((p, i) => (
            <ProductCard
              key={p.id || p.name + i}
              product={p}
              onOpen={() => {
                onSelectProduct?.(p)
                window.scrollTo({ top: 0, behavior: "smooth" })
              }}
              onAffiliate={() => openAffiliate(p)}
            />
          ))}
        </div>
      </section>
    </main>
  )
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
  } = useCart()
  const [voucher, setVoucher] = useState(false)

  const finalTotal = Math.max(0, total - (voucher ? 50000 : 0))

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
        <div className="empty-state" style={{ padding: "60px 20px", textAlign: "center" }}>
          <div className="empty-gift" style={{ margin: "0 auto 16px" }}>
            <Icon name="cart" size={38} />
          </div>
          <h2 style={{ fontSize: "20px", fontWeight: 700, marginBottom: "8px" }}>
            Giỏ hàng của bạn đang trống
          </h2>
          <p style={{ color: "var(--muted)", maxWidth: "420px", margin: "0 auto 24px", lineHeight: 1.6 }}>
            Bạn chưa thêm món quà nào vào giỏ. Hãy dạo quanh GiftFinder để khám phá những món quà đặc biệt nhé!
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
                    {item.platform === "DIRECT" ? "GIFTFINDER DIRECT" : item.platform}
                  </Badge>
                  <h3>{item.name}</h3>
                  <p>Phân loại: {item.variant}</p>
                  <button onClick={() => removeFromCart(item.id)}>
                    <Icon name="trash" size={14} /> Xóa
                  </button>
                </div>
                <b>{money(item.price)}</b>
                <div className="stepper">
                  <button onClick={() => updateQuantity(item.id, Math.max(1, item.quantity - 1))}>
                    <Icon name="minus" size={13} />
                  </button>
                  <b>{item.quantity}</b>
                  <button onClick={() => updateQuantity(item.id, item.quantity + 1)}>
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
                  {hasGiftWrap ? "GÓI QUÀ CAO CẤP (ĐÃ CHỌN)" : "GÓI QUÀ CAO CẤP (CHƯA CHỌN)"}
                </Badge>
                <b>Hộp quà & thiệp viết tay cá nhân hóa</b>
                <span>“Chúc em luôn rạng rỡ như những đóa hoa...”</span>
              </div>
              <strong>{hasGiftWrap ? "+30.000đ" : "0đ"}</strong>
              <button
                onClick={(e) => {
                  e.stopPropagation()
                  setHasGiftWrap(!hasGiftWrap)
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
  )
}

function OrderSummary({
  subtotal,
  shippingFee = 25000,
  giftWrapFee = 0,
  voucher,
  onVoucher,
  action,
}: {
  subtotal: number
  shippingFee?: number
  giftWrapFee?: number
  voucher: boolean
  onVoucher: () => void
  action: ReactNode
}) {
  const actualShipping = subtotal > 0 ? shippingFee : 0
  const discount = voucher ? 50000 : 0
  const total = Math.max(0, subtotal + actualShipping + giftWrapFee - discount)

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
        <Icon name="shield" size={14} /> Thông tin thanh toán được mã hóa an toàn
      </p>
    </aside>
  )
}

function Checkout({ go }: { go: (p: Page) => void }) {
  const { items, subtotal, shippingFee, giftWrapFee, hasGiftWrap, clearCart } = useCart()
  const [payment, setPayment] = useState("vnpay")
  const [voucher, setVoucher] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  if (items.length === 0) {
    return (
      <main className="shell flow-page">
        <div className="empty-state" style={{ padding: "60px 20px", textAlign: "center" }}>
          <div className="empty-gift" style={{ margin: "0 auto 16px" }}>
            <Icon name="cart" size={38} />
          </div>
          <h2 style={{ fontSize: "20px", fontWeight: 700, marginBottom: "8px" }}>
            Chưa có sản phẩm để thanh toán
          </h2>
          <p style={{ color: "var(--muted)", maxWidth: "420px", margin: "0 auto 24px", lineHeight: 1.6 }}>
            Giỏ hàng của bạn đang trống. Vui lòng chọn món quà yêu thích trước khi tiến hành thanh toán nhé!
          </p>
          <Button variant="rose" onClick={() => go("catalog")}>
            Khám phá quà tặng ngay
          </Button>
        </div>
      </main>
    )
  }

  const handleConfirmOrder = () => {
    setSubmitting(true)
    setTimeout(() => {
      clearCart()
      setSubmitting(false)
      go("success")
    }, 600)
  }

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
                  <small>{item.variant} · Số lượng: {item.quantity}</small>
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
            <Button variant="rose" onClick={handleConfirmOrder} disabled={submitting}>
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
  )
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
  )
}

function AffiliateModal({
  close,
  product,
  targetUrl,
}: {
  close: () => void
  product?: ProductItem | null
  targetUrl?: string
}) {
  const [seconds, setSeconds] = useState(1)
  const platformName =
    product?.platform === "TIKTOK" ? "TikTok Shop" : "Shopee Việt Nam"
  const finalUrl =
    targetUrl || product?.affiliateUrl || product?.originalUrl || "https://shopee.vn"

  useEffect(() => {
    const timer = setInterval(() => {
      setSeconds((s) => {
        if (s <= 1) {
          clearInterval(timer)
          close()
          return 0
        }
        return s - 1
      })
    }, 1000)
    return () => clearInterval(timer)
  }, [close])

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
            window.open(finalUrl, "_blank")
            close()
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
  )
}

function Footer() {
  return (
    <footer>
      <div className="newsletter shell">
        <div>
          <Badge tone="rose">GIFTFINDER LETTERS</Badge>
          <h2>Quà hay, deal tốt — gửi thẳng đến bạn.</h2>
          <p>Nhận cảm hứng tặng quà và ưu đãi độc quyền mỗi tuần.</p>
        </div>
        <label>
          <input placeholder="Email của bạn" />
          <Button variant="rose">Đăng ký ngay</Button>
        </label>
      </div>
      <div className="footer-main shell">
        <div className="footer-brand">
          <Logo />
          <p>
            Nền tảng khám phá quà tặng thông minh, kết nối bạn với những món quà
            ý nghĩa từ các gian hàng uy tín.
          </p>
          <Badge tone="green">✓ ĐÃ THÔNG BÁO BỘ CÔNG THƯƠNG</Badge>
        </div>
        {[
          [
            "Khám phá",
            "Quà cho nàng",
            "Quà cho chàng",
            "Theo dịp lễ",
            "Tìm quà bằng AI",
          ],
          [
            "Hỗ trợ",
            "Trung tâm trợ giúp",
            "Theo dõi đơn hàng",
            "Chính sách đổi trả",
            "Liên hệ",
          ],
          [
            "Dành cho đối tác",
            "Bán hàng cùng GiftFinder",
            "Affiliate Program",
            "Featured Placement",
            "Merchant Center",
          ],
        ].map((col) => (
          <div className="footer-col" key={col[0]}>
            <b>{col[0]}</b>
            {col.slice(1).map((x) => (
              <button key={x}>{x}</button>
            ))}
          </div>
        ))}
      </div>
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
          <b>Lazada</b>
        </div>
        <small>© 2025 GiftFinder Vietnam. All rights reserved.</small>
      </div>
    </footer>
  )
}

export default function App() {
  const validPages: Page[] = [
    "home",
    "catalog",
    "product",
    "wishlist",
    "merchant",
    "auth",
    "cart",
    "checkout",
    "success",
  ]

  const [page, setPage] = useState<Page>(() => {
    if (typeof window !== "undefined") {
      const cleanPath = window.location.pathname.replace(/^\/+/, "") as Page
      if (cleanPath && validPages.includes(cleanPath)) {
        return cleanPath
      }
    }
    return "home"
  })

  const [affiliate, setAffiliate] = useState(false)
  const [affiliateProduct, setAffiliateProduct] = useState<ProductItem | null>(null)
  const [affiliateUrl, setAffiliateUrl] = useState<string>("")
  const [toastMsg, setToastMsg] = useState<string | null>(null)
  const [selectedProduct, setSelectedProduct] = useState<ProductItem | null>(null)
  const [productsList, setProductsList] = useState<ProductItem[]>(defaultProducts)
  const [_loadingProducts, setLoadingProducts] = useState(false)

  const go = (next: Page) => {
    setPage(next)
    window.scrollTo({ top: 0, behavior: "smooth" })
    const path = next === "home" ? "/" : `/${next}`
    if (typeof window !== "undefined" && window.location.pathname !== path) {
      window.history.pushState({ page: next }, "", path)
    }
  }

  // Quản lý nút Quay lại (Back) và Tiếp tục (Forward) trên trình duyệt Edge / Chrome
  useEffect(() => {
    const handlePopState = (e: PopStateEvent) => {
      if (e.state?.page && validPages.includes(e.state.page)) {
        setPage(e.state.page)
      } else {
        const cleanPath = window.location.pathname.replace(/^\/+/, "") as Page
        setPage(validPages.includes(cleanPath) ? cleanPath : "home")
      }
      window.scrollTo({ top: 0, behavior: "smooth" })
    }

    // Đánh dấu state ban đầu để khi bấm Back không bao giờ bị thoát hẳn ra khỏi Edge
    const initialPath = window.location.pathname.replace(/^\/+/, "") || "home"
    if (!window.history.state) {
      window.history.replaceState({ page: initialPath }, "", window.location.pathname)
    }

    window.addEventListener("popstate", handlePopState)
    return () => window.removeEventListener("popstate", handlePopState)
  }, [])

  // Tải 244 sản phẩm thật từ Database PostgreSQL qua Backend API (cân bằng cả Shopee và TikTok Shop)
  useEffect(() => {
    let isCancelled = false
    setLoadingProducts(true)

    Promise.all([
      productService.getProducts({ source: 0, pageSize: 30 }), // 116 sản phẩm Shopee trong DB
      productService.getProducts({ source: 1, pageSize: 30 }), // 124 sản phẩm TikTok Shop trong DB
    ])
      .then(([shopeeRes, tiktokRes]) => {
        if (isCancelled) return

        const shopeeItems = (shopeeRes?.items || []).map(mapDtoToProductItem)
        const tiktokItems = (tiktokRes?.items || []).map(mapDtoToProductItem)

        // Trộn xen kẽ đều đặn: 1 sản phẩm Shopee, 1 sản phẩm TikTok Shop
        const mixed: ProductItem[] = []
        const maxLen = Math.max(shopeeItems.length, tiktokItems.length)
        for (let i = 0; i < maxLen; i++) {
          if (shopeeItems[i]) mixed.push(shopeeItems[i])
          if (tiktokItems[i]) mixed.push(tiktokItems[i])
        }

        if (mixed.length > 0) {
          setProductsList(mixed)
        }
      })
      .catch((err) => {
        console.warn("API /Products fallback to preset catalog:", err)
      })
      .finally(() => {
        if (!isCancelled) setLoadingProducts(false)
      })

    return () => {
      isCancelled = true
    }
  }, [])

  const handleOpenAffiliate = (item?: ProductItem) => {
    const target = item || productsList[0] || defaultProducts[0]
    const p = (target.platform || "").toUpperCase()

    let platformName = "Shopee"
    let defaultStoreUrl = "https://shopee.vn"

    if (p.includes("TIKTOK")) {
      platformName = "TikTok Shop"
      defaultStoreUrl = "https://shop.tiktok.com"
    } else if (p.includes("LAZADA")) {
      platformName = "Lazada"
      defaultStoreUrl = "https://www.lazada.vn"
    } else if (p.includes("TIKI")) {
      platformName = "Tiki"
      defaultStoreUrl = "https://tiki.vn"
    } else if (p.includes("SHOPEE")) {
      platformName = "Shopee"
      defaultStoreUrl = "https://shopee.vn"
    } else {
      platformName = "đối tác"
    }

    const targetUrl = target.affiliateUrl || target.originalUrl || defaultStoreUrl

    // Mở trực tiếp sang sàn Shopee / TikTok Shop / Lazada trong tab mới ngay khi click
    try {
      window.open(targetUrl, "_blank", "noopener,noreferrer")
    } catch (e) {
      console.warn("Popup error:", e)
    }

    // Ghi nhận click tracking ngầm trong backend
    if (target.id) {
      productService.trackAffiliateClick(target.id).catch(console.warn)
    }

    setAffiliateProduct(target)
    setAffiliateUrl(targetUrl)
    setAffiliate(false)
    setToastMsg(`Đã mở gian hàng ${platformName} trong tab mới!`)
    setTimeout(() => setToastMsg(null), 3000)
  }

  return (
    <>
      {page !== "auth" && <Header go={go} />}
      {page === "home" && (
        <Home
          go={go}
          products={productsList}
          openAffiliate={handleOpenAffiliate}
          onSelectProduct={(p) => {
            setSelectedProduct(p)
            go("product")
          }}
        />
      )}{" "}
      {page === "catalog" && (
        <Catalog
          go={go}
          products={productsList}
          openAffiliate={handleOpenAffiliate}
          onSelectProduct={(p) => {
            setSelectedProduct(p)
            go("product")
          }}
        />
      )}{" "}
      {page === "product" && (
        <ProductDetail
          go={go}
          product={selectedProduct}
          products={productsList}
          openAffiliate={handleOpenAffiliate}
          onSelectProduct={(p) => setSelectedProduct(p)}
        />
      )}{" "}
      {page === "wishlist" && (
        <Wishlist
          go={go}
          products={productsList}
          openAffiliate={handleOpenAffiliate}
          onSelectProduct={(p) => {
            setSelectedProduct(p)
            go("product")
          }}
        />
      )}{" "}
      {page === "merchant" && <MerchantPage go={go} />}
      {page === "admin" && <AdminConsole go={go} />}
      {page === "auth" && <AuthPage go={go} />}{" "}
      {page === "cart" && <Cart go={go} />}{" "}
      {page === "checkout" && <Checkout go={go} />}{" "}
      {page === "success" && <Success go={go} />}{" "}
      {affiliate && (
        <AffiliateModal
          close={() => {
            setAffiliate(false)
            setAffiliateUrl("")
            setAffiliateProduct(null)
          }}
          product={affiliateProduct}
          targetUrl={affiliateUrl}
        />
      )}
      {toastMsg && (
        <div
          style={{
            position: "fixed",
            bottom: 24,
            left: 24,
            background: "#111827",
            color: "#fff",
            padding: "12px 20px",
            borderRadius: 12,
            display: "flex",
            alignItems: "center",
            gap: 12,
            boxShadow: "0 10px 30px rgba(0,0,0,0.25)",
            zIndex: 9999,
            fontSize: 13,
            fontWeight: 600,
            border: "1px solid rgba(255,255,255,0.1)",
          }}
        >
          <span>{toastMsg}</span>
          <button
            onClick={() => setToastMsg(null)}
            style={{
              background: "none",
              border: "none",
              color: "#94a3b8",
              cursor: "pointer",
              fontSize: 14,
              padding: 0,
            }}
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
          onSelectProduct={(p) => {
            setSelectedProduct(p)
            go("product")
          }}
        />
      )}
    </>
  )
}
