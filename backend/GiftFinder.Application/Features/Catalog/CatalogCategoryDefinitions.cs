using System.Text.RegularExpressions;
using GiftFinder.Domain.Entities;

namespace GiftFinder.Application.Features.Catalog;

public record CatalogCategoryRule(
    string Slug,
    string Title,
    string Group,
    string[] TagNames,
    string[] IncludeKeywords,
    string[] ExcludeKeywords,
    string? Gender = null,
    bool OnlyDiscounted = false,
    bool RequireZodiac = false,
    bool MatchAll = false);

public static class CatalogCategoryDefinitions
{
    private static readonly string[] StrictlyFemaleWords =
    {
        "bạn gái", "cho nữ", "dành cho nữ", "bạn nữ", "phụ nữ", "cho nàng", "tặng mẹ", "quà tặng mẹ",
        "tặng bà", "cô giáo", "ngày của mẹ", "20/10", "8/3", "son môi", "son dưỡng", "son kem",
        "trang điểm", "makeup", "túi kẹp nách", "túi đeo vai nữ", "clutch", "váy", "đầm",
        "kẹp tóc", "cột tóc", "khăn lụa", "hoa sáp", "bó hoa", "dây chuyền nữ", "lắc tay nữ", "bông tai", "khuyên tai"
    };

    private static readonly string[] StrictlyMaleWords =
    {
        "bạn trai", "cho nam", "dành cho nam", "bạn nam", "nam giới", "đàn ông", "phái mạnh",
        "cho chàng", "tặng bố", "tặng ba", "ví nam", "ví da nam", "bóp nam", "thắt lưng nam",
        "dây nịt nam", "cà vạt", "dao cạo râu", "tông đơ", "sáp vuốt tóc", "đồng hồ nam"
    };

    public static readonly IReadOnlyList<CatalogCategoryRule> All = new List<CatalogCategoryRule>
    {
        // ==========================================
        // 0. TẤT CẢ QUÀ TẶNG
        // ==========================================
        new(
            Slug: "tat-ca",
            Title: "Tất cả quà tặng tuyển chọn",
            Group: "Danh mục quà tặng",
            TagNames: Array.Empty<string>(),
            IncludeKeywords: Array.Empty<string>(),
            ExcludeKeywords: Array.Empty<string>(),
            MatchAll: true),

        // ==========================================
        // 1. QUÀ THEO DỊP
        // ==========================================
        new(
            Slug: "qua-sinh-nhat",
            Title: "Quà Sinh Nhật",
            Group: "Quà Theo Dịp",
            TagNames: new[] { "Sinh nhật" },
            IncludeKeywords: new[] { "sinh nhật", "birthday" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-ky-niem",
            Title: "Quà Kỷ Niệm",
            Group: "Quà Theo Dịp",
            TagNames: new[] { "Kỷ niệm" },
            IncludeKeywords: new[] { "kỷ niệm", "kỉ niệm", "anniversary", "ngày yêu", "cặp đôi", "tình yêu" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-tot-nghiep",
            Title: "Quà Tốt Nghiệp",
            Group: "Quà Theo Dịp",
            TagNames: new[] { "Tốt nghiệp" },
            IncludeKeywords: new[] { "tốt nghiệp", "cử nhân", "ra trường", "hướng dương" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-20-10-8-3",
            Title: "Quà 20/10 & 8/3",
            Group: "Quà Theo Dịp",
            TagNames: new[] { "20/10", "8/3" },
            IncludeKeywords: new[] { "20/10", "8/3", "phụ nữ", "cho nữ", "nàng", "hoa sáp", "bó hoa", "son", "phái đẹp" },
            ExcludeKeywords: new[] { "ví nam", "thắt lưng nam", "dây nịt nam", "cạo râu", "tông đơ" }),

        new(
            Slug: "qua-tan-gia",
            Title: "Quà Tân Gia",
            Group: "Quà Theo Dịp",
            TagNames: new[] { "Tân gia" },
            IncludeKeywords: new[] { "tân gia", "nhà mới", "ấm trà", "ấm chén", "bàn trà", "decor", "đèn ngủ", "pha lê", "phong thủy" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-valentine",
            Title: "Quà Valentine",
            Group: "Quà Theo Dịp",
            TagNames: new[] { "Valentine" },
            IncludeKeywords: new[] { "valentine", "14/2", "lễ tình nhân", "socola", "chocolate" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-ngay-cua-me",
            Title: "Quà Ngày Của Mẹ",
            Group: "Quà Theo Dịp",
            TagNames: new[] { "20/10", "8/3", "Bố mẹ & Người lớn tuổi" },
            IncludeKeywords: new[] { "tặng mẹ", "quà cho mẹ", "mẹ yêu", "ngày của mẹ", "20/10", "8/3" },
            ExcludeKeywords: Array.Empty<string>(),
            Gender: "Female"),

        new(
            Slug: "qua-ngay-le",
            Title: "Quà Ngày Lễ",
            Group: "Quà Theo Dịp",
            TagNames: new[] { "20/10", "8/3", "Valentine", "Giáng sinh", "Tết" },
            IncludeKeywords: new[] { "ngày lễ", "dịp lễ", "20/10", "8/3", "14/2", "giáng sinh", "noel", "lễ tết" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-trung-thu",
            Title: "Quà Trung Thu",
            Group: "Quà Theo Dịp",
            TagNames: Array.Empty<string>(),
            IncludeKeywords: new[] { "trung thu", "bánh trung thu", "đèn lồng", "rằm tháng 8", "trăng rằm" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-giang-sinh",
            Title: "Quà Giáng Sinh",
            Group: "Quà Theo Dịp",
            TagNames: new[] { "Giáng sinh" },
            IncludeKeywords: new[] { "giáng sinh", "noel", "christmas", "xmas", "ông già noel", "cây thông", "tuần lộc" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-tet",
            Title: "Quà Tết",
            Group: "Quà Theo Dịp",
            TagNames: new[] { "Tết" },
            IncludeKeywords: new[] { "quà tết", "biếu tết", "dịp tết", "tết nguyên đán", "năm mới", "xuân", "an khang" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-20-11-thay-co",
            Title: "Quà 20/11 & Thầy Cô",
            Group: "Quà Theo Dịp",
            TagNames: Array.Empty<string>(),
            IncludeKeywords: new[] { "20/11", "thầy cô", "cô giáo", "thầy giáo", "nhà giáo", "tri ân thầy cô", "tặng cô", "tặng thầy" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-chia-tay-di-xa",
            Title: "Quà Chia Tay / Đi Xa",
            Group: "Quà Theo Dịp",
            TagNames: new[] { "Thể thao & Du lịch" },
            IncludeKeywords: new[] { "chia tay", "đi xa", "lưu niệm", "kỷ vật", "du lịch", "bình giữ nhiệt", "sổ tay", "album", "khung ảnh" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-tang-khong-can-dip",
            Title: "Quà Tặng Không Cần Dịp",
            Group: "Quà Theo Dịp",
            TagNames: Array.Empty<string>(),
            IncludeKeywords: Array.Empty<string>(),
            ExcludeKeywords: Array.Empty<string>(),
            MatchAll: true),

        // ==========================================
        // 2. QUÀ THEO NGƯỜI NHẬN
        // ==========================================
        new(
            Slug: "qua-cho-me",
            Title: "Quà Cho Mẹ",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Bố mẹ & Người lớn tuổi", "Sức khỏe & Dinh dưỡng" },
            IncludeKeywords: new[] { "tặng mẹ", "cho mẹ", "mẹ yêu", "quà mẹ", "yến sào", "yến chưng", "massage", "ngải cứu", "khăn choàng", "khăn lụa", "cỏ mềm", "ngừa lão hóa", "sâm", "tơ tằm", "ấm trà", "trà thảo mộc" },
            ExcludeKeywords: new[] { "bạn trai", "cho nam", "ví nam", "thắt lưng nam", "gaming", "game thủ", "tai mèo" },
            Gender: "Female"),

        new(
            Slug: "qua-cho-bo",
            Title: "Quà Cho Bố",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Bố mẹ & Người lớn tuổi", "Thưởng trà", "Sức khỏe & Dinh dưỡng" },
            IncludeKeywords: new[] { "tặng bố", "cho bố", "tặng ba", "cho ba", "đồng hồ nam", "ví nam", "ví da nam", "thắt lưng", "dây nịt", "ấm trà", "ấm chén", "bàn trà", "massage", "ngải cứu", "yến sào", "yến chưng", "gỗ sồi", "cạo râu", "bình giữ nhiệt" },
            ExcludeKeywords: new[] { "gấu bông", "thú bông", "loopy", "capybara", "tai mèo", "búp bê", "kẹp tóc", "hoa sáp", "bó hoa", "mặt nạ", "túi mù", "blindbox" },
            Gender: "Male"),

        new(
            Slug: "qua-cho-ong-ba",
            Title: "Quà Cho Ông Bà",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Bố mẹ & Người lớn tuổi", "Sức khỏe & Dinh dưỡng", "Thưởng trà" },
            IncludeKeywords: new[] { "ông bà", "tặng ông", "tặng bà", "người lớn tuổi", "cao tuổi", "yến sào", "yến chưng", "thảo dược", "massage", "mát xa", "ngải cứu", "hồng ngoại", "ấm trà", "ấm chén", "bàn trà", "gỗ sồi", "hạt dinh dưỡng", "trà thảo mộc" },
            ExcludeKeywords: new[] { "gaming", "game thủ", "gấu bông", "loopy", "capybara", "blindbox", "túi mù", "son", "trang điểm", "makeup" }),

        new(
            Slug: "qua-tan-gia-gia-dinh",
            Title: "Quà Tân Gia & Gia Đình",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Tân gia", "Gia dụng & Bàn trà" },
            IncludeKeywords: new[] { "tân gia", "nhà mới", "gia đình", "ấm trà", "ấm chén", "bàn trà", "decor", "trang trí nhà", "phòng khách", "phòng ngủ", "đèn ngủ", "nến thơm", "gỗ sồi" },
            ExcludeKeywords: new[] { "ví nam", "thắt lưng", "son môi", "trang điểm", "makeup", "dây chuyền", "lắc tay", "nhẫn", "bông tai" }),

        new(
            Slug: "qua-cho-vo-chong",
            Title: "Quà Cho Vợ / Chồng",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Kỷ niệm", "Valentine" },
            IncludeKeywords: new[] { "tặng vợ", "tặng chồng", "vợ chồng", "người yêu", "cặp đôi", "kỷ niệm", "nước hoa", "trang sức", "dây chuyền", "lắc tay", "đồng hồ", "ví da", "thắt lưng", "set quà", "nến thơm" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "qua-cho-nang",
            Title: "Quà Cho Nàng (Bạn Gái)",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Mỹ phẩm & Làm đẹp", "Valentine", "20/10", "8/3" },
            IncludeKeywords: new[] { "bạn gái", "cho nữ", "cho nàng", "người yêu", "dây chuyền", "lắc tay", "vòng tay", "bông tai", "khuyên tai", "nhẫn", "son", "mỹ phẩm", "trang điểm", "makeup", "skincare", "dưỡng da", "nước hoa nữ", "túi xách nữ", "hoa sáp", "gấu bông", "nến thơm" },
            ExcludeKeywords: Array.Empty<string>(),
            Gender: "Female"),

        new(
            Slug: "qua-cho-chang",
            Title: "Quà Cho Chàng (Bạn Trai)",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Công nghệ" },
            IncludeKeywords: new[] { "bạn trai", "cho nam", "cho chàng", "đồng hồ nam", "ví nam", "ví da nam", "thắt lưng", "dây nịt", "nước hoa nam", "sữa tắm nam", "fanmen", "gaming", "tai nghe", "chuột", "bàn phím", "loa bluetooth", "dao cạo" },
            ExcludeKeywords: Array.Empty<string>(),
            Gender: "Male"),

        new(
            Slug: "qua-cho-dong-nghiep",
            Title: "Quà Cho Đồng Nghiệp",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Dân văn phòng", "Đọc sách", "Nến thơm & Chill" },
            IncludeKeywords: new[] { "đồng nghiệp", "văn phòng", "công sở", "bàn làm việc", "bình giữ nhiệt", "ly giữ nhiệt", "cốc sứ", "ly sứ", "sổ tay", "bút ký", "nến thơm", "tinh dầu", "lót chuột", "chuột không dây", "gối tựa", "móc khóa" },
            ExcludeKeywords: new[] { "đồ lót", "nhẫn đôi" }),

        new(
            Slug: "qua-cho-sep",
            Title: "Quà Cho Sếp",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Thưởng trà", "Sức khỏe & Dinh dưỡng", "Gia dụng & Bàn trà" },
            IncludeKeywords: new[] { "tặng sếp", "cho sếp", "biếu sếp", "đối tác", "doanh nghiệp", "doanh nhân", "cao cấp", "sang trọng", "yến sào", "yến chưng", "gỗ sồi", "ấm trà", "ấm chén", "bàn trà", "bút ký", "đồng hồ", "set thắt lưng", "hạt dinh dưỡng" },
            ExcludeKeywords: new[] { "gấu bông", "thú bông", "loopy", "capybara", "túi mù", "blindbox", "tai mèo", "kẹp tóc", "móc khóa" }),

        new(
            Slug: "qua-tri-an-thay-co",
            Title: "Quà Tri Ân Thầy Cô",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Đọc sách" },
            IncludeKeywords: new[] { "thầy cô", "cô giáo", "thầy giáo", "20/11", "tri ân", "nhà giáo", "bút ký", "sổ tay", "bình giữ nhiệt", "khăn choàng", "ấm trà", "yến sào", "yến chưng", "thảo dược", "cỏ mềm", "nến thơm" },
            ExcludeKeywords: new[] { "gaming", "game thủ", "tai mèo", "túi mù", "blindbox" }),

        new(
            Slug: "qua-cho-ban-than",
            Title: "Quà Cho Bạn Thân",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Âm nhạc" },
            IncludeKeywords: new[] { "bạn thân", "bạn bè", "bestie", "gấu bông", "loopy", "capybara", "túi mù", "blindbox", "hộp nhạc", "mô hình lắp ráp", "đèn ngủ", "quả cầu pha lê", "nến thơm", "bình giữ nhiệt", "máy ảnh" },
            ExcludeKeywords: new[] { "biếu đối tác", "biếu sếp" }),

        // ==========================================
        // 3. DÒNG SẢN PHẨM
        // ==========================================
        new(
            Slug: "my-pham-cham-soc-da",
            Title: "Mỹ Phẩm & Chăm Sóc Da",
            Group: "Dòng Sản Phẩm",
            TagNames: new[] { "Mỹ phẩm & Làm đẹp" },
            IncludeKeywords: new[] { "mỹ phẩm", "chăm sóc da", "skincare", "dưỡng da", "dưỡng ẩm", "dưỡng trắng", "sữa rửa mặt", "máy rửa mặt", "serum", "kem dưỡng", "mặt nạ", "son", "trang điểm", "makeup", "nước hoa", "sữa tắm", "toner", "cỏ mềm" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "nen-thom-tinh-dau",
            Title: "Nến Thơm & Tinh Dầu",
            Group: "Dòng Sản Phẩm",
            TagNames: new[] { "Nến thơm & Chill" },
            IncludeKeywords: new[] { "nến thơm", "tinh dầu", "khuếch tán", "sáp thơm", "hương thơm", "thư giãn", "chill" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "dong-ho-phu-kien-nam",
            Title: "Đồng Hồ & Phụ Kiện Nam",
            Group: "Dòng Sản Phẩm",
            TagNames: Array.Empty<string>(),
            IncludeKeywords: new[] { "đồng hồ nam", "ví nam", "ví da nam", "bóp nam", "thắt lưng", "dây nịt", "cà vạt", "nước hoa nam", "phụ kiện nam" },
            ExcludeKeywords: Array.Empty<string>(),
            Gender: "Male"),

        new(
            Slug: "trang-suc-nu-cao-cap",
            Title: "Trang Sức Nữ Cao Cấp",
            Group: "Dòng Sản Phẩm",
            TagNames: Array.Empty<string>(),
            IncludeKeywords: new[] { "dây chuyền", "vòng cổ", "lắc tay", "vòng tay", "bông tai", "khuyên tai", "nhẫn", "trang sức", "bạc 925", "bạc s925", "bạc ta", "moissanite", "ngọc trai" },
            ExcludeKeywords: new[] { "cho nam", "ví nam", "thắt lưng", "đồng hồ nam" },
            Gender: "Female"),

        new(
            Slug: "tui-xach-vi-da",
            Title: "Túi Xách & Ví Da",
            Group: "Dòng Sản Phẩm",
            TagNames: Array.Empty<string>(),
            IncludeKeywords: new[] { "túi xách", "túi đeo", "túi kẹp nách", "clutch", "ví da", "ví nam", "ví nữ", "ví cầm tay", "bóp da", "balo" },
            ExcludeKeywords: new[] { "túi quà", "túi giấy", "túi bóng", "túi mù", "túi chườm", "tặng kèm túi", "kèm túi đựng", "hộp túi" }),

        new(
            Slug: "khan-choang-phu-kien",
            Title: "Khăn Choàng & Phụ Kiện",
            Group: "Dòng Sản Phẩm",
            TagNames: Array.Empty<string>(),
            IncludeKeywords: new[] { "khăn choàng", "khăn lụa", "khăn quàng", "khăn cashmere", "khăn len", "kẹp tóc", "phụ kiện tóc", "cài áo", "mũ len", "tất vớ" },
            ExcludeKeywords: new[] { "khăn mặt", "khăn tắm" }),

        new(
            Slug: "do-cong-nghe-gaming",
            Title: "Đồ Công Nghệ & Gaming",
            Group: "Dòng Sản Phẩm",
            TagNames: new[] { "Công nghệ" },
            IncludeKeywords: new[] { "công nghệ", "gaming", "game thủ", "tai nghe", "chuột", "bàn phím", "lót chuột", "mouse pad", "loa bluetooth", "bluetooth", "sạc dự phòng", "pin dự phòng", "máy ảnh", "instax", "smartwatch", "máy rửa mặt" },
            ExcludeKeywords: new[] { "chuột lang", "capybara" }),

        new(
            Slug: "set-qua-thu-cong-giftbox",
            Title: "Set Quà Thủ Công Giftbox",
            Group: "Dòng Sản Phẩm",
            TagNames: Array.Empty<string>(),
            IncludeKeywords: new[] { "set quà", "sét quà", "hộp quà", "giftbox", "gift box", "combo quà", "bộ quà tặng", "thủ công", "handmade", "diy" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "bo-am-tra-ly-giu-nhiet",
            Title: "Bộ Ấm Trà & Ly Giữ Nhiệt",
            Group: "Dòng Sản Phẩm",
            TagNames: new[] { "Thưởng trà", "Gia dụng & Bàn trà" },
            IncludeKeywords: new[] { "ấm trà", "ấm chén", "bàn trà", "pha trà", "thưởng trà", "bình giữ nhiệt", "ly giữ nhiệt", "cốc giữ nhiệt", "cốc sứ", "ly sứ", "cốc hâm nóng" },
            ExcludeKeywords: new[] { "màu trà sữa" }),

        new(
            Slug: "suc-khoe-thao-moc",
            Title: "Sức Khỏe & Thảo Mộc",
            Group: "Dòng Sản Phẩm",
            TagNames: new[] { "Sức khỏe & Dinh dưỡng" },
            IncludeKeywords: new[] { "sức khỏe", "yến sào", "yến chưng", "thảo dược", "thảo mộc", "dinh dưỡng", "hạt dinh dưỡng", "massage", "mát xa", "cổ vai gáy", "ngải cứu", "hồng ngoại", "túi chườm", "nhân sâm", "đông trùng" },
            ExcludeKeywords: Array.Empty<string>()),

        new(
            Slug: "do-decor-ban-lam-viec",
            Title: "Đồ Decor Bàn Làm Việc",
            Group: "Dòng Sản Phẩm",
            TagNames: new[] { "Decor", "Âm nhạc" },
            IncludeKeywords: new[] { "decor", "bàn làm việc", "trang trí", "đèn ngủ", "đèn 3d", "đèn mặt trăng", "quả cầu pha lê", "đồng hồ cát", "hộp nhạc", "mô hình lắp ráp", "tượng", "khung ảnh", "cây để bàn" },
            ExcludeKeywords: new[] { "son", "trang điểm", "makeup", "sữa rửa mặt", "dây chuyền", "lắc tay", "nhẫn", "bông tai", "ví nam", "thắt lưng" }),

        new(
            Slug: "sach-so-tay-qua-tang",
            Title: "Sách & Sổ Tay Quà Tặng",
            Group: "Dòng Sản Phẩm",
            TagNames: new[] { "Đọc sách" },
            IncludeKeywords: new[] { "sổ tay", "sổ da", "sổ ghi chép", "bút ký", "bút máy", "nhật ký", "planner", "bookmark" },
            ExcludeKeywords: new[] { "túi xách" }),

        // ==========================================
        // 4. DANH MỤC ĐẶC BIỆT TRÊN THANH ĐIỀU HƯỚNG & TRANG CHỦ
        // ==========================================
        new(
            Slug: "qua-doanh-nghiep",
            Title: "Quà Doanh Nghiệp & Đối Tác",
            Group: "Quà Doanh Nghiệp",
            TagNames: new[] { "Thưởng trà", "Gia dụng & Bàn trà", "Sức khỏe & Dinh dưỡng" },
            IncludeKeywords: new[] { "doanh nghiệp", "đối tác", "khách hàng", "quà biếu", "tặng sếp", "gỗ sồi", "yến sào", "yến chưng", "ấm trà", "ấm chén", "bút ký", "sổ da", "bình giữ nhiệt", "hạt dinh dưỡng" },
            ExcludeKeywords: new[] { "gấu bông", "thú bông", "loopy", "capybara", "tai mèo", "túi mù", "blindbox", "son", "kẹp tóc" }),

        new(
            Slug: "qua-cung-hoang-dao",
            Title: "Quà Theo Cung Hoàng Đạo",
            Group: "Cung Hoàng Đạo",
            TagNames: Array.Empty<string>(),
            IncludeKeywords: new[] { "cung hoàng đạo", "12 cung", "hoàng đạo", "zodiac", "chòm sao", "bạch dương", "kim ngưu", "song tử", "cự giải", "sư tử", "xử nữ", "thiên bình", "bọ cạp", "thiên yết", "nhân mã", "ma kết", "bảo bình", "song ngư", "mặt trăng", "pha lê", "ngân hà", "vũ trụ" },
            ExcludeKeywords: Array.Empty<string>(),
            RequireZodiac: true),

        new(
            Slug: "flash-sale",
            Title: "⚡ Flash Sale — Deal Quà Hot Hôm Nay",
            Group: "Flash Sale",
            TagNames: Array.Empty<string>(),
            IncludeKeywords: Array.Empty<string>(),
            ExcludeKeywords: Array.Empty<string>(),
            OnlyDiscounted: true),

        new(
            Slug: "qua-bo-me",
            Title: "Quà Tặng Bố Mẹ",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Bố mẹ & Người lớn tuổi", "Sức khỏe & Dinh dưỡng", "Thưởng trà", "Gia dụng & Bàn trà" },
            IncludeKeywords: new[] { "bố mẹ", "cha mẹ", "tặng mẹ", "tặng bố", "cho mẹ", "cho bố", "yến sào", "yến chưng", "massage", "ngải cứu", "ấm trà", "ấm chén", "gỗ sồi", "thảo dược", "khăn choàng" },
            ExcludeKeywords: new[] { "gaming", "game thủ", "tai mèo", "gấu bông", "loopy", "capybara", "túi mù", "blindbox" }),

        new(
            Slug: "sinh-nhat-ban-than",
            Title: "Quà Sinh Nhật Bạn Thân",
            Group: "Quà Theo Người Nhận",
            TagNames: new[] { "Âm nhạc" },
            IncludeKeywords: new[] { "bạn thân", "bạn bè", "sinh nhật bạn", "gấu bông", "loopy", "capybara", "túi mù", "blindbox", "hộp nhạc", "mô hình lắp ráp", "đèn ngủ", "quả cầu pha lê", "nến thơm", "máy ảnh" },
            ExcludeKeywords: new[] { "biếu đối tác", "biếu sếp" })
    };

    private static readonly Dictionary<string, CatalogCategoryRule> BySlug =
        All.ToDictionary(c => c.Slug, c => c, StringComparer.OrdinalIgnoreCase);

    public static CatalogCategoryRule? GetBySlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return GetBySlug("tat-ca");
        return BySlug.TryGetValue(slug.Trim(), out var rule) ? rule : null;
    }

    public static bool ContainsKeywordBoundary(string text, string keyword)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(keyword)) return false;
        var kw = keyword.Trim().ToLowerInvariant();
        var idx = text.IndexOf(kw, StringComparison.Ordinal);
        while (idx >= 0)
        {
            bool leftOk = idx == 0 || !char.IsLetterOrDigit(text[idx - 1]);
            int endIdx = idx + kw.Length;
            bool rightOk = endIdx >= text.Length || !char.IsLetterOrDigit(text[endIdx]);
            if (leftOk && rightOk) return true;
            idx = text.IndexOf(kw, idx + 1, StringComparison.Ordinal);
        }
        return false;
    }

    public static int EvaluateMatchScore(
        string productName,
        string? productDescription,
        IReadOnlyCollection<string> productTagNames,
        bool hasSuitableZodiacs,
        long price,
        long? originalPrice,
        CatalogCategoryRule rule)
    {
        // 1. QUY TẮC TIÊN QUYẾT: Sản phẩm chưa có bất kỳ Tag nào thì KHÔNG hiển thị
        if (productTagNames == null || productTagNames.Count == 0)
        {
            return -1;
        }

        var nameLower = (productName ?? "").ToLowerInvariant();
        var descLower = (productDescription ?? "").ToLowerInvariant();
        var combined = nameLower + " " + descLower;

        // 2. Kiểm tra từ khóa loại trừ của danh mục
        foreach (var ex in rule.ExcludeKeywords)
        {
            if (ContainsKeywordBoundary(combined, ex))
                return -1;
        }

        // 3. Kiểm tra giới tính nghiêm ngặt (không để đồ nữ lọt vào danh mục nam và ngược lại)
        if (rule.Gender == "Male")
        {
            bool isFemaleOnly = ContainsKeywordBoundary(nameLower, "nữ") && !ContainsKeywordBoundary(nameLower, "nam");
            if (isFemaleOnly) return -1;
            foreach (var fw in StrictlyFemaleWords)
            {
                if (ContainsKeywordBoundary(nameLower, fw))
                    return -1;
            }
        }
        else if (rule.Gender == "Female")
        {
            bool isMaleOnly = ContainsKeywordBoundary(nameLower, "nam") && !ContainsKeywordBoundary(nameLower, "nữ");
            if (isMaleOnly) return -1;
            foreach (var mw in StrictlyMaleWords)
            {
                if (ContainsKeywordBoundary(nameLower, mw))
                    return -1;
            }
        }

        // 4. Trường hợp MatchAll ("Tất cả" / "Quà tặng không cần dịp")
        if (rule.MatchAll)
        {
            return 100;
        }

        // 5. Trường hợp Flash Sale
        if (rule.OnlyDiscounted)
        {
            if (originalPrice.HasValue && originalPrice.Value > price && price > 0)
            {
                int discountPct = (int)Math.Round((1.0 - (double)price / originalPrice.Value) * 100);
                return 100 + Math.Clamp(discountPct, 1, 90);
            }
            return -1;
        }

        int score = 0;

        // 6. Trường hợp Cung Hoàng Đạo
        if (rule.RequireZodiac && hasSuitableZodiacs)
        {
            score += 90;
        }

        // 7. So khớp Tag
        if (rule.TagNames.Length > 0)
        {
            foreach (var tName in rule.TagNames)
            {
                if (productTagNames.Any(pt => string.Equals(pt, tName, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 70;
                }
            }
        }

        // 8. So khớp từ khóa trong Tên (+100) và Mô tả (+40)
        foreach (var kw in rule.IncludeKeywords)
        {
            if (ContainsKeywordBoundary(nameLower, kw))
            {
                score += 100;
            }
            else if (ContainsKeywordBoundary(descLower, kw))
            {
                score += 40;
            }
        }

        // 9. Thưởng thêm điểm nếu giới tính khớp trực tiếp từ khóa giới tính
        if (rule.Gender == "Male" && StrictlyMaleWords.Any(mw => ContainsKeywordBoundary(nameLower, mw)))
        {
            score += 50;
        }
        else if (rule.Gender == "Female" && StrictlyFemaleWords.Any(fw => ContainsKeywordBoundary(nameLower, fw)))
        {
            score += 50;
        }

        return score > 0 ? score : -1;
    }
}
