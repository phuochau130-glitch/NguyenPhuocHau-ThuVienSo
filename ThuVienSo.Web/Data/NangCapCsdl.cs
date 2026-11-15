using Microsoft.EntityFrameworkCore;

namespace ThuVienSo.Web.Data;

/// <summary>
/// Nâng cấp cấu trúc cơ sở dữ liệu theo cách an toàn (idempotent) khi ứng dụng khởi động.
/// Bổ sung các cột/bảng mới cho tính năng thống kê, tiền phạt tách lý do và nhật ký hoạt động.
/// </summary>
public static class NangCapCsdl
{
    private static readonly string[] CauLenh =
    {
        // 1. Lượt xem tài liệu (dùng cho tài liệu số và sách vật lý)
        @"IF COL_LENGTH('TaiLieu','soLuotXem') IS NULL
            ALTER TABLE TaiLieu ADD soLuotXem INT NOT NULL DEFAULT 0;",

        // 2. Tách tiền phạt phiếu mượn thành phạt trả trễ và phạt hư hỏng
        @"IF COL_LENGTH('PhieuMuon','tienPhatTraTre') IS NULL
            ALTER TABLE PhieuMuon ADD tienPhatTraTre DECIMAL(18,2) NOT NULL DEFAULT 0;",

        @"IF COL_LENGTH('PhieuMuon','tienPhatHuHong') IS NULL
            ALTER TABLE PhieuMuon ADD tienPhatHuHong DECIMAL(18,2) NOT NULL DEFAULT 0;",

        // Chuyển dữ liệu phạt cũ (gộp) sang phạt trả trễ nếu có
        @"IF COL_LENGTH('PhieuMuon','tienPhat') IS NOT NULL
            UPDATE PhieuMuon SET tienPhatTraTre = tienPhat
            WHERE tienPhatTraTre = 0 AND tienPhat > 0;",

        // 3. Tệp đọc thử (PDF/Word) cho sách vật lý
        @"IF COL_LENGTH('SachVatLi','duongDanTepXemTruoc') IS NULL
            ALTER TABLE SachVatLi ADD duongDanTepXemTruoc VARCHAR(500) NULL;",

        @"IF COL_LENGTH('SachVatLi','dinhDangTepXemTruoc') IS NULL
            ALTER TABLE SachVatLi ADD dinhDangTepXemTruoc VARCHAR(10) NULL;",

        // Mã vạch không còn bắt buộc trên biểu mẫu; chỉ giữ duy nhất nếu có nhập giá trị.
        @"IF OBJECT_ID('dbo.SachVatLi','U') IS NOT NULL
          BEGIN
            DECLARE @tenChiMucMaVach SYSNAME;
            WHILE 1 = 1
            BEGIN
                SELECT TOP (1) @tenChiMucMaVach = i.name
                FROM sys.indexes i
                WHERE i.object_id = OBJECT_ID('dbo.SachVatLi')
                    AND i.is_unique = 1
                    AND i.has_filter = 0
                    AND i.is_primary_key = 0
                    AND i.is_unique_constraint = 0
                    AND (SELECT COUNT(*) FROM sys.index_columns ic
                         WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                           AND ic.key_ordinal > 0) = 1
                    AND EXISTS (SELECT 1 FROM sys.index_columns ic
                        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                          AND ic.key_ordinal > 0 AND c.name = 'maVach');
                IF @tenChiMucMaVach IS NULL BREAK;
                EXEC(N'DROP INDEX ' + QUOTENAME(@tenChiMucMaVach) + N' ON dbo.SachVatLi;');
                SET @tenChiMucMaVach = NULL;
            END;

            IF COL_LENGTH('dbo.SachVatLi','maVach') IS NOT NULL
                AND EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.SachVatLi') AND name = 'maVach' AND is_nullable = 0)
                ALTER TABLE dbo.SachVatLi ALTER COLUMN maVach VARCHAR(50) NULL;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes i
                JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal = 1
                JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE i.object_id = OBJECT_ID('dbo.SachVatLi') AND i.name = 'IX_SachVatLi_maVach')
                CREATE UNIQUE INDEX IX_SachVatLi_maVach ON dbo.SachVatLi(maVach)
                    WHERE maVach IS NOT NULL;
          END",

        // 4. Ảnh bìa (trang bìa) cho mọi tài liệu (sách vật lý và tài liệu số)
        @"IF COL_LENGTH('TaiLieu','duongDanAnhBia') IS NULL
            ALTER TABLE TaiLieu ADD duongDanAnhBia VARCHAR(500) NULL;",

        // 5. Danh mục và tác giả dùng chung trong biểu mẫu quản lý tài liệu
        @"IF OBJECT_ID('DanhMucTaiLieu','U') IS NULL
          BEGIN
            CREATE TABLE DanhMucTaiLieu
            (
                maDanhMuc INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                tenDanhMuc NVARCHAR(100) NOT NULL
            );
            CREATE UNIQUE INDEX UX_DanhMucTaiLieu_Ten ON DanhMucTaiLieu(tenDanhMuc);
          END",

        @"IF OBJECT_ID('TacGiaTaiLieu','U') IS NULL
          BEGIN
            CREATE TABLE TacGiaTaiLieu
            (
                maTacGia INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                tenTacGia NVARCHAR(200) NOT NULL
            );
            CREATE UNIQUE INDEX UX_TacGiaTaiLieu_Ten ON TacGiaTaiLieu(tenTacGia);
          END",

        @"INSERT INTO DanhMucTaiLieu(tenDanhMuc)
          SELECT mau.tenDanhMuc
          FROM (VALUES
            (N'Công nghệ thông tin'),
            (N'Khoa học máy tính'),
            (N'Kinh tế'),
            (N'Văn học'),
            (N'Khoa học xã hội'),
            (N'Giáo trình'),
            (N'Nghiên cứu khoa học')
          ) AS mau(tenDanhMuc)
          WHERE NOT EXISTS
            (SELECT 1 FROM DanhMucTaiLieu dm WHERE dm.tenDanhMuc = mau.tenDanhMuc);",

        @"INSERT INTO DanhMucTaiLieu(tenDanhMuc)
          SELECT DISTINCT LTRIM(RTRIM(t.danhMuc))
          FROM TaiLieu t
          WHERE t.danhMuc IS NOT NULL AND LTRIM(RTRIM(t.danhMuc)) <> ''
            AND NOT EXISTS
              (SELECT 1 FROM DanhMucTaiLieu dm WHERE dm.tenDanhMuc = LTRIM(RTRIM(t.danhMuc)));",

        @"INSERT INTO TacGiaTaiLieu(tenTacGia)
          SELECT DISTINCT LTRIM(RTRIM(t.tacGia))
          FROM TaiLieu t
          WHERE t.tacGia IS NOT NULL AND LTRIM(RTRIM(t.tacGia)) <> ''
            AND NOT EXISTS
              (SELECT 1 FROM TacGiaTaiLieu tg WHERE tg.tenTacGia = LTRIM(RTRIM(t.tacGia)));",

        // 6. Tủ sách cá nhân của độc giả
        @"IF OBJECT_ID('TaiLieuTuSach','U') IS NULL
          BEGIN
            CREATE TABLE TaiLieuTuSach
            (
                maDocGia VARCHAR(20) NOT NULL,
                maTaiLieu VARCHAR(20) NOT NULL,
                ngayLuu DATETIME2 NOT NULL,
                CONSTRAINT PK_TaiLieuTuSach PRIMARY KEY (maDocGia, maTaiLieu),
                CONSTRAINT FK_TaiLieuTuSach_DocGia FOREIGN KEY (maDocGia)
                    REFERENCES DocGia(maDocGia) ON DELETE CASCADE,
                CONSTRAINT FK_TaiLieuTuSach_TaiLieu FOREIGN KEY (maTaiLieu)
                    REFERENCES TaiLieu(maTaiLieu) ON DELETE CASCADE
            );
            CREATE INDEX IX_TaiLieuTuSach_MaTaiLieu ON TaiLieuTuSach(maTaiLieu);
          END",

        // 7. Bảng nhật ký hoạt động hệ thống
        @"IF OBJECT_ID('NhatKyHoatDong','U') IS NULL
          BEGIN
            CREATE TABLE NhatKyHoatDong
            (
                maNhatKy    BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                maNguoiDung VARCHAR(20) NULL,
                hanhDong    NVARCHAR(50) NOT NULL,
                doiTuong    VARCHAR(50) NULL,
                moTa        NVARCHAR(500) NULL,
                diaChiIp    VARCHAR(50) NULL,
                thoiDiem    DATETIME2 NOT NULL,
                CONSTRAINT FK_NhatKyHoatDong_NguoiDung
                    FOREIGN KEY (maNguoiDung) REFERENCES NguoiDung(maNguoiDung) ON DELETE SET NULL
            );
            CREATE INDEX IX_NhatKyHoatDong_ThoiDiem ON NhatKyHoatDong(thoiDiem);
            CREATE INDEX IX_NhatKyHoatDong_HanhDong ON NhatKyHoatDong(hanhDong);
          END",

        // 8. Lệ phí và trạng thái thanh toán của thẻ thư viện
        @"IF COL_LENGTH('TheThuVien','ngayGiaHan') IS NULL
            ALTER TABLE TheThuVien ADD ngayGiaHan DATE NULL;",

        @"IF COL_LENGTH('TheThuVien','lePhi') IS NULL
            ALTER TABLE TheThuVien ADD lePhi DECIMAL(18,2) NOT NULL CONSTRAINT DF_TheThuVien_lePhi DEFAULT 0;",

        @"IF COL_LENGTH('TheThuVien','daThanhToan') IS NULL
            ALTER TABLE TheThuVien ADD daThanhToan BIT NOT NULL CONSTRAINT DF_TheThuVien_daThanhToan DEFAULT 0;",

        @"IF COL_LENGTH('TheThuVien','ngayThanhToan') IS NULL
            ALTER TABLE TheThuVien ADD ngayThanhToan DATETIME2 NULL;",

        @"IF COL_LENGTH('TheThuVien','ngayGiaHan') IS NOT NULL
            UPDATE TheThuVien SET ngayGiaHan = ngayCap WHERE ngayGiaHan IS NULL;",

        // 9. Thời hạn mượn độc giả chọn khi đăng ký phiếu mượn
        @"IF COL_LENGTH('PhieuMuon','soNgayMuon') IS NULL
            ALTER TABLE PhieuMuon ADD soNgayMuon INT NOT NULL CONSTRAINT DF_PhieuMuon_soNgayMuon DEFAULT 14;"
    };

    /// <summary>Thực thi tuần tự các câu lệnh nâng cấp cấu trúc; bỏ qua nếu đã tồn tại.</summary>
    public static async Task ThucThiAsync(ThuVienDbContext db, ILogger logger)
    {
        foreach (var cauLenh in CauLenh)
        {
            await db.Database.ExecuteSqlRawAsync(cauLenh);
        }

        logger.LogInformation("Đã kiểm tra và nâng cấp cấu trúc cơ sở dữ liệu ThuVienSo.");
    }
}
