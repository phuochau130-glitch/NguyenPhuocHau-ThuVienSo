using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Models;

namespace ThuVienSo.Web.Data;

/// <summary>
/// Ngữ cảnh dữ liệu ánh xạ tường minh tới cơ sở dữ liệu ThuVienSo.
/// Mô hình bảng kế thừa (TPT): NguoiDung -> DocGia/ThuThu/QuanTriVien, TaiLieu -> SachVatLi/TaiLieuSo.
/// </summary>
public class ThuVienDbContext : DbContext
{
    public ThuVienDbContext(DbContextOptions<ThuVienDbContext> options) : base(options)
    {
    }

    public DbSet<NguoiDung> NguoiDung => Set<NguoiDung>();
    public DbSet<DocGia> DocGia => Set<DocGia>();
    public DbSet<ThuThu> ThuThu => Set<ThuThu>();
    public DbSet<QuanTriVien> QuanTriVien => Set<QuanTriVien>();
    public DbSet<TheThuVien> TheThuVien => Set<TheThuVien>();
    public DbSet<TaiLieu> TaiLieu => Set<TaiLieu>();
    public DbSet<DanhMucTaiLieu> DanhMucTaiLieu => Set<DanhMucTaiLieu>();
    public DbSet<TacGiaTaiLieu> TacGiaTaiLieu => Set<TacGiaTaiLieu>();
    public DbSet<SachVatLi> SachVatLi => Set<SachVatLi>();
    public DbSet<TaiLieuSo> TaiLieuSo => Set<TaiLieuSo>();
    public DbSet<TaiLieuTuSach> TaiLieuTuSach => Set<TaiLieuTuSach>();
    public DbSet<PhieuMuon> PhieuMuon => Set<PhieuMuon>();
    public DbSet<YeuCauTaiLieu> YeuCauTaiLieu => Set<YeuCauTaiLieu>();
    public DbSet<NhatKyHoatDong> NhatKyHoatDong => Set<NhatKyHoatDong>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        CauHinhNguoiDung(modelBuilder);
        CauHinhTaiLieu(modelBuilder);
        CauHinhDanhMucTacGia(modelBuilder);
        CauHinhTuSach(modelBuilder);
        CauHinhTheThuVien(modelBuilder);
        CauHinhPhieuMuon(modelBuilder);
        CauHinhYeuCauTaiLieu(modelBuilder);
        CauHinhNhatKyHoatDong(modelBuilder);
    }

    private static void CauHinhDanhMucTacGia(ModelBuilder b)
    {
        b.Entity<DanhMucTaiLieu>(e =>
        {
            e.ToTable("DanhMucTaiLieu");
            e.HasKey(x => x.MaDanhMuc);
            e.Property(x => x.MaDanhMuc).HasColumnName("maDanhMuc").ValueGeneratedOnAdd();
            e.Property(x => x.TenDanhMuc).HasColumnName("tenDanhMuc").HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.TenDanhMuc).IsUnique();
        });

        b.Entity<TacGiaTaiLieu>(e =>
        {
            e.ToTable("TacGiaTaiLieu");
            e.HasKey(x => x.MaTacGia);
            e.Property(x => x.MaTacGia).HasColumnName("maTacGia").ValueGeneratedOnAdd();
            e.Property(x => x.TenTacGia).HasColumnName("tenTacGia").HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.TenTacGia).IsUnique();
        });
    }

    private static void CauHinhNguoiDung(ModelBuilder b)
    {
        b.Entity<NguoiDung>(e =>
        {
            e.ToTable("NguoiDung");
            e.HasKey(x => x.MaNguoiDung);
            e.Property(x => x.MaNguoiDung).HasColumnName("maNguoiDung").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.HoTen).HasColumnName("hoTen").HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(100).IsUnicode(false).IsRequired();
            e.Property(x => x.SoDienThoai).HasColumnName("soDienThoai").HasMaxLength(15).IsUnicode(false);
            e.Property(x => x.MatKhau).HasColumnName("matKhau").HasMaxLength(255).IsUnicode(false).IsRequired();
            e.Property(x => x.TrangThai).HasColumnName("trangThai").HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Ignore(x => x.VaiTro);
        });

        b.Entity<DocGia>(e =>
        {
            e.ToTable("DocGia");
            e.HasKey(x => x.MaDocGia);
            e.Property(x => x.MaDocGia).HasColumnName("maDocGia").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.MaSinhVienHoacCanBo).HasColumnName("maSinhVienHoacCanBo").HasMaxLength(30).IsUnicode(false).IsRequired();
            e.Property(x => x.Khoa).HasColumnName("khoa").HasMaxLength(100);
            e.Property(x => x.KhoaMuonDen).HasColumnName("khoaMuonDen");
            e.HasIndex(x => x.MaSinhVienHoacCanBo).IsUnique();
            e.Ignore(x => x.VaiTro);
            e.HasOne(x => x.NguoiDung)
                .WithOne(n => n.HoSoDocGia)
                .HasForeignKey<DocGia>(x => x.MaDocGia)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ThuThu>(e =>
        {
            e.ToTable("ThuThu");
            e.HasKey(x => x.MaThuThu);
            e.Property(x => x.MaThuThu).HasColumnName("maThuThu").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.MaNhanVien).HasColumnName("maNhanVien").HasMaxLength(30).IsUnicode(false).IsRequired();
            e.Property(x => x.BoPhan).HasColumnName("boPhan").HasMaxLength(100);
            e.HasIndex(x => x.MaNhanVien).IsUnique();
            e.HasOne(x => x.NguoiDung)
                .WithOne(n => n.HoSoThuThu)
                .HasForeignKey<ThuThu>(x => x.MaThuThu)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<QuanTriVien>(e =>
        {
            e.ToTable("QuanTriVien");
            e.HasKey(x => x.MaAdmin);
            e.Property(x => x.MaAdmin).HasColumnName("maAdmin").HasMaxLength(20).IsUnicode(false);
            e.HasOne(x => x.NguoiDung)
                .WithOne(n => n.HoSoQuanTri)
                .HasForeignKey<QuanTriVien>(x => x.MaAdmin)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void CauHinhTaiLieu(ModelBuilder b)
    {
        b.Entity<TaiLieu>(e =>
        {
            e.ToTable("TaiLieu");
            e.HasKey(x => x.MaTaiLieu);
            e.Property(x => x.MaTaiLieu).HasColumnName("maTaiLieu").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.TenTaiLieu).HasColumnName("tenTaiLieu").HasMaxLength(255).IsRequired();
            e.Property(x => x.TacGia).HasColumnName("tacGia").HasMaxLength(200);
            e.Property(x => x.NhaXuatBan).HasColumnName("nhaXuatBan").HasMaxLength(150);
            e.Property(x => x.NamXuatBan).HasColumnName("namXuatBan");
            e.Property(x => x.DanhMuc).HasColumnName("danhMuc").HasMaxLength(100);
            e.Property(x => x.LaTaiLieuHanChe).HasColumnName("laTaiLieuHanChe").HasDefaultValue(false);
            e.Property(x => x.SoLuotXem).HasColumnName("soLuotXem").HasDefaultValue(0);
            e.Property(x => x.DuongDanAnhBia).HasColumnName("duongDanAnhBia").HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.GhiChu).HasColumnName("ghiChu").HasMaxLength(500);
            e.Property(x => x.TrangThaiDuyet).HasColumnName("trangThaiDuyet").HasMaxLength(30).HasDefaultValue(TrangThaiDuyetTaiLieu.DaDuyet).IsRequired();
            e.Property(x => x.MaNguoiDang).HasColumnName("maNguoiDang").HasMaxLength(20).IsUnicode(false);
            e.Ignore(x => x.LoaiTaiLieu);
            e.HasOne(x => x.NguoiDang)
                .WithMany(d => d.DanhSachTaiLieuDongGop)
                .HasForeignKey(x => x.MaNguoiDang)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<SachVatLi>(e =>
        {
            e.ToTable("SachVatLi");
            e.HasKey(x => x.MaSach);
            e.Property(x => x.MaSach).HasColumnName("maSach").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.Isbn).HasColumnName("isbn").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.MaVach).HasColumnName("maVach").HasMaxLength(50).IsUnicode(false);
            e.Property(x => x.ViTriKeSach).HasColumnName("viTriKeSach").HasMaxLength(100);
            e.Property(x => x.TongSoLuong).HasColumnName("tongSoLuong").HasDefaultValue(0);
            e.Property(x => x.SoLuongKhaDung).HasColumnName("soLuongKhaDung").HasDefaultValue(0);
            e.Property(x => x.DuongDanTepXemTruoc).HasColumnName("duongDanTepXemTruoc").HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.DinhDangTepXemTruoc).HasColumnName("dinhDangTepXemTruoc").HasMaxLength(10).IsUnicode(false);
            e.HasIndex(x => x.MaVach)
                .IsUnique()
                .HasFilter("[maVach] IS NOT NULL");
            e.HasOne(x => x.TaiLieu)
                .WithOne(t => t.BanSach)
                .HasForeignKey<SachVatLi>(x => x.MaSach)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<TaiLieuSo>(e =>
        {
            e.ToTable("TaiLieuSo");
            e.HasKey(x => x.MaTaiLieuSo);
            e.Property(x => x.MaTaiLieuSo).HasColumnName("maTaiLieuSo").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.DuongDanFile).HasColumnName("duongDanFile").HasMaxLength(500).IsUnicode(false).IsRequired();
            e.Property(x => x.DungLuongFile).HasColumnName("dungLuongFile");
            e.Property(x => x.DinhDang).HasColumnName("dinhDang").HasMaxLength(10).IsUnicode(false);
            e.Property(x => x.SoLuotTai).HasColumnName("soLuotTai").HasDefaultValue(0);
            e.HasOne(x => x.TaiLieu)
                .WithOne(t => t.BanSo)
                .HasForeignKey<TaiLieuSo>(x => x.MaTaiLieuSo)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void CauHinhTheThuVien(ModelBuilder b)
    {
        b.Entity<TheThuVien>(e =>
        {
            e.ToTable("TheThuVien");
            e.HasKey(x => x.MaThe);
            e.Property(x => x.MaThe).HasColumnName("maThe").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.MaDocGia).HasColumnName("maDocGia").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.NgayCap).HasColumnName("ngayCap");
            e.Property(x => x.NgayGiaHan).HasColumnName("ngayGiaHan");
            e.Property(x => x.NgayHetHan).HasColumnName("ngayHetHan");
            e.Property(x => x.TrangThai).HasColumnName("trangThai").HasMaxLength(30).IsRequired();
            e.Property(x => x.LePhi).HasColumnName("lePhi").HasPrecision(18, 2).HasDefaultValue(0m);
            e.Property(x => x.DaThanhToan).HasColumnName("daThanhToan").HasDefaultValue(false);
            e.Property(x => x.NgayThanhToan).HasColumnName("ngayThanhToan");
            e.Property(x => x.GhiChu).HasColumnName("ghiChu").HasMaxLength(255);
            e.Ignore(x => x.ConHieuLuc);
            e.Ignore(x => x.DaHetHan);
            e.HasIndex(x => x.MaDocGia).IsUnique();
            e.HasOne(x => x.DocGia)
                .WithOne(d => d.TheThuVien)
                .HasForeignKey<TheThuVien>(x => x.MaDocGia)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void CauHinhTuSach(ModelBuilder b)
    {
        b.Entity<TaiLieuTuSach>(e =>
        {
            e.ToTable("TaiLieuTuSach");
            e.HasKey(x => new { x.MaDocGia, x.MaTaiLieu });
            e.Property(x => x.MaDocGia).HasColumnName("maDocGia").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.MaTaiLieu).HasColumnName("maTaiLieu").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.NgayLuu).HasColumnName("ngayLuu");
            e.HasOne(x => x.DocGia).WithMany().HasForeignKey(x => x.MaDocGia).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TaiLieu).WithMany().HasForeignKey(x => x.MaTaiLieu).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void CauHinhPhieuMuon(ModelBuilder b)
    {
        b.Entity<PhieuMuon>(e =>
        {
            e.ToTable("PhieuMuon");
            e.HasKey(x => x.MaPhieu);
            e.Property(x => x.MaPhieu).HasColumnName("maPhieu").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.MaDocGia).HasColumnName("maDocGia").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.MaSach).HasColumnName("maSach").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.MaThuThu).HasColumnName("maThuThu").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.NgayMuon).HasColumnName("ngayMuon");
            e.Property(x => x.NgayPhaiTra).HasColumnName("ngayPhaiTra");
            e.Property(x => x.NgayTraThucTe).HasColumnName("ngayTraThucTe");
            e.Property(x => x.TrangThai).HasColumnName("trangThai").HasMaxLength(50).IsRequired();
            e.Property(x => x.SoNgayMuon).HasColumnName("soNgayMuon").HasDefaultValue(QuyDinh.SoNgayMuon);
            e.Property(x => x.TienPhatTraTre).HasColumnName("tienPhatTraTre").HasPrecision(18, 2).HasDefaultValue(0m);
            e.Property(x => x.TienPhatHuHong).HasColumnName("tienPhatHuHong").HasPrecision(18, 2).HasDefaultValue(0m);
            e.Ignore(x => x.TienPhat);
            e.Ignore(x => x.HanNhanSach);
            e.HasOne(x => x.DocGia).WithMany(d => d.DanhSachPhieuMuon).HasForeignKey(x => x.MaDocGia).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Sach).WithMany(s => s.DanhSachPhieuMuon).HasForeignKey(x => x.MaSach).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ThuThu).WithMany(t => t.DanhSachPhieuMuon).HasForeignKey(x => x.MaThuThu).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void CauHinhYeuCauTaiLieu(ModelBuilder b)
    {
        b.Entity<YeuCauTaiLieu>(e =>
        {
            e.ToTable("YeuCauTaiLieu");
            e.HasKey(x => x.MaYeuCau);
            e.Property(x => x.MaYeuCau).HasColumnName("maYeuCau").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.MaDocGia).HasColumnName("maDocGia").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.MaTaiLieuSo).HasColumnName("maTaiLieuSo").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.MaThuThu).HasColumnName("maThuThu").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.NgayYeuCau).HasColumnName("ngayYeuCau");
            e.Property(x => x.LyDoTruyCap).HasColumnName("lyDoTruyCap").HasMaxLength(500);
            e.Property(x => x.TrangThai).HasColumnName("trangThai").HasMaxLength(30).IsRequired();
            e.HasOne(x => x.DocGia).WithMany(d => d.DanhSachYeuCau).HasForeignKey(x => x.MaDocGia).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TaiLieuSo).WithMany(t => t.DanhSachYeuCau).HasForeignKey(x => x.MaTaiLieuSo).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ThuThu).WithMany(t => t.DanhSachYeuCau).HasForeignKey(x => x.MaThuThu).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void CauHinhNhatKyHoatDong(ModelBuilder b)
    {
        b.Entity<NhatKyHoatDong>(e =>
        {
            e.ToTable("NhatKyHoatDong");
            e.HasKey(x => x.MaNhatKy);
            e.Property(x => x.MaNhatKy).HasColumnName("maNhatKy").ValueGeneratedOnAdd();
            e.Property(x => x.MaNguoiDung).HasColumnName("maNguoiDung").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.HanhDong).HasColumnName("hanhDong").HasMaxLength(50).IsRequired();
            e.Property(x => x.DoiTuong).HasColumnName("doiTuong").HasMaxLength(50).IsUnicode(false);
            e.Property(x => x.MoTa).HasColumnName("moTa").HasMaxLength(500);
            e.Property(x => x.DiaChiIp).HasColumnName("diaChiIp").HasMaxLength(50).IsUnicode(false);
            e.Property(x => x.ThoiDiem).HasColumnName("thoiDiem");
            e.HasIndex(x => x.ThoiDiem);
            e.HasIndex(x => x.HanhDong);
            e.HasOne(x => x.NguoiDung)
                .WithMany()
                .HasForeignKey(x => x.MaNguoiDung)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
