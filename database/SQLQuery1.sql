-- ============================================================
-- SCRIPT CSDL ThuVienSo (bản hợp nhất, chạy lại được nhiều lần)
-- - Chạy trên CSDL trống: tạo đầy đủ bảng + dữ liệu mẫu.
-- - Chạy trên CSDL schema cũ: tự bổ sung bảng / cột còn thiếu,
--   giữ nguyên dữ liệu sẵn có (mọi lệnh đều có điều kiện kiểm tra).
-- Mở file bằng SSMS và Execute toàn bộ, hoặc: sqlcmd -S . -E -i SQLQuery1.sql
-- ============================================================
IF DB_ID(N'ThuVienSo') IS NULL
    CREATE DATABASE ThuVienSo;
GO
USE ThuVienSo;
GO

-- ============================================================
-- 1. TẠO CÁC BẢNG DỮ LIỆU (TABLES)
-- ============================================================

-- Bảng 1: NguoiDung (Lớp cơ sở cha)
IF OBJECT_ID(N'dbo.NguoiDung', N'U') IS NULL
BEGIN
    CREATE TABLE NguoiDung (
        maNguoiDung VARCHAR(20) PRIMARY KEY,
        hoTen NVARCHAR(100) NOT NULL,
        email VARCHAR(100) UNIQUE NOT NULL,
        soDienThoai VARCHAR(15) NULL,
        matKhau VARCHAR(255) NOT NULL,
        trangThai NVARCHAR(30) NOT NULL
    );
END
GO

-- Bảng 2: DocGia (Kế thừa NguoiDung)
IF OBJECT_ID(N'dbo.DocGia', N'U') IS NULL
BEGIN
    CREATE TABLE DocGia (
        maDocGia VARCHAR(20) PRIMARY KEY,
        maSinhVienHoacCanBo VARCHAR(30) UNIQUE NOT NULL,
        khoa NVARCHAR(100) NULL,
        khoaMuonDen DATETIME NULL, -- Khóa quyền đăng ký mượn đến thời điểm này (Quy tắc 6: khóa 2 ngày)
        CONSTRAINT FK_DocGia_NguoiDung FOREIGN KEY (maDocGia) REFERENCES NguoiDung(maNguoiDung) ON DELETE CASCADE
    );
END
GO

-- Bảng 3: ThuThu (Kế thừa NguoiDung)
IF OBJECT_ID(N'dbo.ThuThu', N'U') IS NULL
BEGIN
    CREATE TABLE ThuThu (
        maThuThu VARCHAR(20) PRIMARY KEY,
        maNhanVien VARCHAR(30) UNIQUE NOT NULL,
        boPhan NVARCHAR(100) NULL,
        CONSTRAINT FK_ThuThu_NguoiDung FOREIGN KEY (maThuThu) REFERENCES NguoiDung(maNguoiDung) ON DELETE CASCADE
    );
END
GO

-- Bảng 4: QuanTriVien (Kế thừa NguoiDung)
IF OBJECT_ID(N'dbo.QuanTriVien', N'U') IS NULL
BEGIN
    CREATE TABLE QuanTriVien (
        maAdmin VARCHAR(20) PRIMARY KEY,
        CONSTRAINT FK_QuanTriVien_NguoiDung FOREIGN KEY (maAdmin) REFERENCES NguoiDung(maNguoiDung) ON DELETE CASCADE
    );
END
GO

-- Bảng 5: TheThuVien (Quan hệ 1-1 với DocGia)
IF OBJECT_ID(N'dbo.TheThuVien', N'U') IS NULL
BEGIN
    CREATE TABLE TheThuVien (
        maThe VARCHAR(20) PRIMARY KEY,
        maDocGia VARCHAR(20) UNIQUE NOT NULL,
        ngayCap DATE NOT NULL,
        ngayGiaHan DATE NOT NULL, -- Ngày bắt đầu kỳ hạn hiện tại (cấp / gia hạn gần nhất)
        ngayHetHan DATE NOT NULL,
        trangThai NVARCHAR(30) NOT NULL,
        lePhi DECIMAL(18,2) DEFAULT 0 NOT NULL, -- Lệ phí cấp / gia hạn thẻ theo năm
        daThanhToan BIT DEFAULT 0 NOT NULL,
        ngayThanhToan DATETIME NULL,
        ghiChu NVARCHAR(255) NULL, -- Lý do từ chối / ghi chú xử lý thẻ
        CONSTRAINT FK_TheThuVien_DocGia FOREIGN KEY (maDocGia) REFERENCES DocGia(maDocGia)
    );
END
GO

-- Bảng 6: TaiLieu (Lớp gốc tài liệu chung)
IF OBJECT_ID(N'dbo.TaiLieu', N'U') IS NULL
BEGIN
    CREATE TABLE TaiLieu (
        maTaiLieu VARCHAR(20) PRIMARY KEY,
        tenTaiLieu NVARCHAR(255) NOT NULL,
        tacGia NVARCHAR(200) NULL,
        nhaXuatBan NVARCHAR(150) NULL,
        namXuatBan INT NULL,
        danhMuc NVARCHAR(100) NULL,
        laTaiLieuHanChe BIT DEFAULT 0 NOT NULL, -- 0: Tự do, 1: Giới hạn truy cập
        soLuotXem INT DEFAULT 0 NOT NULL,
        duongDanAnhBia VARCHAR(500) NULL,
        ghiChu NVARCHAR(500) NULL, -- Ghi chú / lý do từ chối kiểm duyệt tài liệu
        trangThaiDuyet NVARCHAR(30) DEFAULT N'Đã duyệt' NOT NULL, -- Chờ duyệt, Đã duyệt, Từ chối (Quy tắc 8)
        maNguoiDang VARCHAR(20) NULL, -- Độc giả đóng góp tài liệu nghiên cứu khoa học
        CONSTRAINT FK_TaiLieu_NguoiDang FOREIGN KEY (maNguoiDang) REFERENCES DocGia(maDocGia)
    );
END
GO

-- Bảng 7: SachVatLi (Kế thừa TaiLieu - Sách bản cứng)
IF OBJECT_ID(N'dbo.SachVatLi', N'U') IS NULL
BEGIN
    CREATE TABLE SachVatLi (
        maSach VARCHAR(20) PRIMARY KEY,
        isbn VARCHAR(20) NULL,
        maVach VARCHAR(50) UNIQUE NULL,
        viTriKeSach NVARCHAR(100) NULL,
        tongSoLuong INT DEFAULT 0 NOT NULL,
        soLuongKhaDung INT DEFAULT 0 NOT NULL,
        duongDanTepXemTruoc VARCHAR(500) NULL,
        dinhDangTepXemTruoc VARCHAR(10) NULL,
        CONSTRAINT FK_SachVatLi_TaiLieu FOREIGN KEY (maSach) REFERENCES TaiLieu(maTaiLieu) ON DELETE CASCADE
    );
END
GO

-- Bảng 8: TaiLieuSo (Kế thừa TaiLieu - Tài liệu điện tử / NCKH)
IF OBJECT_ID(N'dbo.TaiLieuSo', N'U') IS NULL
BEGIN
    CREATE TABLE TaiLieuSo (
        maTaiLieuSo VARCHAR(20) PRIMARY KEY,
        duongDanFile VARCHAR(500) NOT NULL,
        dungLuongFile FLOAT NULL,
        dinhDang VARCHAR(10) NULL,
        soLuotTai INT DEFAULT 0 NOT NULL,
        CONSTRAINT FK_TaiLieuSo_TaiLieu FOREIGN KEY (maTaiLieuSo) REFERENCES TaiLieu(maTaiLieu) ON DELETE CASCADE
    );
END
GO

-- Bảng 9: PhieuMuon (Mượn trả sách vật lý)
IF OBJECT_ID(N'dbo.PhieuMuon', N'U') IS NULL
BEGIN
    CREATE TABLE PhieuMuon (
        maPhieu VARCHAR(20) PRIMARY KEY,
        maDocGia VARCHAR(20) NOT NULL,
        maSach VARCHAR(20) NOT NULL,
        maThuThu VARCHAR(20) NULL, -- Thủ thư phê duyệt / bàn giao
        ngayMuon DATETIME NOT NULL,
        ngayPhaiTra DATE NOT NULL,
        ngayTraThucTe DATE NULL,
        trangThai NVARCHAR(50) NOT NULL, -- Chờ duyệt, Giữ sách, Đang mượn, Đã trả...
        soNgayMuon INT DEFAULT 14 NOT NULL, -- Thời hạn mượn thực tế của phiếu
        tienPhat DECIMAL(18,2) DEFAULT 0 NOT NULL,
        tienPhatTraTre DECIMAL(18,2) DEFAULT 0 NOT NULL,
        tienPhatHuHong DECIMAL(18,2) DEFAULT 0 NOT NULL,
        CONSTRAINT FK_PhieuMuon_DocGia FOREIGN KEY (maDocGia) REFERENCES DocGia(maDocGia),
        CONSTRAINT FK_PhieuMuon_Sach FOREIGN KEY (maSach) REFERENCES SachVatLi(maSach),
        CONSTRAINT FK_PhieuMuon_ThuThu FOREIGN KEY (maThuThu) REFERENCES ThuThu(maThuThu)
    );
END
GO

-- Bảng 10: YeuCauTaiLieu (Xin quyền đọc tài liệu số / NCKH bị giới hạn)
IF OBJECT_ID(N'dbo.YeuCauTaiLieu', N'U') IS NULL
BEGIN
    CREATE TABLE YeuCauTaiLieu (
        maYeuCau VARCHAR(20) PRIMARY KEY,
        maDocGia VARCHAR(20) NOT NULL,
        maTaiLieuSo VARCHAR(20) NOT NULL,
        maThuThu VARCHAR(20) NULL, -- Thủ thư duyệt đơn
        ngayYeuCau DATETIME NOT NULL,
        lyDoTruyCap NVARCHAR(500) NULL,
        trangThai NVARCHAR(30) NOT NULL, -- Chờ duyệt, Đã phê duyệt, Từ chối
        CONSTRAINT FK_YeuCau_DocGia FOREIGN KEY (maDocGia) REFERENCES DocGia(maDocGia),
        CONSTRAINT FK_YeuCau_TaiLieuSo FOREIGN KEY (maTaiLieuSo) REFERENCES TaiLieuSo(maTaiLieuSo),
        CONSTRAINT FK_YeuCau_ThuThu FOREIGN KEY (maThuThu) REFERENCES ThuThu(maThuThu)
    );
END
GO

-- Bảng 11: DanhMucTaiLieu (Danh mục phân loại tài liệu)
IF OBJECT_ID(N'dbo.DanhMucTaiLieu', N'U') IS NULL
BEGIN
    CREATE TABLE DanhMucTaiLieu (
        maDanhMuc INT IDENTITY(1,1) PRIMARY KEY,
        tenDanhMuc NVARCHAR(100) UNIQUE NOT NULL
    );
END
GO

-- Bảng 12: TacGiaTaiLieu (Danh mục tác giả)
IF OBJECT_ID(N'dbo.TacGiaTaiLieu', N'U') IS NULL
BEGIN
    CREATE TABLE TacGiaTaiLieu (
        maTacGia INT IDENTITY(1,1) PRIMARY KEY,
        tenTacGia NVARCHAR(200) UNIQUE NOT NULL
    );
END
GO

-- Bảng 13: TaiLieuTuSach (Tủ sách cá nhân của độc giả)
IF OBJECT_ID(N'dbo.TaiLieuTuSach', N'U') IS NULL
BEGIN
    CREATE TABLE TaiLieuTuSach (
        maDocGia VARCHAR(20) NOT NULL,
        maTaiLieu VARCHAR(20) NOT NULL,
        ngayLuu DATETIME2 NOT NULL,
        CONSTRAINT PK_TaiLieuTuSach PRIMARY KEY (maDocGia, maTaiLieu),
        CONSTRAINT FK_TaiLieuTuSach_DocGia FOREIGN KEY (maDocGia) REFERENCES DocGia(maDocGia) ON DELETE CASCADE,
        CONSTRAINT FK_TaiLieuTuSach_TaiLieu FOREIGN KEY (maTaiLieu) REFERENCES TaiLieu(maTaiLieu) ON DELETE CASCADE
    );
END
GO

-- Bảng 14: NhatKyHoatDong (Nhật ký hành động người dùng)
IF OBJECT_ID(N'dbo.NhatKyHoatDong', N'U') IS NULL
BEGIN
    CREATE TABLE NhatKyHoatDong (
        maNhatKy BIGINT IDENTITY(1,1) PRIMARY KEY,
        maNguoiDung VARCHAR(20) NULL,
        hanhDong NVARCHAR(50) NOT NULL,
        doiTuong VARCHAR(50) NULL,
        moTa NVARCHAR(500) NULL,
        diaChiIp VARCHAR(50) NULL,
        thoiDiem DATETIME2 NOT NULL,
        CONSTRAINT FK_NhatKyHoatDong_NguoiDung FOREIGN KEY (maNguoiDung) REFERENCES NguoiDung(maNguoiDung) ON DELETE SET NULL
    );
    CREATE INDEX IX_NhatKyHoatDong_ThoiDiem ON NhatKyHoatDong(thoiDiem);
    CREATE INDEX IX_NhatKyHoatDong_HanhDong ON NhatKyHoatDong(hanhDong);
END
GO

-- ============================================================
-- 2. NÂNG CẤP SCHEMA CŨ (bổ sung cột còn thiếu, giữ dữ liệu)
-- ============================================================
IF COL_LENGTH(N'dbo.TheThuVien', N'ngayGiaHan') IS NULL
BEGIN
    ALTER TABLE TheThuVien ADD ngayGiaHan DATE NULL;
    UPDATE TheThuVien SET ngayGiaHan = ngayCap;
    ALTER TABLE TheThuVien ALTER COLUMN ngayGiaHan DATE NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.TheThuVien', N'lePhi') IS NULL
BEGIN
    ALTER TABLE TheThuVien ADD
        lePhi DECIMAL(18,2) DEFAULT 0 NOT NULL,
        daThanhToan BIT DEFAULT 0 NOT NULL;
    UPDATE TheThuVien SET lePhi = 40000, daThanhToan = 1
    WHERE trangThai NOT IN (N'Chờ duyệt', N'Từ chối');
END
GO
IF COL_LENGTH(N'dbo.TheThuVien', N'ngayThanhToan') IS NULL
    ALTER TABLE TheThuVien ADD ngayThanhToan DATETIME NULL;
GO
IF COL_LENGTH(N'dbo.TaiLieu', N'soLuotXem') IS NULL
BEGIN
    ALTER TABLE TaiLieu ADD
        soLuotXem INT DEFAULT 0 NOT NULL,
        duongDanAnhBia VARCHAR(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.SachVatLi', N'duongDanTepXemTruoc') IS NULL
BEGIN
    ALTER TABLE SachVatLi ADD
        duongDanTepXemTruoc VARCHAR(500) NULL,
        dinhDangTepXemTruoc VARCHAR(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.PhieuMuon', N'soNgayMuon') IS NULL
    ALTER TABLE PhieuMuon ADD soNgayMuon INT DEFAULT 14 NOT NULL;
GO
IF COL_LENGTH(N'dbo.PhieuMuon', N'tienPhatTraTre') IS NULL
BEGIN
    ALTER TABLE PhieuMuon ADD
        tienPhatTraTre DECIMAL(18,2) DEFAULT 0 NOT NULL,
        tienPhatHuHong DECIMAL(18,2) DEFAULT 0 NOT NULL;
END
GO

-- ============================================================
-- 3. THÊM DỮ LIỆU MẪU (chỉ chèn vào bảng đang trống)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM NguoiDung)
BEGIN
    INSERT INTO NguoiDung (maNguoiDung, hoTen, email, soDienThoai, matKhau, trangThai) VALUES
    ('ND001', N'Nguyễn Văn Admin', 'admin@tdmu.edu.vn', '0901111222', 'hashed_pwd_123', N'Hoạt động'),
    ('ND002', N'Trần Thị Thủ Thư', 'thuthu01@tdmu.edu.vn', '0902222333', 'hashed_pwd_123', N'Hoạt động'),
    ('ND003', N'Lê Hoàng Sinh Viên', 'svhoang@tdmu.edu.vn', '0903333444', 'hashed_pwd_123', N'Hoạt động'),
    ('ND004', N'Phạm Minh Giảng Viên', 'gvminh@tdmu.edu.vn', '0904444555', 'hashed_pwd_123', N'Hoạt động'),
    ('ND005', N'Đặng Thu Hà', 'svha@tdmu.edu.vn', '0905555666', 'hashed_pwd_123', N'Hoạt động');
END
GO
IF NOT EXISTS (SELECT 1 FROM QuanTriVien)
BEGIN
    INSERT INTO QuanTriVien (maAdmin) VALUES ('ND001');
END
GO
IF NOT EXISTS (SELECT 1 FROM ThuThu)
BEGIN
    INSERT INTO ThuThu (maThuThu, maNhanVien, boPhan) VALUES
    ('ND002', 'NV001', N'Quản lý kho sách & Mượn trả');
END
GO
IF NOT EXISTS (SELECT 1 FROM DocGia)
BEGIN
    INSERT INTO DocGia (maDocGia, maSinhVienHoacCanBo, khoa) VALUES
    ('ND003', 'SV2024001', N'Công nghệ Thông tin'),
    ('ND004', 'GV10020', N'Khoa Khoa học Máy tính'),
    ('ND005', 'SV2024002', N'Kinh tế & Quản trị Kinh doanh');
END
GO
IF NOT EXISTS (SELECT 1 FROM TheThuVien)
BEGIN
    INSERT INTO TheThuVien (maThe, maDocGia, ngayCap, ngayGiaHan, ngayHetHan, trangThai, lePhi, daThanhToan) VALUES
    ('TTV001', 'ND003', '2025-09-15', '2025-09-15', '2026-09-15', N'Hoạt động', 40000, 1),
    ('TTV002', 'ND004', '2024-01-10', '2024-01-10', '2027-01-10', N'Hoạt động', 40000, 1),
    ('TTV003', 'ND005', '2025-10-01', '2025-10-01', '2026-10-01', N'Chờ duyệt', 40000, 0);
END
GO
IF NOT EXISTS (SELECT 1 FROM TaiLieu)
BEGIN
    INSERT INTO TaiLieu (maTaiLieu, tenTaiLieu, tacGia, nhaXuatBan, namXuatBan, danhMuc, laTaiLieuHanChe, ghiChu, trangThaiDuyet, maNguoiDang) VALUES
    ('TL001', N'Nhập môn Cơ sở Dữ liệu', N'Nguyễn Kim Anh', N'NXB Giáo Dục', 2022, N'Giáo trình', 0, NULL, N'Đã duyệt', NULL),
    ('TL002', N'Lập trình Phân tán & Microservices', N'Trần Minh Triết', N'NXB ĐHQG', 2023, N'Sách tham khảo', 0, NULL, N'Đã duyệt', NULL),
    ('TL003', N'Tài liệu Ôn tập Cấu trúc Dữ liệu', N'Bộ môn CNTT', N'Nội bộ DLU', 2024, N'Tài liệu ôn tập', 0, NULL, N'Đã duyệt', NULL),
    ('TL004', N'Báo cáo Nghiệp vụ Ứng dụng AI trong Thư viện số', N'Phạm Minh Giảng Viên', N'Tạp chí KHCN', 2025, N'Bài báo NCKH', 1, NULL, N'Đã duyệt', NULL),
    ('TL005', N'Nghiên cứu Ứng dụng Học sâu trong Nhận dạng Ảnh Y tế', N'Đặng Thu Hà', N'Tạp chí KHCN', 2026, N'Bài báo NCKH', 1, NULL, N'Chờ duyệt', 'ND005');
END
GO
IF NOT EXISTS (SELECT 1 FROM SachVatLi)
BEGIN
    INSERT INTO SachVatLi (maSach, isbn, maVach, viTriKeSach, tongSoLuong, soLuongKhaDung) VALUES
    ('TL001', '978-604-0-12345-6', 'MV-TL001-01', N'Kệ A1 - Tầng 2', 10, 8),
    ('TL002', '978-604-0-67890-1', 'MV-TL002-01', N'Kệ B3 - Tầng 2', 5, 4);
END
GO
IF NOT EXISTS (SELECT 1 FROM TaiLieuSo)
BEGIN
    INSERT INTO TaiLieuSo (maTaiLieuSo, duongDanFile, dungLuongFile, dinhDang, soLuotTai) VALUES
    ('TL003', '/uploads/tailieu/on-tap-ctdl-2024.pdf', 12.5, 'PDF', 142),
    ('TL004', '/uploads/nckh/ai-digital-library-2025.pdf', 5.8, 'PDF', 18),
    ('TL005', '/uploads/nckh/hoc-sau-nhan-dang-anh-y-te-2026.pdf', 7.2, 'PDF', 0);
END
GO
IF NOT EXISTS (SELECT 1 FROM PhieuMuon)
BEGIN
    INSERT INTO PhieuMuon (maPhieu, maDocGia, maSach, maThuThu, ngayMuon, ngayPhaiTra, ngayTraThucTe, trangThai, soNgayMuon, tienPhat, tienPhatTraTre, tienPhatHuHong) VALUES
    ('PM001', 'ND003', 'TL001', 'ND002', '2026-03-01 09:30:00', '2026-03-08', '2026-03-07', N'Đã trả', 7, 0, 0, 0),
    ('PM002', 'ND004', 'TL002', 'ND002', '2026-03-05 14:15:00', '2026-03-12', NULL, N'Đang mượn', 7, 0, 0, 0);
END
GO
IF NOT EXISTS (SELECT 1 FROM YeuCauTaiLieu)
BEGIN
    INSERT INTO YeuCauTaiLieu (maYeuCau, maDocGia, maTaiLieuSo, maThuThu, ngayYeuCau, lyDoTruyCap, trangThai) VALUES
    ('YC001', 'ND003', 'TL004', 'ND002', '2026-03-06 10:00:00', N'Phục vụ làm tiểu luận môn Học máy', N'Đã phê duyệt');
END
GO

SELECT * FROM NguoiDung;
GO
