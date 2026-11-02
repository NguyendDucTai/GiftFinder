import React, { useEffect, useState, useMemo } from 'react';
import { useAuth } from '../../auth';
import {
  merchantService,
  type MerchantDashboardStats,
  type MerchantProductItem,
  type MerchantProfile,
  type TagDtoItem,
  type BuyFeaturedResponse,
} from '../services/merchantService';
import { getHighResImageUrl } from '../../../App';

type PortalTab = 'overview' | 'products' | 'editor' | 'featured' | 'profile';

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
  const [obLicense, setObLicense] = useState('');
  const [obLoading, setObLoading] = useState(false);
  const [obError, setObError] = useState<string | null>(null);

  // State cho Kênh Quản Lý Gian Hàng (Merchant Seller Center)
  const [activeTab, setActiveTab] = useState<PortalTab>('overview');
  const [stats, setStats] = useState<MerchantDashboardStats | null>(null);
  const [profile, setProfile] = useState<MerchantProfile | null>(null);
  const [products, setProducts] = useState<MerchantProductItem[]>([]);
  const [allTags, setAllTags] = useState<TagDtoItem[]>([]);
  const [loadingPortal, setLoadingPortal] = useState(false);
  const [needsShopSetup, setNeedsShopSetup] = useState(false);

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
  const [prodOriginalUrl, setProdOriginalUrl] = useState('');
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
  const [selectedPlanType, setSelectedPlanType] = useState<0 | 1>(0); // 0 = Weekly (49k), 1 = Monthly (199k)
  const [featuredPendingTx, setFeaturedPendingTx] = useState<BuyFeaturedResponse | null>(null);
  const [featuredLoadingId, setFeaturedLoadingId] = useState<string | null>(null);
  const [simulatingPay, setSimulatingPay] = useState(false);

  // State cho Form Hồ sơ Gian hàng (Tab 'profile')
  const [profShopName, setProfShopName] = useState('');
  const [profEmail, setProfEmail] = useState('');
  const [profPhone, setProfPhone] = useState('');
  const [profLicense, setProfLicense] = useState('');
  const [savingProfile, setSavingProfile] = useState(false);

  // Đồng bộ email mặc định khi user đã đăng nhập nhưng chưa mở shop
  useEffect(() => {
    if (user?.email && !obEmail) {
      setObEmail(user.email);
    }
    if (user?.fullName && !obFullName) {
      setObFullName(user.fullName);
    }
  }, [user]);

  // Hàm tải toàn bộ dữ liệu gian hàng từ Backend
  const loadMerchantPortalData = async () => {
    if (!isAuthenticated) return;
    setLoadingPortal(true);
    setNeedsShopSetup(false);
    try {
      const [profileRes, statsRes, productsRes, tagsRes] = await Promise.all([
        merchantService.getProfile().catch((err) => {
          if (err?.response?.status === 404 || err?.response?.status === 403) {
            setNeedsShopSetup(true);
          }
          return null;
        }),
        merchantService.getDashboardStats().catch(() => null),
        merchantService.getMyProducts(1, 100).catch(() => ({ items: [], totalCount: 0, pageNumber: 1, pageSize: 100 })),
        merchantService.getTags().catch(() => []),
      ]);

      if (profileRes) {
        setProfile(profileRes);
        setProfShopName(profileRes.shopName || '');
        setProfEmail(profileRes.contactEmail || '');
        setProfPhone(profileRes.contactPhone || '');
        setProfLicense(profileRes.businessLicense || '');
      }
      if (statsRes) {
        setStats(statsRes);
      }
      if (productsRes?.items) {
        setProducts(productsRes.items);
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
      loadMerchantPortalData();
    }
  }, [isAuthenticated, isMerchant]);

  // ============================================================================
  // XỬ LÝ ĐĂNG KÝ / ĐĂNG NHẬP / NÂNG CẤP TÀI KHOẢN ĐỐI TÁC (ONBOARDING)
  // ============================================================================
  const handleQuickDemoLogin = async () => {
    setObError(null);
    setObLoading(true);
    try {
      await login({ email: 'shopdemo@giftfinder.vn', password: 'Password@123' }, true);
      onNotify('🎉 Đã đăng nhập vào Gian hàng mẫu: Tiệm Quà & Nến Thơm Mộc!');
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

    // Đăng ký mới tài khoản Merchant 1 bước (POST /api/Auth/register-merchant)
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
      });
      if (res?.token) {
        upgradeToMerchant(res.token, res.shopName || obShopName.trim());
      }
      setNeedsShopSetup(false);
      onNotify(`🎉 Đã nâng cấp tài khoản lên Đối tác và mở gian hàng "${res.shopName}" thành công!`);
      await loadMerchantPortalData();
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
    setProdOriginalUrl('');
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
    setProdOriginalUrl(item.originalUrl || '');
    setProdImageUrl(item.imageUrl || '');
    setProdDesc(item.description || '');
    setSelectedTagIds(item.tagIds || []);
    setCustomTags([]);
    setSelectedZodiacs(item.suitableZodiacs || []);
    setAiRationale(null);
    setActiveTab('editor');
    window.scrollTo({ top: 0, behavior: 'smooth' });

    // Gọi thêm API chi tiết sản phẩm để đảm bảo đầy đủ Description, TagIds và SuitableZodiacs
    try {
      const detail = await merchantService.getProductById(item.id);
      if (detail) {
        setProdName(detail.name || item.name);
        setProdPrice(String(detail.price || item.price));
        setProdOrigPrice(detail.originalPrice ? String(detail.originalPrice) : '');
        setProdOriginalUrl(detail.originalUrl || item.originalUrl || '');
        setProdImageUrl(detail.imageUrl || item.imageUrl || '');
        setProdDesc(detail.description || '');
        if (detail.tagIds) setSelectedTagIds(detail.tagIds);
        if (detail.suitableZodiacs) setSelectedZodiacs(detail.suitableZodiacs);
      }
    } catch {
      // Dùng dữ liệu sẵn có từ list nếu lỗi mạng nhẹ
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

      // Cập nhật danh sách Tags nếu AI vừa tạo thêm Tag mới trong DB
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
    if (!prodOriginalUrl.trim() || !/^https?:\/\//i.test(prodOriginalUrl.trim())) {
      onNotify('⚠️ Đường dẫn đặt mua của Shop phải bắt đầu bằng http:// hoặc https://');
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
        originalUrl: prodOriginalUrl.trim(),
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
        onNotify('🎉 Đăng món quà mới thành công! Sản phẩm đang chờ Admin kiểm duyệt.');
      }

      resetEditorForm();
      await loadMerchantPortalData();
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
      await loadMerchantPortalData();
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
      await loadMerchantPortalData();
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
      await loadMerchantPortalData();
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
      });
      setProfile(updated);
      await loadMerchantPortalData();
      onNotify('✓ Đã lưu thông tin Hồ sơ Gian hàng thành công!');
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

  // Phân nhóm Tags theo loại để hiển thị dễ chọn
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
          {/* CỘT TRÁI: GIỚI THIỆU QUYỀN LỢI HỢP TÁC BÁN HÀNG CÙNG GIFTFINDER */}
          <section className="mp-onboarding-pitch">
            <div className="mp-pitch-badge">✨ HỆ SINH THÁI ĐỐI TÁC QUÀ TẶNG AI</div>
            <h1 className="mp-pitch-title">
              Đưa món quà tâm huyết của bạn đến <em>đúng người</em> đang tìm kiếm
            </h1>
            <p className="mp-pitch-subtitle">
              Khác với sàn thương mại điện tử đại trà, <strong>GiftFinder</strong> sử dụng trí tuệ nhân tạo để kết nối trực tiếp sản phẩm của Shop với những khách hàng đang thực sự cần mua quà theo đúng dịp lễ, người nhận và ngân sách.
            </p>

            <div className="mp-benefit-grid">
              <div className="mp-benefit-card">
                <div className="mp-benefit-icon">🤖</div>
                <div>
                  <h3>AI Gift Concierge Đề Xuất Tự Động</h3>
                  <p>
                    Sản phẩm của bạn được AI tư vấn trực tiếp trong khung chat và kết quả tìm kiếm cá nhân hóa 24/7.
                  </p>
                </div>
              </div>

              <div className="mp-benefit-card">
                <div className="mp-benefit-icon">🏷️</div>
                <div>
                  <h3>Tự Động Gắn Tag & 12 Cung Hoàng Đạo</h3>
                  <p>
                    Trợ lý AI 120B tự động đọc tên & mô tả quà để gán thẻ Dịp tặng, Sở thích và Cung hoàng đạo chuẩn xác.
                  </p>
                </div>
              </div>

              <div className="mp-benefit-card">
                <div className="mp-benefit-icon">🚀</div>
                <div>
                  <h3>Đẩy Top Featured Linh Hoạt</h3>
                  <p>
                    Chiếm vị trí ưu tiên đầu trang Catalog và gợi ý AI chỉ từ 49.000đ/tuần qua cổng VNPAY chính thức.
                  </p>
                </div>
              </div>

              <div className="mp-benefit-card">
                <div className="mp-benefit-icon">📊</div>
                <div>
                  <h3>Đo Lường Click & Chuyển Đổi Thực</h3>
                  <p>
                    Dẫn khách mua trực tiếp về Shopee Mall, TikTok Shop, Zalo hoặc Website riêng của bạn với thống kê minh bạch.
                  </p>
                </div>
              </div>
            </div>

            <div className="mp-pitch-metrics">
              <div>
                <strong>50.000+</strong>
                <span>Lượt tìm quà / tháng</span>
              </div>
              <div className="mp-pitch-divider" />
              <div>
                <strong>0đ</strong>
                <span>Phí mở & duy trì Shop</span>
              </div>
              <div className="mp-pitch-divider" />
              <div>
                <strong>100%</strong>
                <span>Hỗ trợ phân loại bằng AI</span>
              </div>
            </div>
          </section>

          {/* CỘT PHẢI: FORM ĐĂNG KÝ MỞ SHOP / NÂNG CẤP ROLE MERCHANT / ĐĂNG NHẬP */}
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

                {/* Hộp trải nghiệm nhanh 1-Click cho tài khoản Shop Demo */}
                <div className="mp-demo-quick-box">
                  <div className="mp-demo-quick-info">
                    <span>⚡ <strong>Trải nghiệm ngay Kênh Người Bán?</strong></span>
                    <small>Dùng tài khoản Đối tác mẫu đã có sẵn sản phẩm & số liệu thực tế</small>
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
                          <span>Mã GPKD / Mã số thuế (Tùy chọn)</span>
                          <input
                            value={obLicense}
                            onChange={(e) => setObLicense(e.target.value)}
                            placeholder="VD: GPKD-8888"
                          />
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
              /* ĐÃ ĐĂNG NHẬP NHƯNG LÀ MEMBER HOẶC CHƯA CÓ HỒ SƠ SHOP -> NÂNG CẤP LÊN MERCHANT */
              <>
                <div className="mp-form-header">
                  <span className="mp-upgrade-pill">NÂNG CẤP TÀI KHOẢN ĐỐI TÁC</span>
                  <h2>Kích Hoạt Gian Hàng Cho Tài Khoản Của Bạn</h2>
                  <p>
                    Xin chào <strong>{user?.fullName}</strong> ({user?.email}). Hãy điền thông tin thương hiệu bên dưới để nâng cấp tài khoản lên vai trò <strong>Chủ Gian Hàng (Merchant)</strong> ngay lập tức!
                  </p>
                </div>

                <div className="mp-demo-quick-box" style={{ marginBottom: 18 }}>
                  <div className="mp-demo-quick-info">
                    <span>⚡ <strong>Muốn xem sẵn dữ liệu mẫu?</strong></span>
                    <small>Chuyển sang tài khoản Shop Mẫu (Tiệm Quà & Nến Thơm Mộc)</small>
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
                      <span>Email liên hệ gian hàng *</span>
                      <input
                        type="email"
                        value={obEmail}
                        onChange={(e) => setObEmail(e.target.value)}
                        placeholder={user?.email || 'lienhe@shop.vn'}
                        required
                      />
                    </label>
                  </div>

                  <label className="mp-field">
                    <span>Mã Giấy phép kinh doanh / Mã số thuế (Không bắt buộc)</span>
                    <input
                      value={obLicense}
                      onChange={(e) => setObLicense(e.target.value)}
                      placeholder="VD: GPKD-2026-VN"
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
  // MÀN HÌNH 2: TRUNG TÂM QUẢN LÝ KÊNH NGƯỜI BÁN (CÓ THANH QUẢN LÝ BÊN TRÁI)
  // ============================================================================
  const activeShopName =
    profile?.shopName || stats?.shopName || user?.shopName || 'Gian Hàng Đối Tác';
  const isVerifiedShop = profile?.isVerified ?? stats?.isVerified ?? true;

  const approvedCount =
    stats?.approvedProducts ?? products.filter((p) => p.status === 'Approved').length;
  const pendingCount =
    stats?.pendingProducts ?? products.filter((p) => p.status === 'Pending').length;
  const rejectedCount =
    stats?.rejectedProducts ?? products.filter((p) => p.status === 'Rejected').length;
  const inactiveCount = products.filter((p) => p.status === 'Inactive').length;
  const featuredCount =
    stats?.featuredProducts ?? products.filter((p) => p.isFeatured).length;

  // Giá xem trước cho Live Preview trong Editor
  const previewPriceNum = parseInt(prodPrice.replace(/\D/g, ''), 10) || 350000;
  const previewOrigNum =
    parseInt(prodOrigPrice.replace(/\D/g, ''), 10) || Math.round(previewPriceNum * 1.25);
  const previewDisc =
    previewOrigNum > previewPriceNum
      ? Math.max(5, Math.round((1 - previewPriceNum / previewOrigNum) * 100))
      : 15;

  return (
    <div className="mp-portal-wrapper">
      <div className={`mp-portal-shell ${sidebarOpen ? '' : 'sidebar-closed'}`}>
        {/* ====================================================================
            THANH QUẢN LÝ BÊN TRÁI (LEFT SIDEBAR NAVIGATION)
            ==================================================================== */}
        <aside className={`mp-sidebar ${sidebarOpen ? 'open' : 'collapsed'}`}>
          {/* Thẻ Định Danh Shop */}
          <div className="mp-shop-identity">
            <div className="mp-shop-avatar">
              {activeShopName.charAt(0).toUpperCase()}
            </div>
            <div className="mp-shop-meta">
              <span className="mp-shop-role-tag">KÊNH ĐỐI TÁC</span>
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

          {/* Danh sách Menu Điều Hướng */}
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
              <span className="mp-nav-text">Hồ sơ gian hàng</span>
            </button>
          </nav>

          {/* Hộp tóm tắt trạng thái duyệt nhanh trên Sidebar */}
          <div className="mp-sidebar-mini-stats">
            <div className="mp-mini-stat-row">
              <span>🟢 Đang hiển thị</span>
              <strong>{approvedCount} quà</strong>
            </div>
            <div className="mp-mini-stat-row">
              <span>🟡 Chờ Admin duyệt</span>
              <strong>{pendingCount} quà</strong>
            </div>
            {rejectedCount > 0 && (
              <div
                className="mp-mini-stat-row rejected"
                onClick={() => {
                  setStatusFilter('Rejected');
                  setActiveTab('products');
                }}
              >
                <span>🔴 Cần sửa lại</span>
                <strong>{rejectedCount} quà →</strong>
              </div>
            )}
          </div>

          {/* Chân Sidebar */}
          <div className="mp-sidebar-footer">
            <button
              type="button"
              className="mp-sidebar-action-btn"
              onClick={() => loadMerchantPortalData()}
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

        {/* Lớp phủ mờ (Backdrop) khi mở menu trên màn hình nhỏ */}
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
          {/* Thanh Top Header của Kênh Người Bán */}
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
                    {activeTab === 'products' && 'Quản lý sản phẩm'}
                    {activeTab === 'editor' &&
                      (editingProductId ? 'Chỉnh sửa sản phẩm' : 'Đăng quà tặng mới')}
                    {activeTab === 'featured' && 'Quảng cáo Đẩy Top (VNPAY)'}
                    {activeTab === 'profile' && 'Hồ sơ & Cài đặt Gian hàng'}
                  </strong>
                </div>
                <h1 className="mp-page-title">
                  {activeTab === 'overview' && `Tổng quan: ${activeShopName}`}
                  {activeTab === 'products' && 'Kho Quà Tặng Của Gian Hàng'}
                  {activeTab === 'editor' &&
                    (editingProductId
                      ? 'Chỉnh Sửa Thông Tin Món Quà'
                      : 'Đăng Quà Tặng Mới (Tích Hợp AI Phân Loại)')}
                  {activeTab === 'featured' && 'Dịch Vụ Đẩy Top & Hiển Thị Nổi Bật'}
                  {activeTab === 'profile' && 'Hồ Sơ Định Danh & Thông Tin Liên Hệ'}
                </h1>
              </div>
            </div>

            <div className="mp-topbar-actions">
              <button
                type="button"
                className="mp-btn-outline"
                onClick={() => go('catalog')}
              >
                🔍 Xem Catalog Quà
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
              PHÂN KHU 1: TỔNG QUAN HIỆU SUẤT GIAN HÀNG ('overview')
              ================================================================== */}
          {activeTab === 'overview' && (
            <div className="mp-tab-panel">
              {/* 6 Thẻ KPI Thực Tế */}
              <section className="mp-kpi-grid">
                <article
                  className="mp-kpi-card clickable"
                  onClick={() => {
                    setStatusFilter('ALL');
                    setActiveTab('products');
                  }}
                >
                  <div className="mp-kpi-top">
                    <span className="mp-kpi-icon rose">🎁</span>
                    <span className="mp-kpi-badge">Toàn bộ kho</span>
                  </div>
                  <div className="mp-kpi-value">{stats?.totalProducts ?? products.length}</div>
                  <div className="mp-kpi-label">Tổng sản phẩm quà tặng</div>
                </article>

                <article className="mp-kpi-card">
                  <div className="mp-kpi-top">
                    <span className="mp-kpi-icon green">📈</span>
                    <span className="mp-kpi-badge success">Real-time</span>
                  </div>
                  <div className="mp-kpi-value">{stats?.totalClicks ?? 0}</div>
                  <div className="mp-kpi-label">Lượt khách Click mua hàng</div>
                </article>

                <article
                  className="mp-kpi-card clickable"
                  onClick={() => {
                    setStatusFilter('Approved');
                    setActiveTab('products');
                  }}
                >
                  <div className="mp-kpi-top">
                    <span className="mp-kpi-icon emerald">✓</span>
                    <span className="mp-kpi-badge success">Đang bán</span>
                  </div>
                  <div className="mp-kpi-value">{approvedCount}</div>
                  <div className="mp-kpi-label">Đã duyệt hiển thị trên Web & AI</div>
                </article>

                <article
                  className="mp-kpi-card clickable"
                  onClick={() => {
                    setStatusFilter('Pending');
                    setActiveTab('products');
                  }}
                >
                  <div className="mp-kpi-top">
                    <span className="mp-kpi-icon amber">⏳</span>
                    <span className="mp-kpi-badge warning">Đang xét duyệt</span>
                  </div>
                  <div className="mp-kpi-value">{pendingCount}</div>
                  <div className="mp-kpi-label">Đang chờ Admin phê duyệt</div>
                </article>

                <article
                  className="mp-kpi-card clickable"
                  onClick={() => {
                    setStatusFilter('Rejected');
                    setActiveTab('products');
                  }}
                >
                  <div className="mp-kpi-top">
                    <span className="mp-kpi-icon danger">⚠️</span>
                    <span className="mp-kpi-badge danger">Cần chú ý</span>
                  </div>
                  <div className="mp-kpi-value">{rejectedCount}</div>
                  <div className="mp-kpi-label">Sản phẩm bị từ chối cần sửa</div>
                </article>

                <article
                  className="mp-kpi-card clickable"
                  onClick={() => setActiveTab('featured')}
                >
                  <div className="mp-kpi-top">
                    <span className="mp-kpi-icon indigo">🚀</span>
                    <span className="mp-kpi-badge indigo">VNPAY Ads</span>
                  </div>
                  <div className="mp-kpi-value">{featuredCount}</div>
                  <div className="mp-kpi-label">Quà đang chạy Đẩy Top Featured</div>
                </article>
              </section>

              {/* Khu vực 2 cột: Bảng Top 5 sản phẩm hút khách + Thông tin Hồ sơ nhanh */}
              <div className="mp-overview-columns">
                <section className="mp-panel-card">
                  <div className="mp-panel-header">
                    <div>
                      <h2>🏆 Top 5 Quà Tặng Hút Khách Nhất</h2>
                      <p>Xếp hạng theo số lượt khách hàng bấm chuyển hướng mua thực tế</p>
                    </div>
                    <button
                      type="button"
                      className="mp-link-btn"
                      onClick={() => setActiveTab('products')}
                    >
                      Xem tất cả sản phẩm →
                    </button>
                  </div>

                  {stats?.topProducts && stats.topProducts.length > 0 ? (
                    <div className="mp-top-list">
                      {stats.topProducts.map((tp, idx) => (
                        <div key={tp.productId} className="mp-top-row">
                          <div className={`mp-rank-badge rank-${idx + 1}`}>#{idx + 1}</div>
                          <div className="mp-top-info">
                            <h4>
                              {tp.productName}
                              {tp.isFeatured && (
                                <span className="mp-featured-mini-tag">🔥 Featured</span>
                              )}
                            </h4>
                            <span className="mp-top-price">
                              {tp.price.toLocaleString('vi-VN')}đ
                            </span>
                          </div>
                          <div className="mp-top-stats">
                            <strong>{tp.clickCount} lượt click</strong>
                          </div>
                          <div>
                            {!tp.isFeatured ? (
                              <button
                                type="button"
                                className="mp-mini-boost-btn"
                                onClick={() => {
                                  const found = products.find((p) => p.id === tp.productId);
                                  if (found) {
                                    handleInitiateBuyFeatured(found, 0);
                                  } else {
                                    setActiveTab('featured');
                                  }
                                }}
                              >
                                🚀 Đẩy Top
                              </button>
                            ) : (
                              <span className="mp-active-ads-pill">Đang ưu tiên</span>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="mp-empty-box">
                      <p>Chưa có dữ liệu xếp hạng. Hãy đăng sản phẩm đầu tiên cho gian hàng!</p>
                      <button
                        type="button"
                        className="mp-btn-primary"
                        onClick={handleStartCreateNew}
                      >
                        + Đăng quà mới ngay
                      </button>
                    </div>
                  )}
                </section>

                <section className="mp-panel-card">
                  <div className="mp-panel-header">
                    <div>
                      <h2>🏪 Hồ Sơ Đối Tác</h2>
                      <p>Thông tin định danh trên hệ thống</p>
                    </div>
                    <button
                      type="button"
                      className="mp-link-btn"
                      onClick={() => setActiveTab('profile')}
                    >
                      Chỉnh sửa ✏️
                    </button>
                  </div>

                  <div className="mp-Quick-profile-list">
                    <div className="mp-qp-item">
                      <span>Tên thương hiệu:</span>
                      <strong>{activeShopName}</strong>
                    </div>
                    <div className="mp-qp-item">
                      <span>Email nhận thông báo:</span>
                      <strong>{profile?.contactEmail || user?.email || 'Chưa cập nhật'}</strong>
                    </div>
                    <div className="mp-qp-item">
                      <span>Hotline / Zalo:</span>
                      <strong>{profile?.contactPhone || 'Chưa cập nhật'}</strong>
                    </div>
                    <div className="mp-qp-item">
                      <span>Giấy phép kinh doanh:</span>
                      <strong>{profile?.businessLicense || 'Chưa bổ sung'}</strong>
                    </div>
                  </div>

                  <div className="mp-ai-tip-banner">
                    <strong>💡 Mẹo tăng gấp 3 lần đơn hàng:</strong>
                    <p>
                      Khi đăng quà mới, hãy sử dụng tính năng <b>"AI Phân tích & Gợi ý Tag"</b> để sản phẩm được gắn đầy đủ thẻ Dịp lễ & Cung hoàng đạo. AI Chatbot sẽ ưu tiên giới thiệu các món quà có đủ thẻ phân loại!
                    </p>
                  </div>
                </section>
              </div>
            </div>
          )}

          {/* ==================================================================
              PHÂN KHU 2: QUẢN LÝ DANH SÁCH SẢN PHẨM ('products')
              ================================================================== */}
          {activeTab === 'products' && (
            <div className="mp-tab-panel">
              <section className="mp-panel-card">
                {/* Thanh công cụ lọc & tìm kiếm */}
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

                {/* Danh sách sản phẩm */}
                {filteredProducts.length > 0 ? (
                  <div className="mp-product-table">
                    {filteredProducts.map((item) => {
                      const statusInfo =
                        item.status === 'Approved'
                          ? { label: 'Đã duyệt hiển thị', cls: 'approved' }
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
                                📈 <b>{item.clickCount || 0}</b> lượt khách click
                              </span>
                              <span className="mp-prod-divider">•</span>
                              <a
                                href={item.originalUrl}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="mp-prod-ext-link"
                              >
                                🔗 Link gốc sản phẩm ↗
                              </a>
                            </div>

                            {/* Hiển thị Tags & Cung Hoàng Đạo */}
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

                            {/* Hộp hiển thị lý do từ chối của Admin (nếu Rejected) */}
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

                            {/* Thông tin hạn Featured (nếu đang bật) */}
                            {item.isFeatured && item.featuredUntil && (
                              <div className="mp-featured-until-note">
                                ✨ Gói Đẩy Top đang hiệu lực đến:{' '}
                                <b>
                                  {new Date(item.featuredUntil).toLocaleDateString('vi-VN')}
                                </b>
                              </div>
                            )}
                          </div>

                          {/* Cột Nút Hành Động */}
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
                    <p>
                      Bạn có thể đổi bộ lọc trạng thái hoặc đăng sản phẩm quà tặng mới lên hệ thống.
                    </p>
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
              PHÂN KHU 3: ĐĂNG QUÀ MỚI / SỬA QUÀ + TRỢ LÝ AI ('editor')
              ================================================================== */}
          {activeTab === 'editor' && (
            <div className="mp-tab-panel">
              <div className="mp-editor-layout">
                {/* CỘT TRÁI: FORM NHẬP LIỆU & TRỢ LÝ AI */}
                <form onSubmit={handleSaveProduct} className="mp-panel-card mp-editor-form">
                  <div className="mp-panel-header">
                    <div>
                      <h2>
                        {editingProductId
                          ? '✏️ Cập Nhật Thông Tin Quà Tặng'
                          : '✨ Đăng Món Quà Mới Lên GiftFinder'}
                      </h2>
                      <p>
                        {editingProductId
                          ? 'Lưu ý: Sau khi chỉnh sửa, sản phẩm sẽ được chuyển về trạng thái Chờ duyệt để Admin kiểm tra lại.'
                          : 'Điền thông tin sản phẩm và bấm nút AI Phân Tích để tự động gắn thẻ Dịp tặng & Cung hoàng đạo.'}
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

                    <label className="mp-field">
                      <span>Đường dẫn khách đặt mua (Link Shopee / TikTok Shop / Website) *</span>
                      <input
                        value={prodOriginalUrl}
                        onChange={(e) => setProdOriginalUrl(e.target.value)}
                        placeholder="https://shopee.vn/san-pham-cua-shop..."
                        required
                      />
                    </label>

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

                    {/* KHỐI TRỢ LÝ AI GỢI Ý TAG & CUNG HOÀNG ĐẠO */}
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

                    {/* BỘ CHỌN THẺ TAG CHUẨN TỪ DATABASE */}
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

                      {/* Nhập thêm Tag tùy chỉnh bằng tay */}
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

                    {/* BỘ CHỌN 12 CUNG HOÀNG ĐẠO PHÙ HỢP */}
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

                {/* CỘT PHẢI: XEM TRƯỚC THẺ SẢN PHẨM TRỰC TIẾP (LIVE PREVIEW) */}
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
                          <span>5.0 • Gian hàng {activeShopName}</span>
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
                            Mua ngay →
                          </button>
                        </div>
                      </div>
                    </article>

                    <div className="mp-preview-meta-card">
                      <h4>Tóm tắt phân loại AI đã gắn:</h4>
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
                      <p>
                        <strong>Cung Hoàng Đạo:</strong>{' '}
                        {selectedZodiacs.length > 0
                          ? selectedZodiacs
                              .map(
                                (id) => ZODIAC_OPTIONS.find((z) => z.id === id)?.label || id
                              )
                              .join(', ')
                          : 'Tất cả các cung'}
                      </p>
                    </div>
                  </div>
                </aside>
              </div>
            </div>
          )}

          {/* ==================================================================
              PHÂN KHU 4: QUẢNG CÁO ĐẨY TOP FEATURED & VNPAY ('featured')
              ================================================================== */}
          {activeTab === 'featured' && (
            <div className="mp-tab-panel">
              {/* Chọn Gói Quảng Cáo */}
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
                    <li>✓ Tăng trung bình 300% lượt khách bấm xem gian hàng</li>
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

              {/* Danh sách sản phẩm đủ điều kiện Đẩy Top */}
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
                              Giá bán: <b>{p.price.toLocaleString('vi-VN')}đ</b> • Lượt click:{' '}
                              <b>{p.clickCount || 0}</b>
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
              PHÂN KHU 5: HỒ SƠ & CÀI ĐẶT GIAN HÀNG ('profile')
              ================================================================== */}
          {activeTab === 'profile' && (
            <div className="mp-tab-panel">
              <div className="mp-overview-columns">
                <form onSubmit={handleUpdateProfile} className="mp-panel-card">
                  <div className="mp-panel-header">
                    <div>
                      <h2>⚙️ Cập Nhật Thông Tin Gian Hàng</h2>
                      <p>Thông tin này được hiển thị công khai trên các sản phẩm của Shop</p>
                    </div>
                  </div>

                  <div className="mp-form-stack">
                    <label className="mp-field">
                      <span>Tên gian hàng / Thương hiệu quà tặng *</span>
                      <input
                        value={profShopName}
                        onChange={(e) => setProfShopName(e.target.value)}
                        placeholder="VD: Tiệm Quà & Nến Thơm Mộc"
                        required
                      />
                    </label>

                    <label className="mp-field">
                      <span>Email liên hệ & Nhận thông báo từ hệ thống *</span>
                      <input
                        type="email"
                        value={profEmail}
                        onChange={(e) => setProfEmail(e.target.value)}
                        placeholder="shop@giftfinder.vn"
                        required
                      />
                    </label>

                    <label className="mp-field">
                      <span>Số điện thoại Hotline / Zalo CSKH *</span>
                      <input
                        value={profPhone}
                        onChange={(e) => setProfPhone(e.target.value)}
                        placeholder="0909123456"
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
                        {savingProfile ? 'Đang lưu hồ sơ...' : '💾 Lưu Thay Đổi Hồ Sơ'}
                      </button>
                    </div>
                  </div>
                </form>

                <section className="mp-panel-card">
                  <div className="mp-panel-header">
                    <div>
                      <h2>🛡️ Chứng Nhận Đối Tác</h2>
                      <p>Thông tin pháp lý & bảo mật tài khoản</p>
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
                      <span>Trạng thái xác minh:</span>
                      <strong style={{ color: isVerifiedShop ? '#059669' : '#d97706' }}>
                        {isVerifiedShop ? '✓ Đã xác minh chính thức' : '⏳ Đang chờ xác minh'}
                      </strong>
                    </div>
                    <div className="mp-qp-item">
                      <span>Ngày tham gia hệ thống:</span>
                      <strong>
                        {profile?.createdAt
                          ? new Date(profile.createdAt).toLocaleDateString('vi-VN')
                          : 'Hôm nay'}
                      </strong>
                    </div>
                    <div className="mp-qp-item">
                      <span>Vai trò tài khoản (Role):</span>
                      <strong>Merchant (Đối tác bán hàng)</strong>
                    </div>
                  </div>
                </section>
              </div>
            </div>
          )}
        </main>
      </div>

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
      {/* Nút 3 gạch mở lại thanh bên khi đang cuộn trang và thanh bên đóng */}
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

