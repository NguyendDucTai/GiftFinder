import React, { useEffect, useState, useMemo } from 'react';
import { useAuth } from '../../auth';
import {
  merchantService,
  type MerchantDashboardStats,
  type MerchantProductItem,
  type MerchantProfile,
  type MerchantOrder,
  type TagDtoItem,
  type BuyFeaturedResponse,
} from '../services/merchantService';
import { getHighResImageUrl } from '../../../App';

type PortalTab = 'overview' | 'orders' | 'products' | 'editor' | 'featured' | 'profile';

const PROVINCE_OPTIONS = [
  'Hà Nội',
  'TP. Hồ Chí Minh',
  'Đà Nẵng',
  'Hải Phòng',
  'Cần Thơ',
  'Bình Dương',
  'Đồng Nai',
  'Khánh Hòa',
  'Thừa Thiên Huế',
  'Quảng Ninh',
  'Bắc Ninh',
  'Nghệ An',
  'Thanh Hóa',
  'Lâm Đồng',
];

const CARRIER_OPTIONS = [
  'Giao Hàng Nhanh (GHN)',
  'Giao Hàng Tiết Kiệm (GHTK)',
  'Viettel Post',
  'Ahamove Hỏa Tốc 2H',
  'GrabExpress Nội Thành',
  'J&T Express',
];

const ZODIAC_OPTIONS = [
  { id: 'Aries', label: 'Bạch Dương', symbol: '♈', dates: '21/3 - 19/4' },
  { id: 'Taurus', label: 'Kim Ngưu', symbol: '♉', dates: '20/4 - 20/5' },
  { id: 'Gemini', label: 'Song Tử', symbol: '♊', dates: '21/5 - 21/6' },
  { id: 'Cancer', label: 'Cự Giải', symbol: '♋', dates: '22/6 - 22/7' },
  { id: 'Leo', label: 'Sư Tử', symbol: '♌', dates: '23/7 - 22/8' },
  { id: 'Virgo', label: 'Xử Nữ', symbol: '♍', dates: '23/8 - 22/9' },
  { id: 'Libra', label: 'Thiên Bình', symbol: '♎', dates: '23/9 - 23/10' },
  { id: 'Scorpio', label: 'Bọ Cạp', symbol: '♏', dates: '24/10 - 21/11' },
  { id: 'Sagittarius', label: 'Nhân Mã', symbol: '♐', dates: '22/11 - 21/12' },
  { id: 'Capricorn', label: 'Ma Kết', symbol: '♑', dates: '22/12 - 19/1' },
  { id: 'Aquarius', label: 'Bảo Bình', symbol: '♒', dates: '20/1 - 18/2' },
  { id: 'Pisces', label: 'Song Ngư', symbol: '♓', dates: '19/2 - 20/3' },
];

const SAMPLE_GIFT_IMAGES = [
  {
    label: 'Nến thơm & Hoa khô',
    url: 'https://images.unsplash.com/photo-1603006905003-be475563bc59?auto=format&fit=crop&w=900&q=80',
  },
  {
    label: 'Hộp quà Cao cấp',
    url: 'https://images.unsplash.com/photo-1549465220-1a8b9238cd48?auto=format&fit=crop&w=900&q=80',
  },
  {
    label: 'Ly gốm Thủ công',
    url: 'https://images.unsplash.com/photo-1514228742587-6b1558fcca3d?auto=format&fit=crop&w=900&q=80',
  },
  {
    label: 'Tinh dầu Khuếch tán',
    url: 'https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?auto=format&fit=crop&w=900&q=80',
  },
  {
    label: 'Set Trà & Mật ong',
    url: 'https://images.unsplash.com/photo-1544787219-7f47ccb76574?auto=format&fit=crop&w=900&q=80',
  },
];

interface MerchantPortalProps {
  go: (page: any) => void;
  onNotify: (msg: string) => void;
}

export const MerchantPortal: React.FC<MerchantPortalProps> = ({ go, onNotify }) => {
  const {
    user,
    isAuthenticated,
    isMerchant,
    login,
    registerMerchant,
    upgradeToMerchant,
    logout,
  } = useAuth();

  // State cho Onboarding / Đăng ký / Đăng nhập Đối tác khi chưa có quyền Merchant
  const [authMode, setAuthMode] = useState<'register-shop' | 'login-shop'>('register-shop');
  const [obShopName, setObShopName] = useState('');
  const [obFullName, setObFullName] = useState('');
  const [obPhone, setObPhone] = useState('');
  const [obEmail, setObEmail] = useState('');
  const [obPassword, setObPassword] = useState('');
  const [obLicense] = useState('');
  const [obProvince, setObProvince] = useState('Hà Nội');
  const [obAddress, setObAddress] = useState('');
  const [obLoading, setObLoading] = useState(false);
  const [obError, setObError] = useState<string | null>(null);

  // State cho Kênh Quản Lý Gian Hàng (Merchant Seller Center)
  const [activeTab, setActiveTab] = useState<PortalTab>('overview');
  const [chartDays, setChartDays] = useState<7 | 30>(7);
  const [stats, setStats] = useState<MerchantDashboardStats | null>(null);
  const [profile, setProfile] = useState<MerchantProfile | null>(null);
  const [products, setProducts] = useState<MerchantProductItem[]>([]);
  const [orders, setOrders] = useState<MerchantOrder[]>([]);
  const [orderStatusFilter, setOrderStatusFilter] = useState<string>('ALL');
  const [allTags, setAllTags] = useState<TagDtoItem[]>([]);
  const [loadingPortal, setLoadingPortal] = useState(false);
  const [needsShopSetup, setNeedsShopSetup] = useState(false);
  const [showQrModal, setShowQrModal] = useState(false);

  // State Modal nhập Mã vận đơn khi giao cho Shipper
  const [shippingModalOrder, setShippingModalOrder] = useState<MerchantOrder | null>(null);
  const [selectedCarrier, setSelectedCarrier] = useState(CARRIER_OPTIONS[0]);
  const [trackingCodeInput, setTrackingCodeInput] = useState('');
  const [updatingOrderId, setUpdatingOrderId] = useState<string | null>(null);

  // State đóng/mở thanh quản lý bên trái (Left Sidebar)
  const [sidebarOpen, setSidebarOpen] = useState(() => {
    try {
      const saved = localStorage.getItem('mp_sidebar_open');
      return saved !== null ? saved === 'true' : true;
    } catch {
      return true;
    }
  });

  const toggleSidebar = () => {
    setSidebarOpen((prev) => {
      const next = !prev;
      try {
        localStorage.setItem('mp_sidebar_open', String(next));
      } catch {
        // ignore
      }
      return next;
    });
  };

  // Bộ lọc tab Quản lý sản phẩm
  const [statusFilter, setStatusFilter] = useState<string>('ALL');
  const [searchKeyword, setSearchKeyword] = useState('');
  const [productSort, setProductSort] = useState<'newest' | 'price-desc' | 'clicks-desc'>('newest');
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);

  // State cho Form Đăng / Chỉnh sửa sản phẩm (Tab 'editor')
  const [editingProductId, setEditingProductId] = useState<string | null>(null);
  const [prodName, setProdName] = useState('');
  const [prodPrice, setProdPrice] = useState('');
  const [prodOrigPrice, setProdOrigPrice] = useState('');
  const [prodStockQty, setProdStockQty] = useState('50');
  const [prodImageUrl, setProdImageUrl] = useState('');
  const [prodDesc, setProdDesc] = useState('');
  const [selectedTagIds, setSelectedTagIds] = useState<string[]>([]);
  const [customTags, setCustomTags] = useState<string[]>([]);
  const [customTagInput, setCustomTagInput] = useState('');
  const [selectedZodiacs, setSelectedZodiacs] = useState<string[]>([]);
  const [aiRationale, setAiRationale] = useState<string | null>(null);
  const [aiSuggesting, setAiSuggesting] = useState(false);
  const [savingProduct, setSavingProduct] = useState(false);

  // State cho Quảng cáo Đẩy Top Featured (Tab 'featured' & Modal VNPAY)
  const [selectedPlanType, setSelectedPlanType] = useState<0 | 1>(0);
  const [featuredPendingTx, setFeaturedPendingTx] = useState<BuyFeaturedResponse | null>(null);
  const [featuredLoadingId, setFeaturedLoadingId] = useState<string | null>(null);
  const [simulatingPay, setSimulatingPay] = useState(false);

  // State cho Form Hồ sơ Gian hàng (Tab 'profile')
  const [profShopName, setProfShopName] = useState('');
  const [profEmail, setProfEmail] = useState('');
  const [profPhone, setProfPhone] = useState('');
  const [profLicense, setProfLicense] = useState('');
  const [profProvince, setProfProvince] = useState('Hà Nội');
  const [profAddress, setProfAddress] = useState('');
  const [savingProfile, setSavingProfile] = useState(false);

  useEffect(() => {
    if (user?.email && !obEmail) {
      setObEmail(user.email);
    }
    if (user?.fullName && !obFullName) {
      setObFullName(user.fullName);
    }
  }, [user]);

  // Hàm tải toàn bộ dữ liệu gian hàng từ Backend
  const loadMerchantPortalData = async (daysParam: 7 | 30 = chartDays) => {
    if (!isAuthenticated) return;
    setLoadingPortal(true);
    setNeedsShopSetup(false);
    try {
      const [profileRes, statsRes, productsRes, ordersRes, tagsRes] = await Promise.all([
        merchantService.getProfile().catch((err) => {
          if (err?.response?.status === 404 || err?.response?.status === 403) {
            setNeedsShopSetup(true);
          }
          return null;
        }),
        merchantService.getDashboardStats(daysParam).catch(() => null),
        merchantService.getMyProducts(1, 100).catch(() => ({ items: [], totalCount: 0, pageNumber: 1, pageSize: 100 })),
        merchantService.getOrders().catch(() => []),
        merchantService.getTags().catch(() => []),
      ]);

      if (profileRes) {
        setProfile(profileRes);
        setProfShopName(profileRes.shopName || '');
        setProfEmail(profileRes.contactEmail || '');
        setProfPhone(profileRes.contactPhone || '');
        setProfLicense(profileRes.businessLicense || '');
        setProfProvince(profileRes.pickupProvince || 'Hà Nội');
        setProfAddress(profileRes.pickupAddress || '');
      }
      if (statsRes) {
        setStats(statsRes);
      }
      if (productsRes?.items) {
        setProducts(productsRes.items);
      }
      if (Array.isArray(ordersRes)) {
        setOrders(ordersRes);
      }
      if (Array.isArray(tagsRes)) {
        setAllTags(tagsRes);
      }
    } finally {
      setLoadingPortal(false);
    }
  };

  useEffect(() => {
    if (isAuthenticated && isMerchant) {
      loadMerchantPortalData(chartDays);
    }
  }, [isAuthenticated, isMerchant]);

  const handleChangeChartDays = async (days: 7 | 30) => {
    setChartDays(days);
    try {
      const statsRes = await merchantService.getDashboardStats(days);
      if (statsRes) setStats(statsRes);
    } catch {
      // ignore
    }
  };

  // ============================================================================
  // XỬ LÝ ĐƠN HÀNG (TAB 'ORDERS')
  // ============================================================================
  const handleQuickUpdateOrderStatus = async (
    order: MerchantOrder,
    targetStatus: 'Packing' | 'Shipping' | 'Completed' | 'Cancelled',
    carrier?: string,
    tracking?: string
  ) => {
    if (targetStatus === 'Cancelled') {
      const confirmed = window.confirm(
        `Bạn có chắc chắn muốn hủy đơn hàng ${order.orderCode} của khách ${order.receiverName}?`
      );
      if (!confirmed) return;
    }

    setUpdatingOrderId(order.id);
    try {
      await merchantService.updateOrderStatus(order.id, {
        targetStatus,
        shippingCarrier: carrier,
        trackingCode: tracking,
      });

      const statusMsg =
        targetStatus === 'Packing'
          ? `📦 Đã xác nhận đơn ${order.orderCode} & chuyển sang Đang gói quà!`
          : targetStatus === 'Shipping'
            ? `🚚 Đã bàn giao đơn ${order.orderCode} cho ${carrier || 'ĐVVC'}!`
            : targetStatus === 'Completed'
              ? `🎉 Đơn hàng ${order.orderCode} đã giao thành công! Đã cộng +${order.totalAmount.toLocaleString('vi-VN')}đ vào doanh thu Shop!`
              : `Đã hủy đơn hàng ${order.orderCode}.`;

      onNotify(statusMsg);
      setShippingModalOrder(null);
      await loadMerchantPortalData(chartDays);
    } catch (err: any) {
      onNotify(
        `⚠️ ${
          err?.response?.data?.message || 'Không thể cập nhật trạng thái đơn hàng.'
        }`
      );
    } finally {
      setUpdatingOrderId(null);
    }
  };

  const openShippingModal = (order: MerchantOrder) => {
    setShippingModalOrder(order);
    setSelectedCarrier(
      order.shippingMethod === 'Express2H' ? 'Ahamove Hỏa Tốc 2H' : CARRIER_OPTIONS[0]
    );
    setTrackingCodeInput(
      order.trackingCode ||
        `${order.shippingMethod === 'Express2H' ? 'AHA' : 'GHN'}-${Math.floor(
          100000 + Math.random() * 900000
        )}`
    );
  };

  // ============================================================================
  // XỬ LÝ ĐĂNG KÝ / ĐĂNG NHẬP / NÂNG CẤP TÀI KHOẢN ĐỐI TÁC (ONBOARDING)
  // ============================================================================
  const handleQuickDemoLogin = async () => {
    setObError(null);
    setObLoading(true);
    try {
      await login({ email: 'shopdemo@giftfinder.vn', password: 'Password@123' }, true);
      onNotify('🎉 Đã đăng nhập vào Gian hàng mẫu: Tiệm Gốm Mộc!');
    } catch (err: any) {
      setObError(
        err?.response?.data?.message ||
          err?.response?.data?.Message ||
          'Không thể đăng nhập tài khoản Demo. Vui lòng thử lại.'
      );
    } finally {
      setObLoading(false);
    }
  };

  const handleGuestPartnerSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setObError(null);

    if (authMode === 'login-shop') {
      if (!obEmail.trim() || !obPassword) {
        setObError('Vui lòng nhập đầy đủ Email và Mật khẩu.');
        return;
      }
      setObLoading(true);
      try {
        const res = await login({ email: obEmail.trim(), password: obPassword }, true);
        if (res.role === 'Merchant') {
          onNotify(`🎉 Chào mừng chủ gian hàng quay trở lại!`);
        } else {
          onNotify(`👋 Xin chào ${res.fullName}! Hãy điền tên Shop để kích hoạt gian hàng đối tác.`);
        }
      } catch (err: any) {
        setObError(
          err?.response?.data?.message ||
            err?.response?.data?.Message ||
            err?.message ||
            'Email hoặc mật khẩu không chính xác.'
        );
      } finally {
        setObLoading(false);
      }
      return;
    }

    if (!obShopName.trim()) {
      setObError('Vui lòng nhập Tên gian hàng / Thương hiệu quà tặng.');
      return;
    }
    if (!obFullName.trim()) {
      setObError('Vui lòng nhập Họ và tên người đại diện.');
      return;
    }
    if (!obPhone.trim() || !/^[0-9+]{9,15}$/.test(obPhone.trim())) {
      setObError('Số điện thoại liên hệ phải từ 9 - 15 chữ số.');
      return;
    }
    if (!obEmail.trim()) {
      setObError('Vui lòng nhập địa chỉ Email đăng nhập.');
      return;
    }
    if (!obPassword || obPassword.length < 6) {
      setObError('Mật khẩu phải từ 6 ký tự trở lên.');
      return;
    }

    setObLoading(true);
    try {
      await registerMerchant(
        {
          email: obEmail.trim(),
          password: obPassword,
          fullName: obFullName.trim(),
          shopName: obShopName.trim(),
          contactPhone: obPhone.trim(),
          businessLicense: obLicense.trim() || undefined,
        },
        true
      );
      onNotify(`🎉 Chúc mừng! Gian hàng "${obShopName.trim()}" đã được khởi tạo thành công!`);
    } catch (err: any) {
      setObError(
        err?.response?.data?.message ||
          err?.response?.data?.Message ||
          err?.message ||
          'Đăng ký gian hàng thất bại. Vui lòng kiểm tra lại thông tin.'
      );
    } finally {
      setObLoading(false);
    }
  };

  const handleUpgradeMemberToMerchant = async (e: React.FormEvent) => {
    e.preventDefault();
    setObError(null);
    if (!obShopName.trim()) {
      setObError('Vui lòng nhập Tên gian hàng / Thương hiệu quà tặng.');
      return;
    }
    if (!obPhone.trim() || !/^[0-9+]{9,15}$/.test(obPhone.trim())) {
      setObError('Số điện thoại liên hệ phải từ 9 - 15 chữ số.');
      return;
    }

    setObLoading(true);
    try {
      const res = await merchantService.register({
        shopName: obShopName.trim(),
        contactPhone: obPhone.trim(),
        contactEmail: obEmail.trim() || user?.email,
        businessLicense: obLicense.trim() || undefined,
        pickupProvince: obProvince,
        pickupAddress: obAddress.trim() || undefined,
      });
      if (res?.token) {
        upgradeToMerchant(res.token, res.shopName || obShopName.trim());
      }
      setNeedsShopSetup(false);
      onNotify(`🎉 Đã nâng cấp tài khoản lên Đối tác và mở gian hàng "${res.shopName}" thành công!`);
      await loadMerchantPortalData(chartDays);
    } catch (err: any) {
      setObError(
        err?.response?.data?.message ||
          err?.response?.data?.Message ||
          err?.message ||
          'Không thể đăng ký gian hàng. Vui lòng kiểm tra lại.'
      );
    } finally {
      setObLoading(false);
    }
  };

  // ============================================================================
  // XỬ LÝ FORM ĐĂNG QUÀ MỚI / SỬA QUÀ & TRỢ LÝ AI (TAB 'EDITOR')
  // ============================================================================
  const resetEditorForm = () => {
    setEditingProductId(null);
    setProdName('');
    setProdPrice('');
    setProdOrigPrice('');
    setProdStockQty('50');
    setProdImageUrl('');
    setProdDesc('');
    setSelectedTagIds([]);
    setCustomTags([]);
    setCustomTagInput('');
    setSelectedZodiacs([]);
    setAiRationale(null);
  };

  const handleStartCreateNew = () => {
    resetEditorForm();
    setActiveTab('editor');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleStartEditProduct = async (item: MerchantProductItem) => {
    setEditingProductId(item.id);
    setProdName(item.name || '');
    setProdPrice(String(item.price || ''));
    setProdOrigPrice(item.originalPrice ? String(item.originalPrice) : '');
    setProdStockQty(String(item.stockQuantity ?? 50));
    setProdImageUrl(item.imageUrl || '');
    setProdDesc(item.description || '');
    setSelectedTagIds(item.tagIds || []);
    setCustomTags([]);
    setSelectedZodiacs(item.suitableZodiacs || []);
    setAiRationale(null);
    setActiveTab('editor');
    window.scrollTo({ top: 0, behavior: 'smooth' });

    try {
      const detail = await merchantService.getProductById(item.id);
      if (detail) {
        setProdName(detail.name || item.name);
        setProdPrice(String(detail.price || item.price));
        setProdOrigPrice(detail.originalPrice ? String(detail.originalPrice) : '');
        setProdStockQty(String(detail.stockQuantity ?? item.stockQuantity ?? 50));
        setProdImageUrl(detail.imageUrl || item.imageUrl || '');
        setProdDesc(detail.description || '');
        if (detail.tagIds) setSelectedTagIds(detail.tagIds);
        if (detail.suitableZodiacs) setSelectedZodiacs(detail.suitableZodiacs);
      }
    } catch {
      // ignore
    }
  };

  const handleAiSuggestTags = async () => {
    const cleanName = prodName.trim();
    const numericPrice = parseInt(prodPrice.replace(/\D/g, ''), 10) || 250000;
    if (!cleanName) {
      onNotify('⚠️ Vui lòng nhập Tên món quà trước khi nhờ AI phân tích!');
      return;
    }

    setAiSuggesting(true);
    setAiRationale(null);
    try {
      const suggestion = await merchantService.suggestTagsAi({
        productName: cleanName,
        description: prodDesc.trim() || undefined,
        price: numericPrice,
      });

      const updatedTags = await merchantService.getTags().catch(() => allTags);
      if (updatedTags.length > 0) {
        setAllTags(updatedTags);
      }

      if (suggestion.suggestedTagIds && suggestion.suggestedTagIds.length > 0) {
        setSelectedTagIds((prev) => Array.from(new Set([...prev, ...suggestion.suggestedTagIds])));
      }
      if (suggestion.suitableZodiacs && suggestion.suitableZodiacs.length > 0) {
        setSelectedZodiacs(suggestion.suitableZodiacs);
      }
      if (suggestion.zodiacRationale) {
        setAiRationale(suggestion.zodiacRationale);
      }
      onNotify(
        `✨ AI đã gợi ý ${suggestion.suggestedTags?.length || 0} thẻ phân loại & ${
          suggestion.suitableZodiacs?.length || 0
        } cung hoàng đạo phù hợp!`
      );
    } catch (err: any) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        'AI phát hiện sản phẩm chưa phù hợp tiêu chí quà tặng hoặc đang bận.';
      onNotify(`⚠️ ${msg}`);
    } finally {
      setAiSuggesting(false);
    }
  };

  const toggleTagId = (id: string) => {
    setSelectedTagIds((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
    );
  };

  const toggleZodiac = (zodiacId: string) => {
    setSelectedZodiacs((prev) =>
      prev.includes(zodiacId) ? prev.filter((z) => z !== zodiacId) : [...prev, zodiacId]
    );
  };

  const handleAddCustomTag = () => {
    const clean = customTagInput.trim();
    if (!clean) return;
    if (!customTags.includes(clean)) {
      setCustomTags((prev) => [...prev, clean]);
    }
    setCustomTagInput('');
  };

  const handleSaveProduct = async (e: React.FormEvent) => {
    e.preventDefault();
    const priceNum = parseInt(prodPrice.replace(/\D/g, ''), 10);
    const origPriceNum = prodOrigPrice
      ? parseInt(prodOrigPrice.replace(/\D/g, ''), 10)
      : undefined;
    const stockNum = parseInt(prodStockQty.replace(/\D/g, ''), 10);

    if (!prodName.trim()) {
      onNotify('⚠️ Vui lòng nhập tên món quà tặng.');
      return;
    }
    if (!priceNum || priceNum <= 0) {
      onNotify('⚠️ Giá bán phải lớn hơn 0đ.');
      return;
    }
    if (origPriceNum !== undefined && origPriceNum < priceNum) {
      onNotify('⚠️ Giá gốc trước giảm phải lớn hơn hoặc bằng giá bán hiện tại.');
      return;
    }
    if (prodImageUrl.trim() && !/^https?:\/\//i.test(prodImageUrl.trim())) {
      onNotify('⚠️ Đường dẫn ảnh sản phẩm phải là URL hợp lệ (http:// hoặc https://).');
      return;
    }

    setSavingProduct(true);
    try {
      const payload = {
        name: prodName.trim(),
        description: prodDesc.trim() || undefined,
        price: priceNum,
        originalPrice: origPriceNum,
        stockQuantity: isNaN(stockNum) ? 50 : Math.max(0, stockNum),
        imageUrl: prodImageUrl.trim() || SAMPLE_GIFT_IMAGES[0].url,
        tagIds: selectedTagIds,
        customTagNames: customTags,
        suitableZodiacs: selectedZodiacs,
      };

      if (editingProductId) {
        await merchantService.updateProduct(editingProductId, payload);
        onNotify('✓ Đã cập nhật thông tin món quà! Sản phẩm đã được gửi chờ Admin duyệt lại.');
      } else {
        await merchantService.createProduct(payload);
        onNotify('🎉 Đăng món quà mới thành công! Khách hàng sẽ đặt mua trực tiếp trên GiftFinder sau khi Admin duyệt.');
      }

      resetEditorForm();
      await loadMerchantPortalData(chartDays);
      setActiveTab('products');
    } catch (err: any) {
      onNotify(
        `⚠️ ${
          err?.response?.data?.message ||
          err?.response?.data?.Message ||
          'Lưu sản phẩm thất bại. Vui lòng kiểm tra lại dữ liệu nhập.'
        }`
      );
    } finally {
      setSavingProduct(false);
    }
  };

  const handleDeleteProduct = async (productId: string) => {
    try {
      const res = await merchantService.deleteProduct(productId);
      onNotify(`✓ ${res?.message || 'Đã ẩn sản phẩm khỏi gian hàng thành công.'}`);
      setConfirmDeleteId(null);
      await loadMerchantPortalData(chartDays);
    } catch (err: any) {
      onNotify(
        `⚠️ ${
          err?.response?.data?.message || 'Không thể ẩn sản phẩm lúc này.'
        }`
      );
    }
  };

  // ============================================================================
  // XỬ LÝ QUẢNG CÁO ĐẨY TOP FEATURED & THANH TOÁN VNPAY (TAB 'FEATURED')
  // ============================================================================
  const handleInitiateBuyFeatured = async (product: MerchantProductItem, plan: 0 | 1 = selectedPlanType) => {
    if (product.status !== 'Approved') {
      onNotify('⚠️ Chỉ sản phẩm ở trạng thái "Đã duyệt" mới có thể đăng ký gói Quảng cáo Đẩy Top.');
      return;
    }
    setFeaturedLoadingId(product.id);
    try {
      const res = await merchantService.buyFeatured(product.id, plan);
      setFeaturedPendingTx(res);
      onNotify(`✓ Đã khởi tạo giao dịch VNPAY cho "${product.name}"!`);
    } catch (err: any) {
      onNotify(
        `⚠️ ${
          err?.response?.data?.message ||
          err?.response?.data?.Message ||
          'Không thể tạo giao dịch thanh toán VNPAY.'
        }`
      );
    } finally {
      setFeaturedLoadingId(null);
    }
  };

  const handleSimulateFeaturedPayment = async () => {
    if (!featuredPendingTx) return;
    setSimulatingPay(true);
    try {
      await merchantService.simulatePayment(
        featuredPendingTx.productId,
        featuredPendingTx.transactionId
      );
      onNotify(
        `🎉 Thanh toán thành công! Sản phẩm "${featuredPendingTx.productName}" đã được kích hoạt Đẩy Top Featured!`
      );
      setFeaturedPendingTx(null);
      await loadMerchantPortalData(chartDays);
    } catch (err: any) {
      onNotify(
        `⚠️ ${
          err?.response?.data?.message || 'Giả lập thanh toán thất bại.'
        }`
      );
    } finally {
      setSimulatingPay(false);
    }
  };

  const handleCancelFeatured = async (productId: string, productName: string) => {
    setFeaturedLoadingId(productId);
    try {
      const res = await merchantService.cancelFeatured(productId);
      onNotify(`✓ ${res?.message || `Đã hủy gói nổi bật cho "${productName}".`}`);
      await loadMerchantPortalData(chartDays);
    } catch (err: any) {
      onNotify(
        `⚠️ ${
          err?.response?.data?.message || 'Không thể hủy gói nổi bật.'
        }`
      );
    } finally {
      setFeaturedLoadingId(null);
    }
  };

  // ============================================================================
  // XỬ LÝ CẬP NHẬT HỒ SƠ GIAN HÀNG (TAB 'PROFILE')
  // ============================================================================
  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!profShopName.trim()) {
      onNotify('⚠️ Tên gian hàng không được để trống.');
      return;
    }
    if (!profEmail.trim()) {
      onNotify('⚠️ Email liên hệ không được để trống.');
      return;
    }
    if (!profPhone.trim() || !/^[0-9+]{9,15}$/.test(profPhone.trim())) {
      onNotify('⚠️ Số điện thoại liên hệ phải từ 9 - 15 chữ số.');
      return;
    }

    setSavingProfile(true);
    try {
      const updated = await merchantService.updateProfile({
        shopName: profShopName.trim(),
        contactEmail: profEmail.trim(),
        contactPhone: profPhone.trim(),
        businessLicense: profLicense.trim() || undefined,
        pickupProvince: profProvince,
        pickupAddress: profAddress.trim(),
      });
      setProfile(updated);
      await loadMerchantPortalData(chartDays);
      onNotify(`✓ Đã lưu Hồ sơ Gian hàng & Địa chỉ kho (${profProvince}) thành công!`);
    } catch (err: any) {
      onNotify(
        `⚠️ ${
          err?.response?.data?.message ||
          err?.response?.data?.Message ||
          'Cập nhật hồ sơ thất bại.'
        }`
      );
    } finally {
      setSavingProfile(false);
    }
  };

  // Lọc và sắp xếp danh sách sản phẩm
  const filteredProducts = useMemo(() => {
    return products
      .filter((p) => {
        if (statusFilter !== 'ALL' && p.status !== statusFilter) return false;
        if (searchKeyword.trim()) {
          const kw = searchKeyword.toLowerCase();
          const inName = p.name.toLowerCase().includes(kw);
          const inTags = (p.tagNames || []).some((t) => t.toLowerCase().includes(kw));
          return inName || inTags;
        }
        return true;
      })
      .sort((a, b) => {
        if (productSort === 'price-desc') return b.price - a.price;
        if (productSort === 'clicks-desc') return (b.clickCount || 0) - (a.clickCount || 0);
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      });
  }, [products, statusFilter, searchKeyword, productSort]);

  // Lọc danh sách đơn hàng
  const filteredOrders = useMemo(() => {
    if (orderStatusFilter === 'ALL') return orders;
    return orders.filter((o) => o.status === orderStatusFilter);
  }, [orders, orderStatusFilter]);

  // Phân nhóm Tags theo loại
  const groupedTags = useMemo(() => {
    const recipients = allTags.filter(
      (t) => t.typeName === 'Recipient' || t.type === 0 || t.type === 'Recipient'
    );
    const occasions = allTags.filter(
      (t) => t.typeName === 'Occasion' || t.type === 1 || t.type === 'Occasion'
    );
    const hobbies = allTags.filter(
      (t) =>
        t.typeName === 'Hobby' ||
        t.typeName === 'Category' ||
        t.type === 2 ||
        t.type === 3 ||
        t.type === 'Hobby' ||
        t.type === 'Category'
    );
    return { recipients, occasions, hobbies };
  }, [allTags]);

  // ============================================================================
  // MÀN HÌNH 1: PARTNER ONBOARDING & REGISTRATION (DÀNH CHO KHÁCH HOẶC MEMBER)
  // ============================================================================
  if (!isAuthenticated || !isMerchant || needsShopSetup) {
    return (
      <div className="mp-onboarding-page">
        <div className="mp-onboarding-container">
          <section className="mp-onboarding-pitch">
            <div className="mp-pitch-badge">✨ HỆ SINH THÁI GIAN HÀNG QUÀ TẶNG TRỰC TIẾP</div>
            <h1 className="mp-pitch-title">
              Mở Gian Hàng Quà Tặng & Nhận <em>Đơn Đặt Hàng Trực Tiếp</em> Ngay Trên GiftFinder
            </h1>
            <p className="mp-pitch-subtitle">
              Khác với sàn thương mại điện tử thu phí 15%, <strong>GiftFinder</strong> giúp các xưởng quà thủ công, tiệm nến thơm, gốm sứ, hoa tươi nhận đơn đặt hàng COD trực tiếp (kèm lời chúc thiệp & gói quà) mà không cần dẫn khách đi nơi khác.
            </p>

            <div className="mp-benefit-grid">
              <div className="mp-benefit-card">
                <div className="mp-benefit-icon">🛒</div>
                <div>
                  <h3>Khách Đặt Mua Trực Tiếp Trên Web</h3>
                  <p>
                    Người mua chọn số lượng, điền địa chỉ nhận quà, viết thiệp chúc mừng và đặt đơn COD nổ thẳng về Kênh Người Bán.
                  </p>
                </div>
              </div>

              <div className="mp-benefit-card">
                <div className="mp-benefit-icon">⚡</div>
                <div>
                  <h3>Tự Động Tính Ship Nội Tỉnh & Hỏa Tốc 2H</h3>
                  <p>
                    Hệ thống tự động đối chiếu Tỉnh kho của Shop và Tỉnh khách nhận để bật Giao Hỏa Tốc 2H (50k) hoặc Liên Tỉnh (35k).
                  </p>
                </div>
              </div>

              <div className="mp-benefit-card">
                <div className="mp-benefit-icon">🚀</div>
                <div>
                  <h3>Đẩy Top Featured Ra Đơn Thật</h3>
                  <p>
                    Chiếm vị trí ưu tiên đầu trang Catalog và gợi ý AI chỉ từ 49.000đ/tuần để tăng trực tiếp số đơn đặt hàng.
                  </p>
                </div>
              </div>

              <div className="mp-benefit-card">
                <div className="mp-benefit-icon">📈</div>
                <div>
                  <h3>Biểu Đồ Tăng Trưởng Doanh Thu Thực</h3>
                  <p>
                    Theo dõi biểu đồ tăng trưởng lượt Click, Đơn hàng, tỷ lệ chuyển đổi CVR và doanh thu COD theo từng ngày.
                  </p>
                </div>
              </div>
            </div>

            <div className="mp-pitch-metrics">
              <div>
                <strong>0%</strong>
                <span>Phí chiết khấu đơn hàng COD</span>
              </div>
              <div className="mp-pitch-divider" />
              <div>
                <strong>0đ</strong>
                <span>Phí mở & duy trì Shop</span>
              </div>
              <div className="mp-pitch-divider" />
              <div>
                <strong>100%</strong>
                <span>Đơn hàng thật kèm lời chúc thiệp</span>
              </div>
            </div>
          </section>

          <section className="mp-onboarding-form-card">
            {!isAuthenticated ? (
              <>
                <div className="mp-auth-tabs">
                  <button
                    type="button"
                    className={authMode === 'register-shop' ? 'active' : ''}
                    onClick={() => {
                      setAuthMode('register-shop');
                      setObError(null);
                    }}
                  >
                    🏪 Đăng ký Mở Gian Hàng
                  </button>
                  <button
                    type="button"
                    className={authMode === 'login-shop' ? 'active' : ''}
                    onClick={() => {
                      setAuthMode('login-shop');
                      setObError(null);
                    }}
                  >
                    🔑 Đăng nhập Kênh Shop
                  </button>
                </div>

                <div className="mp-demo-quick-box">
                  <div className="mp-demo-quick-info">
                    <span>⚡ <strong>Trải nghiệm ngay Kênh Người Bán?</strong></span>
                    <small>Dùng tài khoản Đối tác mẫu đã có sẵn biểu đồ & đơn hàng thực tế</small>
                  </div>
                  <button
                    type="button"
                    className="mp-demo-quick-btn"
                    onClick={handleQuickDemoLogin}
                    disabled={obLoading}
                  >
                    Vào Shop Mẫu 1-Click →
                  </button>
                </div>

                {obError && <div className="mp-alert-error">⚠️ {obError}</div>}

                <form onSubmit={handleGuestPartnerSubmit} className="mp-form-stack">
                  {authMode === 'register-shop' ? (
                    <>
                      <div className="mp-form-header">
                        <h2>Đăng Ký Đối Tác Bán Hàng Mới</h2>
                        <p>Khởi tạo tài khoản Đối tác (`Merchant`) và mở gian hàng chỉ trong 1 bước.</p>
                      </div>

                      <label className="mp-field">
                        <span>Tên gian hàng / Thương hiệu quà tặng *</span>
                        <input
                          value={obShopName}
                          onChange={(e) => setObShopName(e.target.value)}
                          placeholder="VD: Tiệm Nến Thơm & Quà Thủ Công"
                          required
                        />
                      </label>

                      <div className="mp-field-row">
                        <label className="mp-field">
                          <span>Họ tên người đại diện *</span>
                          <input
                            value={obFullName}
                            onChange={(e) => setObFullName(e.target.value)}
                            placeholder="Nguyễn Minh Anh"
                            required
                          />
                        </label>
                        <label className="mp-field">
                          <span>Số điện thoại / Zalo *</span>
                          <input
                            value={obPhone}
                            onChange={(e) => setObPhone(e.target.value)}
                            placeholder="0909123456"
                            required
                          />
                        </label>
                      </div>

                      <div className="mp-field-row">
                        <label className="mp-field">
                          <span>Email đăng nhập *</span>
                          <input
                            type="email"
                            value={obEmail}
                            onChange={(e) => setObEmail(e.target.value)}
                            placeholder="shop@thuonghieu.vn"
                            required
                          />
                        </label>
                        <label className="mp-field">
                          <span>Tỉnh / Thành phố kho hàng *</span>
                          <select
                            value={obProvince}
                            onChange={(e) => setObProvince(e.target.value)}
                            style={{
                              padding: '11px 14px',
                              borderRadius: 10,
                              border: '1px solid #cbd5e1',
                              fontSize: 14,
                            }}
                          >
                            {PROVINCE_OPTIONS.map((prov) => (
                              <option key={prov} value={prov}>
                                {prov}
                              </option>
                            ))}
                          </select>
                        </label>
                      </div>

                      <label className="mp-field">
                        <span>Mật khẩu bảo mật (tối thiểu 6 ký tự) *</span>
                        <input
                          type="password"
                          value={obPassword}
                          onChange={(e) => setObPassword(e.target.value)}
                          placeholder="••••••••"
                          required
                        />
                      </label>

                      <button type="submit" className="mp-primary-submit-btn" disabled={obLoading}>
                        {obLoading ? 'Đang khởi tạo gian hàng...' : '🚀 Hoàn Tất & Mở Gian Hàng Ngay'}
                      </button>
                    </>
                  ) : (
                    <>
                      <div className="mp-form-header">
                        <h2>Đăng Nhập Quản Lý Gian Hàng</h2>
                        <p>Dành cho Đối tác đã có tài khoản trên hệ thống GiftFinder.</p>
                      </div>

                      <label className="mp-field">
                        <span>Email tài khoản Đối tác *</span>
                        <input
                          type="email"
                          value={obEmail}
                          onChange={(e) => setObEmail(e.target.value)}
                          placeholder="shopdemo@giftfinder.vn"
                          required
                        />
                      </label>

                      <label className="mp-field">
                        <span>Mật khẩu *</span>
                        <input
                          type="password"
                          value={obPassword}
                          onChange={(e) => setObPassword(e.target.value)}
                          placeholder="••••••••"
                          required
                        />
                      </label>

                      <button type="submit" className="mp-primary-submit-btn" disabled={obLoading}>
                        {obLoading ? 'Đang đăng nhập...' : '🔑 Truy Cập Kênh Người Bán'}
                      </button>
                    </>
                  )}
                </form>
              </>
            ) : (
              <>
                <div className="mp-form-header">
                  <span className="mp-upgrade-pill">NÂNG CẤP TÀI KHOẢN ĐỐI TÁC</span>
                  <h2>Kích Hoạt Gian Hàng Cho Tài Khoản Của Bạn</h2>
                  <p>
                    Xin chào <strong>{user?.fullName}</strong> ({user?.email}). Hãy điền thông tin thương hiệu và địa chỉ kho bên dưới để nâng cấp lên <strong>Chủ Gian Hàng (Merchant)</strong>!
                  </p>
                </div>

                <div className="mp-demo-quick-box" style={{ marginBottom: 18 }}>
                  <div className="mp-demo-quick-info">
                    <span>⚡ <strong>Muốn xem sẵn dữ liệu mẫu?</strong></span>
                    <small>Chuyển sang tài khoản Shop Mẫu (Tiệm Gốm Mộc)</small>
                  </div>
                  <button
                    type="button"
                    className="mp-demo-quick-btn"
                    onClick={handleQuickDemoLogin}
                    disabled={obLoading}
                  >
                    Chuyển sang Shop Mẫu →
                  </button>
                </div>

                {obError && <div className="mp-alert-error">⚠️ {obError}</div>}

                <form onSubmit={handleUpgradeMemberToMerchant} className="mp-form-stack">
                  <label className="mp-field">
                    <span>Tên gian hàng / Thương hiệu quà tặng *</span>
                    <input
                      value={obShopName}
                      onChange={(e) => setObShopName(e.target.value)}
                      placeholder="VD: Tiệm Hoa & Quà Tặng Tinh Tế"
                      required
                    />
                  </label>

                  <div className="mp-field-row">
                    <label className="mp-field">
                      <span>Số điện thoại / Zalo CSKH *</span>
                      <input
                        value={obPhone}
                        onChange={(e) => setObPhone(e.target.value)}
                        placeholder="0901234567"
                        required
                      />
                    </label>
                    <label className="mp-field">
                      <span>Tỉnh / Thành phố kho lấy hàng *</span>
                      <select
                        value={obProvince}
                        onChange={(e) => setObProvince(e.target.value)}
                        style={{
                          padding: '11px 14px',
                          borderRadius: 10,
                          border: '1px solid #cbd5e1',
                          fontSize: 14,
                        }}
                      >
                        {PROVINCE_OPTIONS.map((prov) => (
                          <option key={prov} value={prov}>
                            {prov}
                          </option>
                        ))}
                      </select>
                    </label>
                  </div>

                  <label className="mp-field">
                    <span>Địa chỉ kho lấy hàng chi tiết</span>
                    <input
                      value={obAddress}
                      onChange={(e) => setObAddress(e.target.value)}
                      placeholder="Số nhà, đường, phường/xã, quận/huyện..."
                    />
                  </label>

                  <div style={{ display: 'flex', gap: 12, marginTop: 8 }}>
                    <button
                      type="button"
                      className="mp-secondary-btn"
                      onClick={() => go('home')}
                    >
                      ← Về trang chủ
                    </button>
                    <button
                      type="submit"
                      className="mp-primary-submit-btn"
                      style={{ flex: 1 }}
                      disabled={obLoading}
                    >
                      {obLoading ? 'Đang kích hoạt...' : '✨ Kích Hoạt Gian Hàng Ngay'}
                    </button>
                  </div>
                </form>
              </>
            )}
          </section>
        </div>
      </div>
    );
  }

  // ============================================================================
  // MÀN HÌNH 2: TRUNG TÂM QUẢN LÝ KÊNH NGƯỜI BÁN
  // ============================================================================
  const activeShopName =
    profile?.shopName || stats?.shopName || user?.shopName || 'Gian Hàng Đối Tác';
  const activePickupProvince =
    profile?.pickupProvince || stats?.pickupProvince || 'Hà Nội';
  const isVerifiedShop = profile?.isVerified ?? stats?.isVerified ?? true;

  const shopSlug =
    stats?.shopSlug ||
    activeShopName
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '') ||
    'tiem-gom-moc';
  const shopShareUrl = `${window.location.origin}/?shop=${shopSlug}`;

  const approvedCount =
    stats?.approvedProducts ?? products.filter((p) => p.status === 'Approved').length;
  const pendingCount =
    stats?.pendingProducts ?? products.filter((p) => p.status === 'Pending').length;
  const rejectedCount =
    stats?.rejectedProducts ?? products.filter((p) => p.status === 'Rejected').length;
  const inactiveCount = products.filter((p) => p.status === 'Inactive').length;
  const featuredCount =
    stats?.featuredProducts ?? products.filter((p) => p.isFeatured).length;

  const pendingOrdersCount =
    stats?.pendingOrders ??
    orders.filter((o) => o.status === 'Pending' || o.status === 'Packing').length;
  const actualPendingOrdersCount = orders.filter((o) => o.status === 'Pending').length;
  const packingOrdersCount = orders.filter((o) => o.status === 'Packing').length;
  const shippingOrdersCount =
    stats?.shippingOrders ?? orders.filter((o) => o.status === 'Shipping').length;
  const completedOrdersCount =
    stats?.completedOrders ?? orders.filter((o) => o.status === 'Completed').length;
  const totalOrdersCount =
    stats?.totalOrders ?? orders.filter((o) => o.status !== 'Cancelled').length;

  const completedRevenue =
    stats?.completedRevenue ??
    orders
      .filter((o) => o.status === 'Completed')
      .reduce((sum, o) => sum + o.totalAmount, 0);
  const pendingRevenue =
    stats?.pendingRevenue ??
    orders
      .filter((o) => o.status === 'Pending' || o.status === 'Packing' || o.status === 'Shipping')
      .reduce((sum, o) => sum + o.totalAmount, 0);
  const avgOrderValue =
    stats?.averageOrderValue ??
    (totalOrdersCount > 0 ? Math.round((completedRevenue + pendingRevenue) / totalOrdersCount) : 0);

  // Dữ liệu biểu đồ tăng trưởng theo ngày
  const chartPoints = stats?.dailyChart || [];
  const maxChartVal = Math.max(
    4,
    ...chartPoints.map((d) => Math.max(d.clicks, d.orders))
  );

  // Giá xem trước cho Live Preview trong Editor
  const previewPriceNum = parseInt(prodPrice.replace(/\D/g, ''), 10) || 350000;
  const previewOrigNum =
    parseInt(prodOrigPrice.replace(/\D/g, ''), 10) || Math.round(previewPriceNum * 1.25);
  const previewDisc =
    previewOrigNum > previewPriceNum
      ? Math.max(5, Math.round((1 - previewPriceNum / previewOrigNum) * 100))
      : 15;

  const renderOrderStatusBadge = (status: string) => {
    switch (status) {
      case 'Pending':
        return (
          <span
            style={{
              background: '#fef3c7',
              color: '#b45309',
              padding: '4px 10px',
              borderRadius: 999,
              fontSize: 12,
              fontWeight: 700,
            }}
          >
            ⏳ Chờ xác nhận
          </span>
        );
      case 'Packing':
        return (
          <span
            style={{
              background: '#e0e7ff',
              color: '#4338ca',
              padding: '4px 10px',
              borderRadius: 999,
              fontSize: 12,
              fontWeight: 700,
            }}
          >
            🎁 Đang gói quà & viết thiệp
          </span>
        );
      case 'Shipping':
        return (
          <span
            style={{
              background: '#dbeafe',
              color: '#1d4ed8',
              padding: '4px 10px',
              borderRadius: 999,
              fontSize: 12,
              fontWeight: 700,
            }}
          >
            🚚 Shipper đang giao
          </span>
        );
      case 'Completed':
        return (
          <span
            style={{
              background: '#dcfce7',
              color: '#15803d',
              padding: '4px 10px',
              borderRadius: 999,
              fontSize: 12,
              fontWeight: 700,
            }}
          >
            ✓ Đã giao & thu COD
          </span>
        );
      default:
        return (
          <span
            style={{
              background: '#fee2e2',
              color: '#b91c1c',
              padding: '4px 10px',
              borderRadius: 999,
              fontSize: 12,
              fontWeight: 700,
            }}
          >
            ✕ Đã hủy
          </span>
        );
    }
  };

  return (
    <div className="mp-portal-wrapper">
      {/* ====================================================================
          THANH ĐIỀU HƯỚNG CHUYÊN BIỆT CHO MERCHANT (SELLER CENTER TOP NAVBAR)
          ==================================================================== */}
      <header className="mp-seller-navbar">
        <div className="mp-seller-brand">
          <button
            type="button"
            className={`mp-hamburger-btn ${!sidebarOpen ? 'is-collapsed' : ''}`}
            onClick={toggleSidebar}
            title={sidebarOpen ? 'Thu gọn thanh bên (☰)' : 'Mở rộng thanh bên (☰)'}
            aria-label="Đóng mở thanh quản trị bên trái"
            style={{ width: 38, height: 38 }}
          >
            <span className="mp-hamburger-bar" />
            <span className="mp-hamburger-bar" />
            <span className="mp-hamburger-bar" />
          </button>
          <div
            className="mp-brand-logo-badge"
            onClick={() => setActiveTab('overview')}
            style={{ cursor: 'pointer' }}
          >
            <span style={{ fontSize: 22 }}>🎁</span>
            <span className="mp-brand-logo-text">EGift</span>
            <span className="mp-seller-tag">Kênh Người Bán</span>
          </div>
        </div>

        <div className="mp-seller-current-shop-pill">
          <span>🏪</span>
          <span className="shop-name">{activeShopName}</span>
          <span style={{ color: 'var(--eg-border)' }}>|</span>
          <span style={{ color: 'var(--eg-text-secondary)', fontSize: 12 }}>
            📍 Kho {activePickupProvince}
          </span>
          {isVerifiedShop && (
            <span style={{ color: '#059669', fontSize: 11, fontWeight: 700 }}>
              ✓ Đã xác minh
            </span>
          )}
        </div>

        <div className="mp-seller-navbar-actions">
          <button
            type="button"
            className="mp-btn-outline"
            style={{ padding: '7px 12px', fontSize: 12.5 }}
            onClick={() => go('home')}
            title="Quay lại trang mua sắm quà tặng"
          >
            🌐 Xem Website người mua
          </button>
          <button
            type="button"
            className="mp-btn-primary"
            style={{ padding: '7px 14px', fontSize: 12.5 }}
            onClick={handleStartCreateNew}
          >
            + Đăng quà mới
          </button>
          <button
            type="button"
            className="mp-btn-outline danger"
            style={{ padding: '7px 12px', fontSize: 12.5 }}
            onClick={() => {
              logout();
              go('home');
              onNotify('Đã đăng xuất khỏi Kênh Người Bán.');
            }}
            title="Đăng xuất tài khoản gian hàng"
          >
            🚪 Đăng xuất
          </button>
        </div>
      </header>

      <div className={`mp-portal-shell ${sidebarOpen ? '' : 'sidebar-closed'}`}>
        {/* ====================================================================
            THANH QUẢN LÝ BÊN TRÁI (LEFT SIDEBAR NAVIGATION)
            ==================================================================== */}
        <aside className={`mp-sidebar ${sidebarOpen ? 'open' : 'collapsed'}`}>
          <div className="mp-shop-identity">
            <div className="mp-shop-avatar">
              {activeShopName.charAt(0).toUpperCase()}
            </div>
            <div className="mp-shop-meta">
              <span className="mp-shop-role-tag">KÊNH ĐỐI TÁC · {activePickupProvince.toUpperCase()}</span>
              <h2 className="mp-shop-title" title={activeShopName}>
                {activeShopName}
              </h2>
              <div className={`mp-verify-badge ${isVerifiedShop ? 'verified' : 'unverified'}`}>
                <span className="mp-dot" />
                {isVerifiedShop ? 'Đối tác đã xác minh' : 'Đang hoạt động'}
              </div>
            </div>
            <button
              type="button"
              className="mp-sidebar-toggle-btn in-sidebar"
              onClick={toggleSidebar}
              title="Thu gọn thanh bên (☰)"
              aria-label="Thu gọn thanh bên"
            >
              <span className="mp-hamburger-bar" />
              <span className="mp-hamburger-bar" />
              <span className="mp-hamburger-bar" />
            </button>
          </div>

          <nav className="mp-sidebar-nav" aria-label="Menu quản lý gian hàng">
            <div className="mp-nav-group-label">QUẢN TRỊ GIAN HÀNG</div>

            <button
              type="button"
              className={`mp-nav-item ${activeTab === 'overview' ? 'active' : ''}`}
              onClick={() => setActiveTab('overview')}
            >
              <span className="mp-nav-icon">📊</span>
              <span className="mp-nav-text">Tổng quan hiệu suất</span>
            </button>

            <button
              type="button"
              className={`mp-nav-item ${activeTab === 'orders' ? 'active' : ''}`}
              onClick={() => setActiveTab('orders')}
            >
              <span className="mp-nav-icon">📦</span>
              <span className="mp-nav-text">Quản lý đơn hàng</span>
              {pendingOrdersCount > 0 ? (
                <span
                  className="mp-nav-count"
                  style={{ background: '#e11d48', color: '#fff' }}
                >
                  {pendingOrdersCount} mới
                </span>
              ) : (
                <span className="mp-nav-count">{orders.length}</span>
              )}
            </button>

            <button
              type="button"
              className={`mp-nav-item ${activeTab === 'products' ? 'active' : ''}`}
              onClick={() => setActiveTab('products')}
            >
              <span className="mp-nav-icon">🎁</span>
              <span className="mp-nav-text">Quản lý sản phẩm</span>
              <span className="mp-nav-count">{products.length}</span>
            </button>

            <button
              type="button"
              className={`mp-nav-item ${activeTab === 'editor' ? 'active' : ''}`}
              onClick={handleStartCreateNew}
            >
              <span className="mp-nav-icon">✨</span>
              <span className="mp-nav-text">
                {editingProductId ? 'Chỉnh sửa món quà' : 'Đăng quà mới (AI)'}
              </span>
              <span className="mp-nav-ai-pill">AI</span>
            </button>

            <button
              type="button"
              className={`mp-nav-item ${activeTab === 'featured' ? 'active' : ''}`}
              onClick={() => setActiveTab('featured')}
            >
              <span className="mp-nav-icon">🚀</span>
              <span className="mp-nav-text">Quảng cáo Đẩy Top</span>
              {featuredCount > 0 && (
                <span className="mp-nav-count featured">{featuredCount}</span>
              )}
            </button>

            <button
              type="button"
              className={`mp-nav-item ${activeTab === 'profile' ? 'active' : ''}`}
              onClick={() => setActiveTab('profile')}
            >
              <span className="mp-nav-icon">⚙️</span>
              <span className="mp-nav-text">Hồ sơ & Kho hàng</span>
            </button>
          </nav>

          <div className="mp-sidebar-mini-stats">
            <div className="mp-mini-stat-row">
              <span>📍 Kho lấy hàng</span>
              <strong>{activePickupProvince}</strong>
            </div>
            <div className="mp-mini-stat-row">
              <span>🟢 Đang bán trên Web</span>
              <strong>{approvedCount} quà</strong>
            </div>
            <div className="mp-mini-stat-row">
              <span>📦 Đơn chờ đóng gói</span>
              <strong style={{ color: pendingOrdersCount > 0 ? '#e11d48' : undefined }}>
                {pendingOrdersCount} đơn
              </strong>
            </div>
            {rejectedCount > 0 && (
              <div
                className="mp-mini-stat-row rejected"
                onClick={() => {
                  setStatusFilter('Rejected');
                  setActiveTab('products');
                }}
              >
                <span>🔴 Quà cần sửa lại</span>
                <strong>{rejectedCount} quà →</strong>
              </div>
            )}
          </div>

          <div className="mp-sidebar-footer">
            <button
              type="button"
              className="mp-sidebar-action-btn"
              onClick={() => loadMerchantPortalData(chartDays)}
              disabled={loadingPortal}
            >
              🔄 {loadingPortal ? 'Đang đồng bộ...' : 'Làm mới dữ liệu'}
            </button>
            <button
              type="button"
              className="mp-sidebar-action-btn"
              onClick={() => go('home')}
            >
              🌐 Xem mặt tiền Website
            </button>
            <button
              type="button"
              className="mp-sidebar-action-btn danger"
              onClick={() => {
                logout();
                go('home');
                onNotify('Đã đăng xuất khỏi Kênh Người Bán.');
              }}
            >
              🚪 Đăng xuất
            </button>
          </div>
        </aside>

        {sidebarOpen && (
          <div
            className="mp-sidebar-backdrop"
            onClick={toggleSidebar}
            aria-label="Đóng thanh quản lý"
          />
        )}

        {/* ====================================================================
            NỘI DUNG CHÍNH BÊN PHẢI (MAIN CONTENT AREA)
            ==================================================================== */}
        <main className={`mp-main-content ${sidebarOpen ? 'with-sidebar' : 'full-width'}`}>
          <header className="mp-topbar">
            <div className="mp-topbar-left">
              <button
                type="button"
                className={`mp-hamburger-btn ${!sidebarOpen ? 'is-collapsed' : ''}`}
                onClick={toggleSidebar}
                title={sidebarOpen ? 'Thu gọn thanh bên trái (☰)' : 'Mở rộng thanh bên trái (☰)'}
                aria-label="Đóng mở thanh quản trị bên trái"
              >
                <span className="mp-hamburger-bar" />
                <span className="mp-hamburger-bar" />
                <span className="mp-hamburger-bar" />
              </button>
              <div className="mp-topbar-titles">
                <div className="mp-breadcrumb">
                  <span onClick={() => go('home')} style={{ cursor: 'pointer' }}>
                    Trang chủ
                  </span>
                  <span>/</span>
                  <span>Kênh Người Bán</span>
                  <span>/</span>
                  <strong>
                    {activeTab === 'overview' && 'Tổng quan hiệu suất'}
                    {activeTab === 'orders' && 'Quản lý đơn hàng COD'}
                    {activeTab === 'products' && 'Quản lý sản phẩm'}
                    {activeTab === 'editor' &&
                      (editingProductId ? 'Chỉnh sửa sản phẩm' : 'Đăng quà tặng mới')}
                    {activeTab === 'featured' && 'Quảng cáo Đẩy Top (VNPAY)'}
                    {activeTab === 'profile' && 'Hồ sơ & Địa chỉ Kho hàng'}
                  </strong>
                </div>
                <h1 className="mp-page-title">
                  {activeTab === 'overview' && `Tổng quan: ${activeShopName}`}
                  {activeTab === 'orders' && `Đơn Đặt Quà Trực Tiếp (${orders.length} đơn)`}
                  {activeTab === 'products' && 'Kho Quà Tặng Của Gian Hàng'}
                  {activeTab === 'editor' &&
                    (editingProductId
                      ? 'Chỉnh Sửa Thông Tin Món Quà'
                      : 'Đăng Quà Tặng Bán Trực Tiếp Trên GiftFinder')}
                  {activeTab === 'featured' && 'Dịch Vụ Đẩy Top & Hiển Thị Nổi Bật'}
                  {activeTab === 'profile' && 'Hồ Sơ Định Danh & Địa Chỉ Kho Lấy Hàng'}
                </h1>
              </div>
            </div>

            <div className="mp-topbar-actions">
              <button
                type="button"
                className="mp-btn-outline"
                onClick={() => setActiveTab('orders')}
              >
                📦 Đơn hàng ({pendingOrdersCount} chờ giao)
              </button>
              <button
                type="button"
                className="mp-btn-primary"
                onClick={handleStartCreateNew}
              >
                + Đăng quà tặng mới
              </button>
            </div>
          </header>

          {/* ==================================================================
              PHÂN KHU 1: TỔNG QUAN HIỆU SUẤT GIAN HÀNG ('overview' - MẪU EGIFT)
              ================================================================== */}
          {activeTab === 'overview' && (
            <div className="mp-tab-panel">
              {/* KHỐI 1: VIỆC CẦN XỬ LÝ HÔM NAY (TO-DO LIST) */}
              <section className="mp-todo-section">
                <div className="mp-todo-header">
                  <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                    <span style={{ fontSize: 20 }}>⚡</span>
                    <div>
                      <h2 style={{ margin: 0, fontSize: 16, fontWeight: 800, color: 'var(--eg-text-primary)' }}>
                        Việc Cần Xử Lý Hôm Nay
                      </h2>
                      <p style={{ margin: '2px 0 0', fontSize: 12.5, color: 'var(--eg-text-secondary)' }}>
                        Theo dõi nhanh các tác vụ đơn hàng và sản phẩm cần xử lý ngay
                      </p>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={() => loadMerchantPortalData(chartDays)}
                    disabled={loadingPortal}
                    style={{
                      background: 'none',
                      border: 'none',
                      color: 'var(--eg-accent)',
                      fontSize: 13,
                      fontWeight: 700,
                      cursor: 'pointer',
                      display: 'flex',
                      alignItems: 'center',
                      gap: 4,
                    }}
                  >
                    🔄 {loadingPortal ? 'Đang tải...' : 'Làm mới'}
                  </button>
                </div>

                <div className="mp-todo-grid">
                  <div
                    className={`mp-todo-item ${actualPendingOrdersCount > 0 ? 'urgent' : ''}`}
                    onClick={() => {
                      setOrderStatusFilter('Pending');
                      setActiveTab('orders');
                    }}
                    role="button"
                    tabIndex={0}
                    title="Nhấn để xem danh sách đơn chờ xác nhận"
                  >
                    <div
                      className="mp-todo-num"
                      style={{
                        color: actualPendingOrdersCount > 0 ? 'var(--eg-accent)' : 'var(--eg-text-primary)',
                      }}
                    >
                      {actualPendingOrdersCount}
                    </div>
                    <div className="mp-todo-label">⏳ Chờ xác nhận</div>
                    <div className="mp-todo-sub">Đơn khách mới đặt COD</div>
                  </div>

                  <div
                    className={`mp-todo-item ${packingOrdersCount > 0 ? 'urgent' : ''}`}
                    onClick={() => {
                      setOrderStatusFilter('Packing');
                      setActiveTab('orders');
                    }}
                    role="button"
                    tabIndex={0}
                    title="Nhấn để xem đơn đang đóng gói"
                  >
                    <div
                      className="mp-todo-num"
                      style={{
                        color: packingOrdersCount > 0 ? '#b45309' : 'var(--eg-text-primary)',
                      }}
                    >
                      {packingOrdersCount}
                    </div>
                    <div className="mp-todo-label">🎁 Đang gói quà</div>
                    <div className="mp-todo-sub">Chuẩn bị giao Shipper</div>
                  </div>

                  <div
                    className="mp-todo-item"
                    onClick={() => {
                      setOrderStatusFilter('Shipping');
                      setActiveTab('orders');
                    }}
                    role="button"
                    tabIndex={0}
                    title="Nhấn để xem đơn đang vận chuyển"
                  >
                    <div className="mp-todo-num" style={{ color: '#059669' }}>
                      {shippingOrdersCount}
                    </div>
                    <div className="mp-todo-label">🚚 Đang vận chuyển</div>
                    <div className="mp-todo-sub">Shipper đang giao khách</div>
                  </div>

                  <div
                    className={`mp-todo-item ${rejectedCount > 0 ? 'urgent' : ''}`}
                    onClick={() => {
                      setStatusFilter('Rejected');
                      setActiveTab('products');
                    }}
                    role="button"
                    tabIndex={0}
                    title="Nhấn để xem sản phẩm cần chỉnh sửa"
                  >
                    <div
                      className="mp-todo-num"
                      style={{
                        color: rejectedCount > 0 ? '#dc2626' : 'var(--eg-text-primary)',
                      }}
                    >
                      {rejectedCount}
                    </div>
                    <div className="mp-todo-label">⚠️ Quà cần chỉnh sửa</div>
                    <div className="mp-todo-sub">Bị từ chối / cần bổ sung</div>
                  </div>
                </div>
              </section>

              {/* KHỐI 2: CHỈ SỐ KINH DOANH CỐT LÕI (4 THẺ GỌN GÀNG) */}
              <div className="mp-kpi-heading-row">
                <h2 style={{ fontSize: 16, fontWeight: 800, color: 'var(--eg-text-primary)', margin: 0 }}>
                  📊 Chỉ Số Hiệu Suất Gian Hàng
                </h2>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <button
                    type="button"
                    onClick={() => handleChangeChartDays(7)}
                    style={{
                      padding: '6px 12px',
                      borderRadius: 6,
                      border: chartDays === 7 ? '1px solid var(--eg-accent)' : '1px solid var(--eg-border)',
                      background: chartDays === 7 ? 'var(--eg-surface-strong)' : '#ffffff',
                      color: chartDays === 7 ? 'var(--eg-accent)' : 'var(--eg-text-secondary)',
                      fontWeight: 700,
                      fontSize: 12.5,
                      cursor: 'pointer',
                    }}
                  >
                    7 ngày qua
                  </button>
                  <button
                    type="button"
                    onClick={() => handleChangeChartDays(30)}
                    style={{
                      padding: '6px 12px',
                      borderRadius: 6,
                      border: chartDays === 30 ? '1px solid var(--eg-accent)' : '1px solid var(--eg-border)',
                      background: chartDays === 30 ? 'var(--eg-surface-strong)' : '#ffffff',
                      color: chartDays === 30 ? 'var(--eg-accent)' : 'var(--eg-text-secondary)',
                      fontWeight: 700,
                      fontSize: 12.5,
                      cursor: 'pointer',
                    }}
                  >
                    30 ngày qua
                  </button>
                </div>
              </div>

              <section className="mp-kpi-grid-4">
                <article className="mp-kpi-card" onClick={() => setActiveTab('orders')}>
                  <div className="mp-kpi-top">
                    <span className="mp-kpi-icon" style={{ background: '#fdf8f0', color: 'var(--eg-accent)' }}>💰</span>
                    <span className="mp-kpi-pill success">Đã thu COD</span>
                  </div>
                  <div className="mp-kpi-value" style={{ color: 'var(--eg-accent)' }}>
                    {completedRevenue.toLocaleString('vi-VN')}đ
                  </div>
                  <div className="mp-kpi-label">Doanh thu hoàn tất</div>
                  <div className="mp-kpi-sub">
                    <span>• Đã giao thành công</span>
                    <b>{completedOrdersCount} đơn</b>
                  </div>
                </article>

                <article className="mp-kpi-card" onClick={() => setActiveTab('orders')}>
                  <div className="mp-kpi-top">
                    <span className="mp-kpi-icon" style={{ background: '#fef3c7', color: '#b45309' }}>🚚</span>
                    <span className="mp-kpi-pill warning">Chờ thu COD</span>
                  </div>
                  <div className="mp-kpi-value" style={{ color: '#b45309' }}>
                    {pendingRevenue.toLocaleString('vi-VN')}đ
                  </div>
                  <div className="mp-kpi-label">Doanh số đang giao</div>
                  <div className="mp-kpi-sub">
                    <span>• Chờ Shipper đối soát</span>
                    <b>{pendingOrdersCount + shippingOrdersCount} đơn</b>
                  </div>
                </article>

                <article className="mp-kpi-card" onClick={() => setActiveTab('orders')}>
                  <div className="mp-kpi-top">
                    <span className="mp-kpi-icon" style={{ background: 'var(--eg-surface-strong)', color: 'var(--eg-text-primary)' }}>📦</span>
                    <span className="mp-kpi-pill neutral">Đơn trực tiếp</span>
                  </div>
                  <div className="mp-kpi-value" style={{ color: 'var(--eg-text-primary)' }}>
                    {totalOrdersCount}
                  </div>
                  <div className="mp-kpi-label">Tổng đơn hàng COD</div>
                  <div className="mp-kpi-sub">
                    <span>• Giá trị trung bình (AOV)</span>
                    <b>{avgOrderValue.toLocaleString('vi-VN')}đ</b>
                  </div>
                </article>

                <article className="mp-kpi-card" onClick={() => setActiveTab('products')}>
                  <div className="mp-kpi-top">
                    <span className="mp-kpi-icon" style={{ background: 'var(--eg-surface-strong)', color: 'var(--eg-text-primary)' }}>🎁</span>
                    <span className="mp-kpi-pill neutral">Kho quà</span>
                  </div>
                  <div className="mp-kpi-value" style={{ color: 'var(--eg-text-primary)' }}>
                    {approvedCount} <small style={{ fontSize: 13, fontWeight: 500, color: 'var(--eg-text-secondary)' }}>/ {products.length} món</small>
                  </div>
                  <div className="mp-kpi-label">Quà đang hiển thị</div>
                  <div className="mp-kpi-sub">
                    <span>• Lượt xem gian hàng</span>
                    <b>{stats?.totalClicks ?? 0} lượt</b>
                  </div>
                </article>
              </section>

              {/* KHỐI 3: HÀNG 2 CỘT CÂN ĐỐI (BIỂU ĐỒ DOANH THU & ĐƠN HÀNG + 5 ĐƠN HÀNG MỚI NHẤT) */}
              <div className="mp-overview-columns">
                {/* CỘT TRÁI: BIỂU ĐỒ DOANH THU & ĐƠN HÀNG */}
                <section className="mp-panel-card">
                  <div className="mp-panel-header">
                    <div>
                      <h2 style={{ fontSize: 15, fontWeight: 800, color: 'var(--eg-text-primary)', margin: 0 }}>
                        📈 Biến Động Đơn Hàng & Lượt Xem ({chartDays} ngày qua)
                      </h2>
                      <p style={{ margin: '3px 0 0', fontSize: 12, color: 'var(--eg-text-secondary)' }}>
                        Theo dõi lượng khách xem quà và đơn hàng phát sinh
                      </p>
                    </div>
                    <div style={{ display: 'flex', gap: 12, alignItems: 'center', fontSize: 12, fontWeight: 700 }}>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 5, color: 'var(--eg-text-primary)' }}>
                        <i style={{ width: 14, height: 10, background: 'var(--eg-border)', borderRadius: 2, display: 'inline-block' }} />
                        Lượt xem
                      </span>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 5, color: 'var(--eg-accent)' }}>
                        <i style={{ width: 14, height: 10, background: 'var(--eg-accent)', borderRadius: 2, display: 'inline-block' }} />
                        Đơn hàng
                      </span>
                    </div>
                  </div>

                  <div
                    style={{
                      padding: '16px 8px 6px',
                      border: '1px solid var(--eg-border)',
                      borderRadius: 12,
                      background: '#ffffff',
                    }}
                  >
                    <div
                      style={{
                        display: 'grid',
                        gridTemplateColumns: `repeat(${Math.max(1, chartPoints.length)}, minmax(0, 1fr))`,
                        alignItems: 'end',
                        height: 200,
                        gap: chartDays === 7 ? 12 : 4,
                        padding: '10px 10px 0',
                        borderBottom: '1px solid var(--eg-border)',
                      }}
                    >
                      {chartPoints.map((pt) => {
                        const clickHeightPct = Math.max(
                          4,
                          Math.round((pt.clicks / maxChartVal) * 160)
                        );
                        const orderHeightPct = Math.max(
                          pt.orders > 0 ? 18 : 4,
                          Math.round((pt.orders / maxChartVal) * 160)
                        );
                        return (
                          <div
                            key={pt.fullDate}
                            title={`Ngày ${pt.dateLabel}: ${pt.clicks} Xem · ${pt.orders} Đơn (${pt.revenue.toLocaleString('vi-VN')}đ)`}
                            style={{
                              display: 'flex',
                              flexDirection: 'column',
                              alignItems: 'center',
                              justifyContent: 'flex-end',
                              height: '100%',
                            }}
                          >
                            {(pt.orders > 0 || pt.clicks > 0) && chartDays === 7 && (
                              <div
                                style={{
                                  fontSize: 10.5,
                                  fontWeight: 700,
                                  color: pt.orders > 0 ? 'var(--eg-accent)' : 'var(--eg-text-secondary)',
                                  marginBottom: 4,
                                  whiteSpace: 'nowrap',
                                }}
                              >
                                {pt.orders > 0 ? `${pt.orders} đơn` : `${pt.clicks} xem`}
                              </div>
                            )}
                            <div
                              style={{
                                display: 'flex',
                                alignItems: 'flex-end',
                                gap: chartDays === 7 ? 4 : 2,
                                width: '100%',
                                justifyContent: 'center',
                              }}
                            >
                              <div
                                style={{
                                  width: chartDays === 7 ? 16 : 6,
                                  height: clickHeightPct,
                                  background: pt.clicks > 0 ? '#dfc8a8' : '#f5ebe0',
                                  borderRadius: '4px 4px 0 0',
                                  transition: 'height 0.3s ease',
                                }}
                              />
                              <div
                                style={{
                                  width: chartDays === 7 ? 16 : 6,
                                  height: orderHeightPct,
                                  background: pt.orders > 0 ? 'var(--eg-accent)' : '#eedec7',
                                  borderRadius: '4px 4px 0 0',
                                  transition: 'height 0.3s ease',
                                }}
                              />
                            </div>
                          </div>
                        );
                      })}
                    </div>

                    <div
                      style={{
                        display: 'grid',
                        gridTemplateColumns: `repeat(${Math.max(1, chartPoints.length)}, minmax(0, 1fr))`,
                        padding: '8px 10px 4px',
                        textAlign: 'center',
                      }}
                    >
                      {chartPoints.map((pt, i) => (
                        <span
                          key={pt.fullDate}
                          style={{
                            fontSize: chartDays === 7 ? 12 : 10,
                            color: 'var(--eg-text-secondary)',
                            fontWeight: 600,
                            display: chartDays === 30 && i % 3 !== 0 ? 'none' : 'block',
                          }}
                        >
                          {pt.dateLabel}
                        </span>
                      ))}
                    </div>
                  </div>
                </section>

                {/* CỘT PHẢI: BẢNG 5 ĐƠN HÀNG MỚI NHẤT */}
                <section className="mp-panel-card">
                  <div className="mp-panel-header">
                    <div>
                      <h2 style={{ fontSize: 15, fontWeight: 800, color: 'var(--eg-text-primary)', margin: 0 }}>
                        🛒 Đơn Hàng Mới Nhất
                      </h2>
                      <p style={{ margin: '3px 0 0', fontSize: 12, color: 'var(--eg-text-secondary)' }}>
                        Đơn khách đặt mua quà trực tiếp từ gian hàng
                      </p>
                    </div>
                    <button
                      type="button"
                      className="mp-link-btn"
                      onClick={() => setActiveTab('orders')}
                    >
                      Xem tất cả ({orders.length}) →
                    </button>
                  </div>

                  {orders.length > 0 ? (
                    <div style={{ overflowX: 'auto', flex: 1 }}>
                      <table
                        style={{
                          width: '100%',
                          borderCollapse: 'collapse',
                          fontSize: 13,
                        }}
                      >
                        <thead>
                          <tr
                            style={{
                              background: 'var(--eg-surface-strong)',
                              color: 'var(--eg-text-secondary)',
                              textAlign: 'left',
                              borderBottom: '1px solid var(--eg-border)',
                            }}
                          >
                            <th style={{ padding: '8px 6px', fontSize: 12 }}>Mã đơn</th>
                            <th style={{ padding: '8px 6px', fontSize: 12 }}>Khách nhận</th>
                            <th style={{ padding: '8px 6px', fontSize: 12 }}>Thu hộ COD</th>
                            <th style={{ padding: '8px 6px', fontSize: 12 }}>Trạng thái</th>
                          </tr>
                        </thead>
                        <tbody>
                          {orders.slice(0, 5).map((ord) => (
                            <tr
                              key={ord.id}
                              style={{
                                borderBottom: '1px solid var(--eg-surface-muted)',
                                cursor: 'pointer',
                              }}
                              onClick={() => setActiveTab('orders')}
                            >
                              <td style={{ padding: '10px 6px', fontWeight: 700, color: 'var(--eg-text-primary)' }}>
                                {ord.orderCode}
                                <div style={{ fontSize: 11, color: 'var(--eg-text-tertiary)', fontWeight: 500 }}>
                                  {new Date(ord.createdAt).toLocaleDateString('vi-VN')}
                                </div>
                              </td>
                              <td style={{ padding: '10px 6px' }}>
                                <strong style={{ color: 'var(--eg-text-primary)' }}>{ord.receiverName}</strong>
                                <div style={{ fontSize: 11.5, color: 'var(--eg-text-secondary)' }}>
                                  📍 {ord.receiverProvince}{' '}
                                  {ord.shippingMethod === 'Express2H' && (
                                    <span style={{ color: 'var(--eg-accent)', fontWeight: 700 }}>
                                      (⚡ Hỏa tốc 2H)
                                    </span>
                                  )}
                                </div>
                              </td>
                              <td style={{ padding: '10px 6px', fontWeight: 800, color: 'var(--eg-accent)' }}>
                                {ord.totalAmount.toLocaleString('vi-VN')}đ
                              </td>
                              <td style={{ padding: '10px 6px' }}>
                                {renderOrderStatusBadge(ord.status)}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <div className="mp-empty-box" style={{ padding: '24px 16px' }}>
                      <p style={{ color: 'var(--eg-text-secondary)', margin: 0 }}>Chưa có đơn đặt hàng nào.</p>
                    </div>
                  )}
                </section>
              </div>

              {/* KHỐI 4: LỐI TẮT NHANH (QUICK ACTIONS) & CHIA SẺ LINK GIAN HÀNG */}
              <section className="mp-quick-actions-bar">
                <div className="mp-share-chip">
                  <span style={{ fontSize: 13, fontWeight: 700, color: 'var(--eg-text-primary)' }}>
                    🔗 Link Gian Hàng:
                  </span>
                  <input
                    readOnly
                    value={shopShareUrl}
                    className="mp-share-input"
                  />
                  <button
                    type="button"
                    className="mp-btn-action"
                    onClick={() => {
                      navigator.clipboard?.writeText(shopShareUrl);
                      onNotify('📋 Đã sao chép link gian hàng vào bộ nhớ tạm!');
                    }}
                  >
                    📋 Sao chép
                  </button>
                  <button
                    type="button"
                    className="mp-btn-action"
                    onClick={() => setShowQrModal(true)}
                  >
                    📱 Lấy mã QR
                  </button>
                </div>

                <div className="mp-action-buttons-group">
                  <button
                    type="button"
                    className="mp-btn-egift primary"
                    onClick={handleStartCreateNew}
                  >
                    + Đăng quà tặng mới
                  </button>
                  <button
                    type="button"
                    className="mp-btn-egift outline"
                    onClick={() => setActiveTab('orders')}
                  >
                    📦 Quản lý đơn hàng
                  </button>
                  <button
                    type="button"
                    className="mp-btn-egift outline"
                    onClick={() => setActiveTab('featured')}
                  >
                    🚀 Quảng cáo Đẩy Top
                  </button>
                </div>
              </section>
            </div>
          )}

          {/* ==================================================================
              PHÂN KHU 2: QUẢN LÝ ĐƠN ĐẶT HÀNG TRỰC TIẾP ('orders')
              ================================================================== */}
          {activeTab === 'orders' && (
            <div className="mp-tab-panel">
              <section className="mp-panel-card">
                <div className="mp-products-toolbar">
                  <div className="mp-status-tabs">
                    {[
                      { id: 'ALL', label: 'Tất cả đơn', count: orders.length },
                      {
                        id: 'Pending',
                        label: '⏳ Chờ xác nhận',
                        count: orders.filter((o) => o.status === 'Pending').length,
                      },
                      {
                        id: 'Packing',
                        label: '🎁 Đang gói quà',
                        count: orders.filter((o) => o.status === 'Packing').length,
                      },
                      {
                        id: 'Shipping',
                        label: '🚚 Đang giao',
                        count: orders.filter((o) => o.status === 'Shipping').length,
                      },
                      {
                        id: 'Completed',
                        label: '✓ Đã giao thành công',
                        count: orders.filter((o) => o.status === 'Completed').length,
                      },
                      {
                        id: 'Cancelled',
                        label: '✕ Đã hủy',
                        count: orders.filter((o) => o.status === 'Cancelled').length,
                      },
                    ].map((tab) => (
                      <button
                        key={tab.id}
                        type="button"
                        className={`mp-status-tab ${orderStatusFilter === tab.id ? 'active' : ''}`}
                        onClick={() => setOrderStatusFilter(tab.id)}
                      >
                        {tab.label} <span>{tab.count}</span>
                      </button>
                    ))}
                  </div>
                </div>

                {filteredOrders.length > 0 ? (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                    {filteredOrders.map((ord) => (
                      <article
                        key={ord.id}
                        style={{
                          border: '1px solid #e2e8f0',
                          borderRadius: 16,
                          padding: 20,
                          background: '#ffffff',
                          boxShadow: '0 2px 8px rgba(15, 23, 42, 0.03)',
                        }}
                      >
                        {/* Dòng tiêu đề đơn hàng */}
                        <div
                          style={{
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'center',
                            flexWrap: 'wrap',
                            gap: 10,
                            paddingBottom: 14,
                            borderBottom: '1px solid #f1f5f9',
                            marginBottom: 14,
                          }}
                        >
                          <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
                            <strong style={{ fontSize: 16, color: '#0f172a' }}>
                              Đơn hàng #{ord.orderCode}
                            </strong>
                            {renderOrderStatusBadge(ord.status)}
                            <span
                              style={{
                                background:
                                  ord.shippingMethod === 'Express2H' ? '#ffe4e6' : '#f1f5f9',
                                color:
                                  ord.shippingMethod === 'Express2H' ? '#e11d48' : '#334155',
                                padding: '4px 10px',
                                borderRadius: 999,
                                fontSize: 12,
                                fontWeight: 700,
                              }}
                            >
                              {ord.shippingMethod === 'Express2H'
                                ? '⚡ Giao Hỏa Tốc Nội Thành 2H'
                                : '🚚 Giao Tiêu Chuẩn'}
                            </span>
                            <span
                              style={{
                                background: '#fef9c3',
                                color: '#854d0e',
                                padding: '4px 10px',
                                borderRadius: 999,
                                fontSize: 12,
                                fontWeight: 700,
                              }}
                            >
                              💵 Thanh toán COD (Thu hộ khi giao)
                            </span>
                          </div>
                          <span style={{ fontSize: 12.5, color: '#64748b' }}>
                            Đặt lúc: {new Date(ord.createdAt).toLocaleString('vi-VN')}
                          </span>
                        </div>

                        {/* Nội dung 2 cột: Sản phẩm & Quà tặng vs Địa chỉ giao & Hành động */}
                        <div
                          style={{
                            display: 'grid',
                            gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
                            gap: 20,
                          }}
                        >
                          {/* Cột trái: Danh sách món quà + Lời chúc thiệp + Che giá */}
                          <div>
                            <div style={{ marginBottom: 12 }}>
                              {ord.items.map((item) => (
                                <div
                                  key={item.id}
                                  style={{
                                    display: 'flex',
                                    gap: 12,
                                    alignItems: 'center',
                                    padding: '8px 0',
                                    borderBottom: '1px dashed #f1f5f9',
                                  }}
                                >
                                  <img
                                    src={getHighResImageUrl(
                                      item.productImageUrl || SAMPLE_GIFT_IMAGES[0].url
                                    )}
                                    alt={item.productName}
                                    style={{
                                      width: 56,
                                      height: 56,
                                      borderRadius: 10,
                                      objectFit: 'cover',
                                    }}
                                  />
                                  <div style={{ flex: 1 }}>
                                    <strong style={{ fontSize: 14, color: '#0f172a' }}>
                                      {item.productName}
                                    </strong>
                                    <div style={{ fontSize: 12.5, color: '#64748b' }}>
                                      Phân loại: {item.variant} · Số lượng: <b>x{item.quantity}</b>
                                    </div>
                                  </div>
                                  <strong style={{ fontSize: 14, color: '#0f172a' }}>
                                    {item.lineTotal.toLocaleString('vi-VN')}đ
                                  </strong>
                                </div>
                              ))}
                            </div>

                            {/* Khung yêu cầu Gói quà & Lời chúc viết lên thiệp */}
                            {(ord.hasGiftWrap || ord.giftMessage) && (
                              <div
                                style={{
                                  padding: '12px 14px',
                                  borderRadius: 12,
                                  background: '#fff1f2',
                                  border: '1px solid #fecdd3',
                                  marginBottom: 10,
                                }}
                              >
                                <div
                                  style={{
                                    fontSize: 12.5,
                                    fontWeight: 800,
                                    color: '#be123c',
                                    marginBottom: 4,
                                  }}
                                >
                                  🎀 YÊU CẦU GÓI QUÀ CAO CẤP & VIẾT THIỆP TAY (+30.000đ):
                                </div>
                                <p
                                  style={{
                                    margin: 0,
                                    fontSize: 13.5,
                                    fontStyle: 'italic',
                                    color: '#881337',
                                  }}
                                >
                                  “{ord.giftMessage || 'Chúc bạn luôn vui vẻ và hạnh phúc!'}”
                                </p>
                              </div>
                            )}

                            {/* Cảnh báo Che giá tiền trên hộp quà */}
                            {ord.hidePriceOnPackage && (
                              <div
                                style={{
                                  padding: '8px 12px',
                                  borderRadius: 8,
                                  background: '#fef2f2',
                                  border: '1px dashed #f87171',
                                  color: '#b91c1c',
                                  fontSize: 12.5,
                                  fontWeight: 700,
                                }}
                              >
                                🤫 ĐƠN TẶNG QUÀ BÍ MẬT: Shop vui lòng che giá sản phẩm trên nhãn dán bên ngoài hộp quà!
                              </div>
                            )}
                          </div>

                          {/* Cột phải: Thông tin người nhận + Vận đơn + Tổng thu COD */}
                          <div
                            style={{
                              background: '#f8fafc',
                              padding: 16,
                              borderRadius: 12,
                              display: 'flex',
                              flexDirection: 'column',
                              justifyContent: 'space-between',
                            }}
                          >
                            <div>
                              <div
                                style={{
                                  fontSize: 12.5,
                                  fontWeight: 800,
                                  color: '#475569',
                                  marginBottom: 6,
                                }}
                              >
                                📍 THÔNG TIN NGƯỜI NHẬN QUÀ:
                              </div>
                              <div style={{ fontSize: 14, fontWeight: 700, color: '#0f172a' }}>
                                {ord.receiverName} — 📞 {ord.receiverPhone}
                              </div>
                              <div style={{ fontSize: 13, color: '#334155', marginTop: 4 }}>
                                {ord.receiverAddress}, {ord.receiverDistrict},{' '}
                                <b>{ord.receiverProvince}</b>
                              </div>
                              {ord.deliveryNote && (
                                <div
                                  style={{
                                    fontSize: 12.5,
                                    color: '#d97706',
                                    marginTop: 6,
                                    fontWeight: 600,
                                  }}
                                >
                                  📝 Ghi chú giao hàng: {ord.deliveryNote}
                                </div>
                              )}

                              {ord.trackingCode && (
                                <div
                                  style={{
                                    marginTop: 10,
                                    padding: '8px 12px',
                                    background: '#eff6ff',
                                    borderRadius: 8,
                                    border: '1px solid #bfdbfe',
                                    fontSize: 12.5,
                                    color: '#1e40af',
                                  }}
                                >
                                  🚚 Đơn vị vận chuyển: <b>{ord.shippingCarrier}</b> · Mã vận đơn:{' '}
                                  <b>{ord.trackingCode}</b>
                                </div>
                              )}
                            </div>

                            <div
                              style={{
                                marginTop: 14,
                                paddingTop: 12,
                                borderTop: '1px solid #e2e8f0',
                                display: 'flex',
                                justifyContent: 'space-between',
                                alignItems: 'center',
                                flexWrap: 'wrap',
                                gap: 10,
                              }}
                            >
                              <div>
                                <span style={{ fontSize: 12, color: '#64748b', display: 'block' }}>
                                  Tổng tiền Shipper thu hộ (COD):
                                </span>
                                <strong style={{ fontSize: 18, color: '#e11d48' }}>
                                  {ord.totalAmount.toLocaleString('vi-VN')}đ
                                </strong>
                                <small style={{ fontSize: 11.5, color: '#64748b', marginLeft: 6 }}>
                                  (Ship: {ord.shippingFee.toLocaleString('vi-VN')}đ)
                                </small>
                              </div>

                              {/* Các nút xử lý đơn hàng tinh gọn cho Shop */}
                              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                                {ord.status === 'Pending' && (
                                  <>
                                    <button
                                      type="button"
                                      className="mp-btn-outline"
                                      disabled={updatingOrderId === ord.id}
                                      onClick={() =>
                                        handleQuickUpdateOrderStatus(ord, 'Packing')
                                      }
                                    >
                                      🎁 Chuẩn bị & Gói quà
                                    </button>
                                    <button
                                      type="button"
                                      className="mp-btn-primary"
                                      disabled={updatingOrderId === ord.id}
                                      onClick={() => openShippingModal(ord)}
                                    >
                                      🚚 Giao cho Shipper →
                                    </button>
                                  </>
                                )}

                                {ord.status === 'Packing' && (
                                  <button
                                    type="button"
                                    className="mp-btn-primary"
                                    disabled={updatingOrderId === ord.id}
                                    onClick={() => openShippingModal(ord)}
                                  >
                                    🚚 Giao cho Shipper (Nhập Mã VĐ) →
                                  </button>
                                )}

                                {ord.status === 'Shipping' && (
                                  <button
                                    type="button"
                                    style={{
                                      padding: '9px 16px',
                                      borderRadius: 10,
                                      border: 'none',
                                      background: '#16a34a',
                                      color: '#fff',
                                      fontWeight: 700,
                                      fontSize: 13,
                                      cursor: 'pointer',
                                    }}
                                    disabled={updatingOrderId === ord.id}
                                    onClick={() =>
                                      handleQuickUpdateOrderStatus(ord, 'Completed')
                                    }
                                  >
                                    ✅ Shipper báo: Đã giao xong (Cộng doanh thu)
                                  </button>
                                )}

                                {(ord.status === 'Pending' || ord.status === 'Packing') && (
                                  <button
                                    type="button"
                                    className="mp-action-btn delete"
                                    disabled={updatingOrderId === ord.id}
                                    onClick={() =>
                                      handleQuickUpdateOrderStatus(ord, 'Cancelled')
                                    }
                                  >
                                    ✕ Hủy
                                  </button>
                                )}
                              </div>
                            </div>
                          </div>
                        </div>
                      </article>
                    ))}
                  </div>
                ) : (
                  <div className="mp-empty-box">
                    <div style={{ fontSize: 40, marginBottom: 10 }}>📦</div>
                    <h3>Chưa có đơn hàng nào trong trạng thái này</h3>
                    <p>Khi khách hàng đặt mua quà của Shop, đơn hàng sẽ hiển thị ngay tại đây.</p>
                  </div>
                )}
              </section>
            </div>
          )}

          {/* ==================================================================
              PHÂN KHU 3: QUẢN LÝ DANH SÁCH SẢN PHẨM ('products')
              ================================================================== */}
          {activeTab === 'products' && (
            <div className="mp-tab-panel">
              <section className="mp-panel-card">
                <div className="mp-products-toolbar">
                  <div className="mp-status-tabs">
                    {[
                      { id: 'ALL', label: 'Tất cả', count: products.length },
                      { id: 'Approved', label: 'Đã duyệt', count: approvedCount },
                      { id: 'Pending', label: 'Chờ duyệt', count: pendingCount },
                      { id: 'Rejected', label: 'Bị từ chối', count: rejectedCount },
                      { id: 'Inactive', label: 'Đã ẩn', count: inactiveCount },
                    ].map((tab) => (
                      <button
                        key={tab.id}
                        type="button"
                        className={`mp-status-tab ${statusFilter === tab.id ? 'active' : ''}`}
                        onClick={() => setStatusFilter(tab.id)}
                      >
                        {tab.label} <span>{tab.count}</span>
                      </button>
                    ))}
                  </div>

                  <div className="mp-search-sort-group">
                    <input
                      type="search"
                      className="mp-search-input"
                      placeholder="Tìm theo tên quà hoặc thẻ tag..."
                      value={searchKeyword}
                      onChange={(e) => setSearchKeyword(e.target.value)}
                    />
                    <select
                      className="mp-sort-select"
                      value={productSort}
                      onChange={(e) => setProductSort(e.target.value as any)}
                    >
                      <option value="newest">Mới đăng gần nhất</option>
                      <option value="clicks-desc">Nhiều lượt click nhất</option>
                      <option value="price-desc">Giá từ cao đến thấp</option>
                    </select>
                  </div>
                </div>

                {filteredProducts.length > 0 ? (
                  <div className="mp-product-table">
                    {filteredProducts.map((item) => {
                      const statusInfo =
                        item.status === 'Approved'
                          ? { label: 'Đang bán trên GiftFinder', cls: 'approved' }
                          : item.status === 'Rejected'
                            ? { label: 'Bị từ chối', cls: 'rejected' }
                            : item.status === 'Inactive'
                              ? { label: 'Đã ẩn', cls: 'inactive' }
                              : { label: 'Đang chờ duyệt', cls: 'pending' };

                      return (
                        <article key={item.id} className="mp-product-item-card">
                          <div className="mp-prod-media">
                            <img
                              src={getHighResImageUrl(item.imageUrl)}
                              alt={item.name}
                              onError={(e) => {
                                (e.currentTarget as HTMLImageElement).src =
                                  SAMPLE_GIFT_IMAGES[0].url;
                              }}
                            />
                            {item.isFeatured && (
                              <span className="mp-prod-featured-ribbon">🔥 FEATURED</span>
                            )}
                          </div>

                          <div className="mp-prod-body">
                            <div className="mp-prod-title-row">
                              <h3>{item.name}</h3>
                              <span className={`mp-status-pill ${statusInfo.cls}`}>
                                ● {statusInfo.label}
                              </span>
                            </div>

                            <div className="mp-prod-price-row">
                              <strong className="mp-price-current">
                                {item.price.toLocaleString('vi-VN')}đ
                              </strong>
                              {item.originalPrice && item.originalPrice > item.price && (
                                <span className="mp-price-old">
                                  {item.originalPrice.toLocaleString('vi-VN')}đ
                                </span>
                              )}
                              <span className="mp-prod-divider">•</span>
                              <span className="mp-prod-clicks">
                                📦 Tồn kho: <b>{item.stockQuantity ?? 50}</b>
                              </span>
                              <span className="mp-prod-divider">•</span>
                              <span className="mp-prod-clicks">
                                🛒 Đã bán: <b>{item.soldCount ?? 0}</b>
                              </span>
                              <span className="mp-prod-divider">•</span>
                              <span className="mp-prod-clicks">
                                📈 <b>{item.clickCount || 0}</b> lượt xem
                              </span>
                            </div>

                            <div className="mp-prod-tags-row">
                              {item.tagNames && item.tagNames.length > 0 ? (
                                item.tagNames.map((t) => (
                                  <span key={t} className="mp-tag-chip">
                                    #{t}
                                  </span>
                                ))
                              ) : (
                                <span className="mp-no-tag-warn">
                                  ⚠️ Chưa có thẻ Tag (Hãy bấm "Sửa" và dùng AI gắn Tag để hiển thị trên Catalog)
                                </span>
                              )}

                              {item.suitableZodiacs &&
                                item.suitableZodiacs.map((z) => {
                                  const zObj = ZODIAC_OPTIONS.find((opt) => opt.id === z);
                                  return (
                                    <span key={z} className="mp-zodiac-chip">
                                      {zObj ? `${zObj.symbol} ${zObj.label}` : z}
                                    </span>
                                  );
                                })}
                            </div>

                            {item.status === 'Rejected' && item.rejectReason && (
                              <div className="mp-reject-reason-box">
                                <strong>⚠️ Lý do Admin từ chối duyệt:</strong>{' '}
                                <span>{item.rejectReason}</span>
                                <button
                                  type="button"
                                  className="mp-fix-now-btn"
                                  onClick={() => handleStartEditProduct(item)}
                                >
                                  Sửa & Gửi duyệt lại ngay →
                                </button>
                              </div>
                            )}

                            {item.isFeatured && item.featuredUntil && (
                              <div className="mp-featured-until-note">
                                ✨ Gói Đẩy Top đang hiệu lực đến:{' '}
                                <b>
                                  {new Date(item.featuredUntil).toLocaleDateString('vi-VN')}
                                </b>
                              </div>
                            )}
                          </div>

                          <div className="mp-prod-actions">
                            <button
                              type="button"
                              className="mp-action-btn edit"
                              onClick={() => handleStartEditProduct(item)}
                            >
                              ✏️ Chỉnh sửa
                            </button>

                            {item.status === 'Approved' && !item.isFeatured && (
                              <button
                                type="button"
                                className="mp-action-btn boost"
                                onClick={() => handleInitiateBuyFeatured(item, 0)}
                                disabled={featuredLoadingId === item.id}
                              >
                                🚀 Đẩy Top (49k)
                              </button>
                            )}

                            {item.isFeatured && (
                              <button
                                type="button"
                                className="mp-action-btn cancel-boost"
                                onClick={() => handleCancelFeatured(item.id, item.name)}
                                disabled={featuredLoadingId === item.id}
                              >
                                ⏹️ Hủy Đẩy Top
                              </button>
                            )}

                            {item.status !== 'Inactive' && (
                              <>
                                {confirmDeleteId === item.id ? (
                                  <div className="mp-confirm-delete-inline">
                                    <button
                                      type="button"
                                      className="mp-confirm-yes"
                                      onClick={() => handleDeleteProduct(item.id)}
                                    >
                                      Xác nhận ẩn
                                    </button>
                                    <button
                                      type="button"
                                      className="mp-confirm-no"
                                      onClick={() => setConfirmDeleteId(null)}
                                    >
                                      Hủy
                                    </button>
                                  </div>
                                ) : (
                                  <button
                                    type="button"
                                    className="mp-action-btn delete"
                                    onClick={() => setConfirmDeleteId(item.id)}
                                  >
                                    🗑️ Ẩn quà
                                  </button>
                                )}
                              </>
                            )}
                          </div>
                        </article>
                      );
                    })}
                  </div>
                ) : (
                  <div className="mp-empty-box">
                    <div style={{ fontSize: 40, marginBottom: 10 }}>🎁</div>
                    <h3>Không tìm thấy món quà nào trong mục này</h3>
                    <button
                      type="button"
                      className="mp-btn-primary"
                      onClick={handleStartCreateNew}
                    >
                      + Đăng quà tặng mới
                    </button>
                  </div>
                )}
              </section>
            </div>
          )}

          {/* ==================================================================
              PHÂN KHU 4: ĐĂNG QUÀ MỚI / SỬA QUÀ + TRỢ LÝ AI ('editor')
              ================================================================== */}
          {activeTab === 'editor' && (
            <div className="mp-tab-panel">
              <div className="mp-editor-layout">
                <form onSubmit={handleSaveProduct} className="mp-panel-card mp-editor-form">
                  <div className="mp-panel-header">
                    <div>
                      <h2>
                        {editingProductId
                          ? '✏️ Cập Nhật Thông Tin Quà Tặng'
                          : '✨ Đăng Món Quà Bán Trực Tiếp Trên GiftFinder'}
                      </h2>
                      <p>
                        Khách hàng sẽ chọn số lượng, nhập địa chỉ nhận quà và đặt mua COD trực tiếp trên web mà không cần dẫn link ra ngoài.
                      </p>
                    </div>
                    {editingProductId && (
                      <button
                        type="button"
                        className="mp-btn-outline"
                        onClick={resetEditorForm}
                      >
                        ✕ Hủy sửa / Tạo mới
                      </button>
                    )}
                  </div>

                  <div className="mp-form-stack">
                    <label className="mp-field">
                      <span>Tên sản phẩm quà tặng *</span>
                      <input
                        value={prodName}
                        onChange={(e) => setProdName(e.target.value)}
                        placeholder="VD: Set Quà Nến Thơm Tinh Dầu & Hoa Khô Kỷ Niệm"
                        required
                      />
                    </label>

                    <div className="mp-field-row">
                      <label className="mp-field">
                        <span>Giá bán ưu đãi (VNĐ) *</span>
                        <input
                          value={prodPrice}
                          onChange={(e) => setProdPrice(e.target.value)}
                          placeholder="VD: 350000"
                          required
                        />
                      </label>
                      <label className="mp-field">
                        <span>Giá gốc trước giảm (VNĐ - Không bắt buộc)</span>
                        <input
                          value={prodOrigPrice}
                          onChange={(e) => setProdOrigPrice(e.target.value)}
                          placeholder="VD: 450000"
                        />
                      </label>
                    </div>

                    <div className="mp-field-row">
                      <label className="mp-field">
                        <span>Số lượng tồn kho sẵn sàng giao *</span>
                        <input
                          type="number"
                          min={0}
                          value={prodStockQty}
                          onChange={(e) => setProdStockQty(e.target.value)}
                          placeholder="VD: 50"
                          required
                        />
                      </label>
                      <label className="mp-field">
                        <span>Kho xuất hàng của Shop</span>
                        <input
                          readOnly
                          value={`📍 ${activePickupProvince} (${activeShopName})`}
                          style={{ background: '#f8fafc', color: '#475569' }}
                        />
                      </label>
                    </div>

                    <div className="mp-field">
                      <span>Đường dẫn hình ảnh sản phẩm (URL ảnh chất lượng cao)</span>
                      <input
                        value={prodImageUrl}
                        onChange={(e) => setProdImageUrl(e.target.value)}
                        placeholder="https://images.unsplash.com/..."
                      />
                      <div className="mp-sample-imgs-bar">
                        <small>Chọn nhanh ảnh mẫu HD:</small>
                        {SAMPLE_GIFT_IMAGES.map((sample) => (
                          <button
                            key={sample.label}
                            type="button"
                            className="mp-sample-img-chip"
                            onClick={() => setProdImageUrl(sample.url)}
                          >
                            🖼️ {sample.label}
                          </button>
                        ))}
                      </div>
                    </div>

                    <label className="mp-field">
                      <span>Mô tả chi tiết & câu chuyện món quà</span>
                      <textarea
                        rows={3}
                        value={prodDesc}
                        onChange={(e) => setProdDesc(e.target.value)}
                        placeholder="Thành phần hộp quà, ý nghĩa tặng quà, chính sách gói quà và viết thiệp tay..."
                      />
                    </label>

                    <div className="mp-ai-assistant-box">
                      <div className="mp-ai-assistant-head">
                        <div>
                          <span className="mp-ai-badge">🤖 GROQ AI 120B ASSISTANT</span>
                          <h3>Trợ Lý AI Tự Động Thẩm Định & Gợi Ý Phân Loại</h3>
                          <p>
                            AI sẽ đọc tên và mô tả món quà để tự động tích chọn các thẻ Dịp lễ, Sở thích và Cung hoàng đạo phù hợp nhất.
                          </p>
                        </div>
                        <button
                          type="button"
                          className="mp-ai-trigger-btn"
                          onClick={handleAiSuggestTags}
                          disabled={aiSuggesting}
                        >
                          {aiSuggesting
                            ? '⏳ AI đang phân tích...'
                            : '✨ Phân tích & Gợi ý bằng AI'}
                        </button>
                      </div>

                      {aiRationale && (
                        <div className="mp-ai-rationale-result">
                          <strong>💡 Nhận định từ AI Gift Concierge:</strong>
                          <p>{aiRationale}</p>
                        </div>
                      )}
                    </div>

                    <div className="mp-tag-selector-section">
                      <div className="mp-section-subtitle">
                        <span>🏷️ Chọn Thẻ Phân Loại Quà Tặng (Đã chọn: {selectedTagIds.length + customTags.length})</span>
                        <small>Bắt buộc chọn ít nhất 1 thẻ để sản phẩm xuất hiện trên Catalog & AI</small>
                      </div>

                      {groupedTags.recipients.length > 0 && (
                        <div className="mp-tag-group">
                          <label>👤 Người nhận quà:</label>
                          <div className="mp-chip-wrap">
                            {groupedTags.recipients.map((t) => (
                              <button
                                key={t.id}
                                type="button"
                                className={`mp-selectable-chip ${
                                  selectedTagIds.includes(t.id) ? 'selected' : ''
                                }`}
                                onClick={() => toggleTagId(t.id)}
                              >
                                {selectedTagIds.includes(t.id) ? '✓ ' : '+ '}
                                {t.name}
                              </button>
                            ))}
                          </div>
                        </div>
                      )}

                      {groupedTags.occasions.length > 0 && (
                        <div className="mp-tag-group">
                          <label>🎉 Dịp tặng quà:</label>
                          <div className="mp-chip-wrap">
                            {groupedTags.occasions.map((t) => (
                              <button
                                key={t.id}
                                type="button"
                                className={`mp-selectable-chip ${
                                  selectedTagIds.includes(t.id) ? 'selected' : ''
                                }`}
                                onClick={() => toggleTagId(t.id)}
                              >
                                {selectedTagIds.includes(t.id) ? '✓ ' : '+ '}
                                {t.name}
                              </button>
                            ))}
                          </div>
                        </div>
                      )}

                      {groupedTags.hobbies.length > 0 && (
                        <div className="mp-tag-group">
                          <label>💡 Sở thích & Dòng sản phẩm:</label>
                          <div className="mp-chip-wrap">
                            {groupedTags.hobbies.map((t) => (
                              <button
                                key={t.id}
                                type="button"
                                className={`mp-selectable-chip ${
                                  selectedTagIds.includes(t.id) ? 'selected' : ''
                                }`}
                                onClick={() => toggleTagId(t.id)}
                              >
                                {selectedTagIds.includes(t.id) ? '✓ ' : '+ '}
                                {t.name}
                              </button>
                            ))}
                          </div>
                        </div>
                      )}

                      <div className="mp-custom-tag-Adder">
                        <input
                          value={customTagInput}
                          onChange={(e) => setCustomTagInput(e.target.value)}
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') {
                              e.preventDefault();
                              handleAddCustomTag();
                            }
                          }}
                          placeholder="Tự gõ thêm thẻ Tag mới (VD: Quà handmade, Khắc tên...)"
                        />
                        <button type="button" onClick={handleAddCustomTag}>
                          + Thêm Tag
                        </button>
                      </div>

                      {customTags.length > 0 && (
                        <div className="mp-chip-wrap" style={{ marginTop: 8 }}>
                          {customTags.map((ct) => (
                            <span key={ct} className="mp-selectable-chip selected">
                              ✓ {ct}{' '}
                              <i
                                style={{ cursor: 'pointer', marginLeft: 4 }}
                                onClick={() =>
                                  setCustomTags((prev) => prev.filter((x) => x !== ct))
                                }
                              >
                                ✕
                              </i>
                            </span>
                          ))}
                        </div>
                      )}
                    </div>

                    <div className="mp-tag-selector-section">
                      <div className="mp-section-subtitle">
                        <span>🔮 Cung Hoàng Đạo Phù Hợp (Đã chọn: {selectedZodiacs.length}/12)</span>
                        <small>Giúp sản phẩm xuất hiện trong chuyên mục "Quà theo cung hoàng đạo"</small>
                      </div>
                      <div className="mp-zodiac-grid">
                        {ZODIAC_OPTIONS.map((z) => {
                          const isSel = selectedZodiacs.includes(z.id);
                          return (
                            <button
                              key={z.id}
                              type="button"
                              className={`mp-zodiac-btn ${isSel ? 'selected' : ''}`}
                              onClick={() => toggleZodiac(z.id)}
                            >
                              <span className="mp-z-symbol">{z.symbol}</span>
                              <div className="mp-z-copy">
                                <strong>{z.label}</strong>
                                <small>{z.dates}</small>
                              </div>
                            </button>
                          );
                        })}
                      </div>
                    </div>

                    <div className="mp-form-actions-bar">
                      <button
                        type="button"
                        className="mp-secondary-btn"
                        onClick={() => setActiveTab('products')}
                      >
                        Hủy bỏ
                      </button>
                      <button
                        type="submit"
                        className="mp-primary-submit-btn"
                        disabled={savingProduct}
                      >
                        {savingProduct
                          ? 'Đang lưu sản phẩm...'
                          : editingProductId
                            ? '💾 Lưu Cập Nhật & Gửi Duyệt Lại'
                            : '🚀 Đăng Sản Phẩm Quà Tặng'}
                      </button>
                    </div>
                  </div>
                </form>

                <aside className="mp-editor-preview">
                  <div className="mp-preview-sticky">
                    <span className="mp-preview-eyebrow">👁️ XEM TRƯỚC HIỂN THỊ TRÊN WEB</span>
                    <article className="product-card" style={{ pointerEvents: 'none' }}>
                      <div className="product-photo">
                        <span className="platform-pill direct">GiftFinder Direct</span>
                        <span className="discount-pill">-{previewDisc}%</span>
                        <img
                          src={getHighResImageUrl(prodImageUrl || SAMPLE_GIFT_IMAGES[0].url)}
                          alt="Preview"
                          onError={(e) => {
                            (e.currentTarget as HTMLImageElement).src =
                              SAMPLE_GIFT_IMAGES[0].url;
                          }}
                        />
                      </div>
                      <div className="product-body">
                        <h3 className="product-title">
                          {prodName.trim() || 'Tên món quà tặng của bạn sẽ hiển thị tại đây'}
                        </h3>
                        <div className="product-rating">
                          <span className="stars">★★★★★</span>
                          <span>5.0 • {activeShopName} ({activePickupProvince})</span>
                        </div>
                        <div className="price-row">
                          <div>
                            <strong className="price-now">
                              {previewPriceNum.toLocaleString('vi-VN')}đ
                            </strong>
                            <span className="price-old">
                              {previewOrigNum.toLocaleString('vi-VN')}đ
                            </span>
                          </div>
                        </div>
                        <div className="card-actions">
                          <button type="button" className="buy-btn">
                            Đặt mua ngay (COD) →
                          </button>
                        </div>
                      </div>
                    </article>

                    <div className="mp-preview-meta-card">
                      <h4>Tóm tắt phân loại AI đã gắn:</h4>
                      <p>
                        <strong>Tồn kho sẵn có:</strong> {prodStockQty || 50} món (Kho:{' '}
                        {activePickupProvince})
                      </p>
                      <p>
                        <strong>Thẻ Tag:</strong>{' '}
                        {selectedTagIds.length + customTags.length > 0
                          ? [
                              ...allTags
                                .filter((t) => selectedTagIds.includes(t.id))
                                .map((t) => t.name),
                              ...customTags,
                            ].join(', ')
                          : 'Chưa chọn thẻ nào'}
                      </p>
                    </div>
                  </div>
                </aside>
              </div>
            </div>
          )}

          {/* ==================================================================
              PHÂN KHU 5: QUẢNG CÁO ĐẨY TOP FEATURED & VNPAY ('featured')
              ================================================================== */}
          {activeTab === 'featured' && (
            <div className="mp-tab-panel">
              <section className="mp-featured-plans-grid">
                <div
                  className={`mp-plan-card ${selectedPlanType === 0 ? 'selected' : ''}`}
                  onClick={() => setSelectedPlanType(0)}
                >
                  <div className="mp-plan-head">
                    <span className="mp-plan-tag">GÓI LINH HOẠT</span>
                    <h3>Gói Đẩy Top Theo Tuần (7 Ngày)</h3>
                    <div className="mp-plan-price">
                      <strong>49.000đ</strong> <span>/ 7 ngày</span>
                    </div>
                  </div>
                  <ul className="mp-plan-features">
                    <li>✓ Ưu tiên vị trí đầu trong kết quả gợi ý của AI Chatbot</li>
                    <li>✓ Gắn huy hiệu <b>"Được tài trợ"</b> nổi bật trên thẻ sản phẩm</li>
                    <li>✓ Tăng trực tiếp số đơn đặt hàng COD cho gian hàng</li>
                  </ul>
                </div>

                <div
                  className={`mp-plan-card popular ${selectedPlanType === 1 ? 'selected' : ''}`}
                  onClick={() => setSelectedPlanType(1)}
                >
                  <div className="mp-plan-ribbon">TIẾT KIỆM 20%</div>
                  <div className="mp-plan-head">
                    <span className="mp-plan-tag">GÓI CHUYÊN NGHIỆP</span>
                    <h3>Gói Đẩy Top Theo Tháng (30 Ngày)</h3>
                    <div className="mp-plan-price">
                      <strong>199.000đ</strong> <span>/ 30 ngày</span>
                    </div>
                  </div>
                  <ul className="mp-plan-features">
                    <li>✓ Toàn bộ đặc quyền hiển thị ưu tiên suốt 30 ngày liên tục</li>
                    <li>✓ Xuất hiện xuyên suốt các dịp lễ lớn & sinh nhật trong tháng</li>
                    <li>✓ Hỗ trợ cộng dồn thời gian nếu gia hạn nối tiếp</li>
                  </ul>
                </div>
              </section>

              <section className="mp-panel-card" style={{ marginTop: 24 }}>
                <div className="mp-panel-header">
                  <div>
                    <h2>
                      🚀 Chọn Sản Phẩm Để Kích Hoạt{' '}
                      {selectedPlanType === 0
                        ? 'Gói Tuần (49.000đ)'
                        : 'Gói Tháng (199.000đ)'}
                    </h2>
                    <p>
                      Theo quy định hệ thống (BR-24), chỉ những sản phẩm ở trạng thái <b>Đã duyệt</b> mới được đăng ký gói hiển thị nổi bật.
                    </p>
                  </div>
                </div>

                {products.filter((p) => p.status === 'Approved').length > 0 ? (
                  <div className="mp-featured-prod-list">
                    {products
                      .filter((p) => p.status === 'Approved')
                      .map((p) => (
                        <div key={p.id} className="mp-featured-prod-row">
                          <img
                            src={getHighResImageUrl(p.imageUrl)}
                            alt={p.name}
                            onError={(e) => {
                              (e.currentTarget as HTMLImageElement).src =
                                SAMPLE_GIFT_IMAGES[0].url;
                            }}
                          />
                          <div className="mp-fp-info">
                            <h4>
                              {p.name}
                              {p.isFeatured && (
                                <span className="mp-featured-mini-tag">🔥 Đang Featured</span>
                              )}
                            </h4>
                            <p>
                              Giá bán: <b>{p.price.toLocaleString('vi-VN')}đ</b> • Đã bán:{' '}
                              <b>{p.soldCount || 0}</b> • Lượt xem: <b>{p.clickCount || 0}</b>
                              {p.isFeatured && p.featuredUntil && (
                                <>
                                  {' '}
                                  • Hết hạn:{' '}
                                  <b>
                                    {new Date(p.featuredUntil).toLocaleDateString('vi-VN')}
                                  </b>
                                </>
                              )}
                            </p>
                          </div>
                          <div className="mp-fp-actions">
                            <button
                              type="button"
                              className="mp-btn-primary"
                              disabled={featuredLoadingId === p.id}
                              onClick={() => handleInitiateBuyFeatured(p, selectedPlanType)}
                            >
                              {featuredLoadingId === p.id
                                ? 'Đang tạo đơn...'
                                : p.isFeatured
                                  ? `➕ Gia hạn thêm (${selectedPlanType === 0 ? '49k' : '199k'})`
                                  : `🚀 Mua gói Đẩy Top (${selectedPlanType === 0 ? '49k' : '199k'})`}
                            </button>
                            {p.isFeatured && (
                              <button
                                type="button"
                                className="mp-btn-outline danger"
                                disabled={featuredLoadingId === p.id}
                                onClick={() => handleCancelFeatured(p.id, p.name)}
                              >
                                Hủy gói
                              </button>
                            )}
                          </div>
                        </div>
                      ))}
                  </div>
                ) : (
                  <div className="mp-empty-box">
                    <p>
                      Gian hàng hiện chưa có sản phẩm nào ở trạng thái <b>Đã duyệt</b>. Vui lòng chờ Admin duyệt sản phẩm trước khi bật quảng cáo!
                    </p>
                  </div>
                )}
              </section>
            </div>
          )}

          {/* ==================================================================
              PHÂN KHU 6: HỒ SƠ & ĐỊA CHỈ KHO HÀNG ('profile')
              ================================================================== */}
          {activeTab === 'profile' && (
            <div className="mp-tab-panel">
              <div className="mp-overview-columns">
                <form onSubmit={handleUpdateProfile} className="mp-panel-card">
                  <div className="mp-panel-header">
                    <div>
                      <h2>⚙️ Cập Nhật Thông Tin Gian Hàng & Kho Lấy Hàng</h2>
                      <p>Tỉnh/Thành phố kho lấy hàng được dùng để tính phí ship Nội tỉnh & Hỏa tốc 2H</p>
                    </div>
                  </div>

                  <div className="mp-form-stack">
                    <label className="mp-field">
                      <span>Tên gian hàng / Thương hiệu quà tặng *</span>
                      <input
                        value={profShopName}
                        onChange={(e) => setProfShopName(e.target.value)}
                        placeholder="VD: Tiệm Gốm Mộc"
                        required
                      />
                    </label>

                    <div className="mp-field-row">
                      <label className="mp-field">
                        <span>Tỉnh / Thành phố Kho lấy hàng *</span>
                        <select
                          value={profProvince}
                          onChange={(e) => setProfProvince(e.target.value)}
                          style={{
                            padding: '11px 14px',
                            borderRadius: 10,
                            border: '1px solid #cbd5e1',
                            fontSize: 14,
                            fontWeight: 600,
                          }}
                        >
                          {PROVINCE_OPTIONS.map((prov) => (
                            <option key={prov} value={prov}>
                              {prov}
                            </option>
                          ))}
                        </select>
                      </label>
                      <label className="mp-field">
                        <span>Số điện thoại Hotline / Shipper liên hệ *</span>
                        <input
                          value={profPhone}
                          onChange={(e) => setProfPhone(e.target.value)}
                          placeholder="0909123456"
                          required
                        />
                      </label>
                    </div>

                    <label className="mp-field">
                      <span>Địa chỉ kho lấy hàng chi tiết *</span>
                      <input
                        value={profAddress}
                        onChange={(e) => setProfAddress(e.target.value)}
                        placeholder="Số nhà, đường, phường/xã, quận/huyện..."
                      />
                    </label>

                    <label className="mp-field">
                      <span>Email liên hệ & Nhận thông báo đơn hàng mới *</span>
                      <input
                        type="email"
                        value={profEmail}
                        onChange={(e) => setProfEmail(e.target.value)}
                        placeholder="shop@giftfinder.vn"
                        required
                      />
                    </label>

                    <label className="mp-field">
                      <span>Mã số thuế / Giấy phép kinh doanh</span>
                      <input
                        value={profLicense}
                        onChange={(e) => setProfLicense(e.target.value)}
                        placeholder="VD: GPKD-8888"
                      />
                    </label>

                    <div style={{ marginTop: 8 }}>
                      <button
                        type="submit"
                        className="mp-primary-submit-btn"
                        disabled={savingProfile}
                      >
                        {savingProfile ? 'Đang lưu hồ sơ...' : '💾 Lưu Thay Đổi Hồ Sơ & Kho Hàng'}
                      </button>
                    </div>
                  </div>
                </form>

                <section className="mp-panel-card">
                  <div className="mp-panel-header">
                    <div>
                      <h2>🛡️ Chứng Nhận Đối Tác & Chính Sách Vận Chuyển</h2>
                      <p>Thông tin định danh và cơ chế tính phí giao quà tự động</p>
                    </div>
                  </div>

                  <div className="mp-Quick-profile-list">
                    <div className="mp-qp-item">
                      <span>Mã định danh Shop (Merchant ID):</span>
                      <strong style={{ fontSize: 12, fontFamily: 'monospace' }}>
                        {profile?.id || 'Đang đồng bộ'}
                      </strong>
                    </div>
                    <div className="mp-qp-item">
                      <span>Tỉnh / Thành phố kho xuất hàng:</span>
                      <strong style={{ color: '#e11d48' }}>📍 {profProvince}</strong>
                    </div>
                    <div className="mp-qp-item">
                      <span>Khách đặt cùng tỉnh ({profProvince}):</span>
                      <strong style={{ color: '#059669' }}>
                        Mở Giao Tiêu Chuẩn (25k) & Hỏa Tốc 2H (50k)
                      </strong>
                    </div>
                    <div className="mp-qp-item">
                      <span>Khách đặt khác tỉnh (Liên tỉnh):</span>
                      <strong>Giao Tiêu Chuẩn Liên Tỉnh (35k) · Khóa Hỏa Tốc</strong>
                    </div>
                    <div className="mp-qp-item">
                      <span>Miễn phí vận chuyển (Freeship):</span>
                      <strong>Đơn quà tiêu chuẩn từ 500.000đ trở lên</strong>
                    </div>
                  </div>
                </section>
              </div>
            </div>
          )}
        </main>
      </div>

      {/* ======================================================================
          MODAL NHẬP MÃ VẬN ĐƠN KHI BÀN GIAO CHO SHIPPER
          ====================================================================== */}
      {shippingModalOrder && (
        <div className="modal-layer" onMouseDown={() => setShippingModalOrder(null)}>
          <div
            className="modal-card mp-vnpay-modal"
            onMouseDown={(e) => e.stopPropagation()}
          >
            <div className="modal-heading">
              <div className="modal-title-icon">🚚</div>
              <div>
                <h2>Bàn Giao Đơn Hàng Cho Shipper</h2>
                <p>Mã đơn hàng: #{shippingModalOrder.orderCode}</p>
              </div>
              <button
                type="button"
                className="close-button"
                onClick={() => setShippingModalOrder(null)}
              >
                ✕
              </button>
            </div>

            <div className="modal-body">
              <div className="mp-tx-summary-box" style={{ marginBottom: 16 }}>
                <div className="mp-tx-row">
                  <span>Người nhận quà:</span>
                  <strong>
                    {shippingModalOrder.receiverName} ({shippingModalOrder.receiverPhone})
                  </strong>
                </div>
                <div className="mp-tx-row">
                  <span>Địa chỉ giao:</span>
                  <strong>
                    {shippingModalOrder.receiverDistrict}, {shippingModalOrder.receiverProvince}
                  </strong>
                </div>
                <div className="mp-tx-row total">
                  <span>Tiền Shipper thu hộ (COD):</span>
                  <strong>{shippingModalOrder.totalAmount.toLocaleString('vi-VN')} VNĐ</strong>
                </div>
              </div>

              <div className="mp-form-stack">
                <label className="mp-field">
                  <span>Chọn đơn vị vận chuyển *</span>
                  <select
                    value={selectedCarrier}
                    onChange={(e) => setSelectedCarrier(e.target.value)}
                    style={{
                      padding: '11px 14px',
                      borderRadius: 10,
                      border: '1px solid #cbd5e1',
                      fontSize: 14,
                      fontWeight: 600,
                    }}
                  >
                    {CARRIER_OPTIONS.map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="mp-field">
                  <span>Mã vận đơn bưu cục (Tracking Code) *</span>
                  <input
                    value={trackingCodeInput}
                    onChange={(e) => setTrackingCodeInput(e.target.value)}
                    placeholder="VD: GHN-849201"
                  />
                </label>

                <button
                  type="button"
                  className="mp-primary-submit-btn"
                  disabled={updatingOrderId === shippingModalOrder.id}
                  onClick={() =>
                    handleQuickUpdateOrderStatus(
                      shippingModalOrder,
                      'Shipping',
                      selectedCarrier,
                      trackingCodeInput
                    )
                  }
                >
                  {updatingOrderId === shippingModalOrder.id
                    ? 'Đang cập nhật...'
                    : '🚀 Xác Nhận Đã Giao Cho Shipper'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ======================================================================
          MODAL MÃ QR CHIA SẺ GIAN HÀNG
          ====================================================================== */}
      {showQrModal && (
        <div className="modal-layer" onMouseDown={() => setShowQrModal(false)}>
          <div
            className="modal-card mp-vnpay-modal"
            style={{ maxWidth: 420, textAlign: 'center' }}
            onMouseDown={(e) => e.stopPropagation()}
          >
            <div className="modal-heading">
              <div className="modal-title-icon">📱</div>
              <div style={{ textAlign: 'left' }}>
                <h2>Mã QR Gian Hàng {activeShopName}</h2>
                <p>In lên thiệp cảm ơn hoặc gửi cho khách quét đặt quà</p>
              </div>
              <button
                type="button"
                className="close-button"
                onClick={() => setShowQrModal(false)}
              >
                ✕
              </button>
            </div>
            <div className="modal-body">
              <img
                src={`https://api.qrserver.com/v1/create-qr-code/?size=240x240&data=${encodeURIComponent(
                  shopShareUrl
                )}`}
                alt="QR Shop"
                style={{
                  width: 220,
                  height: 220,
                  margin: '10px auto 16px',
                  borderRadius: 16,
                  border: '1px solid #e2e8f0',
                  padding: 12,
                  background: '#fff',
                }}
              />
              <div style={{ fontSize: 13, fontWeight: 700, color: '#334155', marginBottom: 14 }}>
                {shopShareUrl}
              </div>
              <button
                type="button"
                className="mp-primary-submit-btn"
                style={{ width: '100%' }}
                onClick={() => {
                  navigator.clipboard?.writeText(shopShareUrl);
                  onNotify('📋 Đã sao chép link Gian hàng!');
                  setShowQrModal(false);
                }}
              >
                📋 Sao chép Link & Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ======================================================================
          MODAL THANH TOÁN GÓI FEATURED VNPAY & GIẢ LẬP KÍCH HOẠT TỨC THÌ
          ====================================================================== */}
      {featuredPendingTx && (
        <div className="modal-layer" onMouseDown={() => setFeaturedPendingTx(null)}>
          <div
            className="modal-card mp-vnpay-modal"
            onMouseDown={(e) => e.stopPropagation()}
          >
            <div className="modal-heading">
              <div className="modal-title-icon">🚀</div>
              <div>
                <h2>Thanh Toán Gói Quảng Cáo Đẩy Top</h2>
                <p>Mã giao dịch: #{featuredPendingTx.transactionId.slice(0, 8).toUpperCase()}</p>
              </div>
              <button
                type="button"
                className="close-button"
                onClick={() => setFeaturedPendingTx(null)}
              >
                ✕
              </button>
            </div>

            <div className="modal-body">
              <div className="mp-tx-summary-box">
                <div className="mp-tx-row">
                  <span>Sản phẩm quảng cáo:</span>
                  <strong>{featuredPendingTx.productName}</strong>
                </div>
                <div className="mp-tx-row">
                  <span>Gói dịch vụ:</span>
                  <strong>
                    {featuredPendingTx.planType === 'Monthly'
                      ? 'Gói Tháng (30 ngày)'
                      : 'Gói Tuần (7 ngày)'}
                  </strong>
                </div>
                <div className="mp-tx-row">
                  <span>Phương thức:</span>
                  <strong>Cổng thanh toán VNPAY (HMAC-SHA512)</strong>
                </div>
                <div className="mp-tx-row total">
                  <span>Tổng thanh toán:</span>
                  <strong>{featuredPendingTx.amount.toLocaleString('vi-VN')} VNĐ</strong>
                </div>
              </div>

              <div className="mp-vnpay-actions-stack">
                <button
                  type="button"
                  className="mp-vnpay-real-btn"
                  onClick={() => {
                    window.open(featuredPendingTx.paymentUrl, '_blank', 'noopener,noreferrer');
                    onNotify('Đã mở cổng thanh toán VNPAY Sandbox trong tab mới!');
                  }}
                >
                  💳 Mở Cổng Thanh Toán VNPAY Sandbox ↗
                </button>

                <div className="mp-vnpay-divider">
                  <span>HOẶC DÀNH CHO MÔI TRƯỜNG DEMO / TEST</span>
                </div>

                <button
                  type="button"
                  className="mp-vnpay-sim-btn"
                  onClick={handleSimulateFeaturedPayment}
                  disabled={simulatingPay}
                >
                  {simulatingPay
                    ? '⏳ Đang kích hoạt gói Đẩy Top...'
                    : '⚡ Xác Nhận Thanh Toán Thành Công Ngay (1-Click Activate)'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {!sidebarOpen && (
        <button
          type="button"
          className="mp-floating-sidebar-toggle"
          onClick={toggleSidebar}
          title="Mở thanh quản lý bên trái (☰)"
          aria-label="Mở thanh quản lý bên trái"
        >
          <span className="mp-hamburger-icon-group">
            <span className="mp-hamburger-bar" />
            <span className="mp-hamburger-bar" />
            <span className="mp-hamburger-bar" />
          </span>
          <span className="mp-floating-toggle-label">Thanh quản lý</span>
        </button>
      )}
    </div>
  );
};
