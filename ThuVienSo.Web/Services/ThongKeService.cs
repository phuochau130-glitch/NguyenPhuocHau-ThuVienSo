using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Services;

public interface IThongKeService
{
    Task<BaoCaoThongKeViewModel> BaoCaoAsync(DateTime? tuNgay, DateTime? denNgay, string? loaiBaoCao = null);
    Task<ThongKeBangDieuKhienViewModel> ThongKeBangDieuKhienAsync();
    byte[] XuatExcel(BaoCaoThongKeViewModel baoCao);
}

/// <summary>Nghiệp vụ thống kê và xuất báo cáo (UC-15, PH-08).</summary>
public class ThongKeService : IThongKeService
{
    private readonly ThuVienDbContext _db;

    public ThongKeService(ThuVienDbContext db) => _db = db;

    public async Task<BaoCaoThongKeViewModel> BaoCaoAsync(DateTime? tuNgay, DateTime? denNgay, string? loaiBaoCao = null)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        var tu = tuNgay?.Date;
        var den = denNgay?.Date.AddDays(1).AddTicks(-1);

        var baoCao = new BaoCaoThongKeViewModel { TuNgay = tuNgay, DenNgay = denNgay, LoaiBaoCao = loaiBaoCao };

        // Thống kê số lượng tài liệu
        baoCao.TaiLieu.TongSoTaiLieu = await _db.TaiLieu.CountAsync();
        baoCao.TaiLieu.SoSachVatLi = await _db.SachVatLi.CountAsync();
        baoCao.TaiLieu.SoTaiLieuSo = await _db.TaiLieuSo.CountAsync();
        baoCao.TaiLieu.SoTaiLieuGioiHan = await _db.TaiLieu.CountAsync(t => t.LaTaiLieuHanChe);
        baoCao.TaiLieu.TongSoBanSach = await _db.SachVatLi.SumAsync(s => (int?)s.TongSoLuong) ?? 0;
        baoCao.TaiLieu.TongSoBanKhaDung = await _db.SachVatLi.SumAsync(s => (int?)s.SoLuongKhaDung) ?? 0;
        baoCao.TaiLieu.TongLuotTai = await _db.TaiLieuSo.SumAsync(s => (int?)s.SoLuotTai) ?? 0;
        baoCao.TaiLieu.TongLuotXem = await _db.TaiLieu.SumAsync(t => (int?)t.SoLuotXem) ?? 0;
        baoCao.TaiLieu.LuotXemTaiLieuSo = await _db.TaiLieuSo.SumAsync(s => (int?)s.TaiLieu.SoLuotXem) ?? 0;
        baoCao.TaiLieu.LuotXemSachVatLi = await _db.SachVatLi.SumAsync(s => (int?)s.TaiLieu.SoLuotXem) ?? 0;
        baoCao.TaiLieu.SoTaiLieuChoDuyet = await _db.TaiLieu.CountAsync(t => t.TrangThaiDuyet == TrangThaiDuyetTaiLieu.ChoDuyet);
        baoCao.MuonTra.SoLuotQuaHanDaTra = await _db.PhieuMuon.CountAsync(p =>
            p.TrangThai == TrangThaiPhieuMuon.DaTra &&
            p.NgayTraThucTe != null && p.NgayTraThucTe > p.NgayPhaiTra);

        var hoatDongTaiKhoan = _db.NhatKyHoatDong
            .AsNoTracking()
            .Where(k => k.HanhDong == HanhDongNhatKy.DangNhap
                || k.HanhDong == HanhDongNhatKy.DangXuat
                || k.HanhDong == HanhDongNhatKy.DoiMatKhau
                || k.HanhDong == HanhDongNhatKy.CapNhatHoSo);

        if (tu.HasValue) hoatDongTaiKhoan = hoatDongTaiKhoan.Where(k => k.ThoiDiem >= tu.Value);
        if (den.HasValue) hoatDongTaiKhoan = hoatDongTaiKhoan.Where(k => k.ThoiDiem <= den.Value);

        var dangNhapTrongKy = hoatDongTaiKhoan.Where(k => k.HanhDong == HanhDongNhatKy.DangNhap);
        baoCao.TaiKhoan.TongLuotTruyCap = await dangNhapTrongKy.CountAsync();
        baoCao.TaiKhoan.SoTaiKhoanTruyCap = await dangNhapTrongKy
            .Where(k => k.MaNguoiDung != null)
            .Select(k => k.MaNguoiDung!)
            .Distinct()
            .CountAsync();
        baoCao.TaiKhoan.SoLanDoiMatKhau = await hoatDongTaiKhoan
            .CountAsync(k => k.HanhDong == HanhDongNhatKy.DoiMatKhau);
        baoCao.TaiKhoan.SoLanCapNhatHoSo = await hoatDongTaiKhoan
            .CountAsync(k => k.HanhDong == HanhDongNhatKy.CapNhatHoSo);
        baoCao.TaiKhoan.DanhSach = await hoatDongTaiKhoan
            .OrderByDescending(k => k.ThoiDiem)
            .Take(1000)
            .Select(k => new ChiTietHoatDongTaiKhoan
            {
                ThoiDiem = k.ThoiDiem,
                MaNguoiThucHien = k.MaNguoiDung,
                TenNguoiThucHien = k.NguoiDung != null ? k.NguoiDung.HoTen : null,
                MaTaiKhoanTacDong = k.DoiTuong,
                HanhDong = k.HanhDong,
                MoTa = k.MoTa,
                DiaChiIp = k.DiaChiIp
            })
            .ToListAsync();

        baoCao.TaiLieu.TaiLieuXemNhieuNhat = await _db.TaiLieu
            .AsNoTracking()
            .OrderByDescending(t => t.SoLuotXem)
            .Take(10)
            .Select(t => new ThongKeTaiLieuXemNhieu
            {
                MaTaiLieu = t.MaTaiLieu,
                TenTaiLieu = t.TenTaiLieu,
                LoaiTaiLieu = t.BanSach != null ? "Sách vật lý" : t.BanSo != null ? "Tài liệu số" : "Khác",
                SoLuotXem = t.SoLuotXem
            })
            .ToListAsync();

        // Bộ lọc theo khoảng thời gian phát sinh phiếu mượn
        var phieuTrongKy = _db.PhieuMuon
            .AsNoTracking()
            .Include(p => p.DocGia).ThenInclude(d => d.NguoiDung)
            .Include(p => p.Sach).ThenInclude(s => s.TaiLieu)
            .AsQueryable();

        if (tu.HasValue) phieuTrongKy = phieuTrongKy.Where(p => p.NgayMuon >= tu.Value);
        if (den.HasValue) phieuTrongKy = phieuTrongKy.Where(p => p.NgayMuon <= den.Value);

        var danhSachPhieu = await phieuTrongKy
            .OrderByDescending(p => p.NgayMuon)
            .ToListAsync();

        baoCao.MuonTra.TongLuotMuon = danhSachPhieu.Count;
        baoCao.MuonTra.ChoDuyet = danhSachPhieu.Count(p => p.TrangThai == TrangThaiPhieuMuon.ChoDuyet);
        baoCao.MuonTra.DangGiu = danhSachPhieu.Count(p => p.TrangThai == TrangThaiPhieuMuon.DangGiu);
        baoCao.MuonTra.DangMuon = danhSachPhieu.Count(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon);
        baoCao.MuonTra.DaTra = danhSachPhieu.Count(p => p.TrangThai == TrangThaiPhieuMuon.DaTra);
        baoCao.MuonTra.QuaHan = danhSachPhieu.Count(p =>
            p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.NgayPhaiTra < homNay);
        baoCao.MuonTra.TienPhatTraTre = danhSachPhieu.Sum(p => p.TienPhatTraTre);
        baoCao.MuonTra.TienPhatHuHong = danhSachPhieu.Sum(p => p.TienPhatHuHong);
        baoCao.MuonTra.ChiTietPhieuMuon = danhSachPhieu;

        // Danh sách các khoản tiền phạt kèm lý do (trả trễ / hư hỏng sách)
        baoCao.MuonTra.DanhSachPhat = danhSachPhieu
            .Where(p => p.TienPhat > 0)
            .OrderByDescending(p => p.TienPhat)
            .Select(p => new ChiTietPhat
            {
                MaPhieu = p.MaPhieu,
                MaNguoiDung = p.MaDocGia,
                TenDocGia = p.DocGia.NguoiDung.HoTen,
                TenSach = p.Sach.TaiLieu.TenTaiLieu,
                TienPhatTraTre = p.TienPhatTraTre,
                TienPhatHuHong = p.TienPhatHuHong,
                LyDo = LyDoPhat(p),
                NgayTra = p.NgayTraThucTe?.ToDateTime(TimeOnly.MinValue)
            })
            .ToList();

        // Danh sách lượt tải/đọc tài liệu trong kỳ (trích từ nhật ký hoạt động)
        var nhatKyTaiDoc = _db.NhatKyHoatDong
            .AsNoTracking()
            .Where(k => k.HanhDong == HanhDongNhatKy.XemTaiLieu || k.HanhDong == HanhDongNhatKy.TaiTaiLieu);

        if (tu.HasValue) nhatKyTaiDoc = nhatKyTaiDoc.Where(k => k.ThoiDiem >= tu.Value);
        if (den.HasValue) nhatKyTaiDoc = nhatKyTaiDoc.Where(k => k.ThoiDiem <= den.Value);

        baoCao.DanhSachTaiDoc = await nhatKyTaiDoc
            .OrderByDescending(k => k.ThoiDiem)
            .Take(1000)
            .Select(k => new ChiTietTaiDoc
            {
                ThoiDiem = k.ThoiDiem,
                MaNguoiDung = k.MaNguoiDung,
                TenNguoiDung = k.NguoiDung != null ? k.NguoiDung.HoTen : null,
                MaTaiLieu = k.DoiTuong ?? string.Empty,
                TenTaiLieu = k.MoTa ?? string.Empty,
                HanhDong = k.HanhDong
            })
            .ToListAsync();

        // Thống kê theo danh mục tài liệu
        baoCao.TaiLieu.TheoDanhMuc = await _db.TaiLieu
            .AsNoTracking()
            .Where(t => t.DanhMuc != null)
            .GroupBy(t => t.DanhMuc!)
            .Select(g => new ThongKeTheoDanhMuc { DanhMuc = g.Key, SoLuong = g.Count() })
            .OrderByDescending(x => x.SoLuong)
            .ToListAsync();

        // Sách được mượn nhiều nhất trong kỳ
        baoCao.MuonTra.SachMuonNhieuNhat = danhSachPhieu
            .GroupBy(p => p.Sach.TaiLieu.TenTaiLieu)
            .Select(g => new ThongKeSachMuonNhieu { TenSach = g.Key, SoLuotMuon = g.Count() })
            .OrderByDescending(x => x.SoLuotMuon)
            .Take(10)
            .ToList();

        // Thống kê từng tài liệu theo lượt xem, mượn và trả
        var muonTheoTaiLieu = danhSachPhieu
            .GroupBy(p => p.Sach.TaiLieu.MaTaiLieu)
            .ToDictionary(
                g => g.Key,
                g => (
                    Muon: g.Count(),
                    Tra: g.Count(p => p.TrangThai == TrangThaiPhieuMuon.DaTra)));

        baoCao.DanhSachThongKeTaiLieu = (await _db.TaiLieu
                .AsNoTracking()
                .ToListAsync())
            .Select(t =>
            {
                muonTheoTaiLieu.TryGetValue(t.MaTaiLieu, out var so);
                return new ThongKeTaiLieuTheoLuot
                {
                    MaTaiLieu = t.MaTaiLieu,
                    TenTaiLieu = t.TenTaiLieu,
                    LoaiTaiLieu = t.BanSach != null ? "Sách vật lý" : t.BanSo != null ? "Tài liệu số" : "Khác",
                    LuotXem = t.SoLuotXem,
                    LuotMuon = so.Muon,
                    LuotTra = so.Tra
                };
            })
            .OrderByDescending(x => x.LuotXem)
            .ThenByDescending(x => x.LuotMuon)
            .ToList();

        // Thống kê lượt phạt và số tiền phạt theo từng tài khoản
        baoCao.DanhSachPhatTheoTaiKhoan = danhSachPhieu
            .Where(p => p.TienPhat > 0)
            .GroupBy(p => p.MaDocGia)
            .Select(g => new ThongKePhatTheoTaiKhoan
            {
                MaNguoiDung = g.Key,
                TenDocGia = g.First().DocGia.NguoiDung.HoTen,
                Email = g.First().DocGia.NguoiDung.Email,
                SoLuotPhat = g.Count(),
                TienPhatTraTre = g.Sum(p => p.TienPhatTraTre),
                TienPhatHuHong = g.Sum(p => p.TienPhatHuHong)
            })
            .OrderByDescending(x => x.TongTien)
            .ToList();

        return baoCao;
    }

    /// <summary>Số liệu thống kê gọn phục vụ bảng điều khiển (đếm nhanh, không nạp chi tiết lớn).</summary>
    public async Task<ThongKeBangDieuKhienViewModel> ThongKeBangDieuKhienAsync()
    {
        var model = new ThongKeBangDieuKhienViewModel
        {
            SoTaiLieuSo = await _db.TaiLieuSo.CountAsync(),
            SoSachVatLi = await _db.SachVatLi.CountAsync(),
            TongLuotXem = await _db.TaiLieu.SumAsync(t => (int?)t.SoLuotXem) ?? 0,
            LuotXemTaiLieuSo = await _db.TaiLieuSo.SumAsync(s => (int?)s.TaiLieu.SoLuotXem) ?? 0,
            LuotXemSachVatLi = await _db.SachVatLi.SumAsync(s => (int?)s.TaiLieu.SoLuotXem) ?? 0,
            TongLuotTai = await _db.TaiLieuSo.SumAsync(s => (int?)s.SoLuotTai) ?? 0,
            TongLuotMuon = await _db.PhieuMuon.CountAsync(),
            TongLuotTra = await _db.PhieuMuon.CountAsync(p => p.TrangThai == TrangThaiPhieuMuon.DaTra),
            TongTienPhat = await _db.PhieuMuon.SumAsync(p => (decimal?)(p.TienPhatTraTre + p.TienPhatHuHong)) ?? 0m
        };

        model.SachMuonNhieuNhat = await _db.PhieuMuon
            .AsNoTracking()
            .GroupBy(p => p.Sach.TaiLieu.TenTaiLieu)
            .Select(g => new ThongKeSachMuonNhieu { TenSach = g.Key, SoLuotMuon = g.Count() })
            .OrderByDescending(x => x.SoLuotMuon)
            .Take(5)
            .ToListAsync();

        model.TaiLieuXemNhieuNhat = await _db.TaiLieu
            .AsNoTracking()
            .OrderByDescending(t => t.SoLuotXem)
            .Take(5)
            .Select(t => new ThongKeTaiLieuXemNhieu
            {
                MaTaiLieu = t.MaTaiLieu,
                TenTaiLieu = t.TenTaiLieu,
                LoaiTaiLieu = t.BanSach != null ? "Sách vật lý" : t.BanSo != null ? "Tài liệu số" : "Khác",
                SoLuotXem = t.SoLuotXem
            })
            .ToListAsync();

        return model;
    }

    /// <summary>Diễn giải lý do phạt của một phiếu mượn dựa trên các khoản tiền phạt phát sinh.</summary>
    private static string LyDoPhat(PhieuMuon p)
    {
        var lyDo = new List<string>();
        if (p.TienPhatTraTre > 0) lyDo.Add("Trả sách trễ hạn");
        if (p.TienPhatHuHong > 0) lyDo.Add("Hư hỏng sách");
        return lyDo.Count == 0 ? "Không" : string.Join(" + ", lyDo);
    }

    public byte[] XuatExcel(BaoCaoThongKeViewModel baoCao)
    {
        using var workbook = new XLWorkbook();

        var loai = baoCao.LoaiBaoCao;
        var tatCa = string.IsNullOrWhiteSpace(loai);
        var xuatTongHop = tatCa || loai == "thongke";
        var xuatMuonTra = tatCa || loai == "muontra";
        var xuatPhat = tatCa || loai == "phat";
        var xuatTaiDoc = tatCa || loai == "taidoc";
        var xuatTaiKhoan = tatCa || loai == "taikhoan";

        if (xuatTongHop)
        {
        var sheet1 = workbook.Worksheets.Add("Tổng hợp");
        sheet1.Cell(1, 1).Value = "BÁO CÁO THỐNG KÊ HOẠT ĐỘNG THƯ VIỆN SỐ";
        sheet1.Cell(1, 1).Style.Font.Bold = true;
        sheet1.Cell(1, 1).Style.Font.FontSize = 14;
        sheet1.Range(1, 1, 1, 2).Merge();

        var khoang = baoCao.TuNgay.HasValue || baoCao.DenNgay.HasValue
            ? $"Khoảng thời gian: {baoCao.TuNgay:dd/MM/yyyy} - {baoCao.DenNgay:dd/MM/yyyy}"
            : "Khoảng thời gian: toàn bộ dữ liệu";
        sheet1.Cell(2, 1).Value = khoang;
        sheet1.Range(2, 1, 2, 2).Merge();

        var dong = 4;
        void TieuDe(string ten)
        {
            sheet1.Cell(dong, 1).Value = ten;
            sheet1.Cell(dong, 1).Style.Font.Bold = true;
            sheet1.Cell(dong, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            sheet1.Cell(dong, 2).Style.Fill.BackgroundColor = XLColor.LightGray;
            dong++;
        }

        void DongSo(string nhan, object giaTri)
        {
            sheet1.Cell(dong, 1).Value = nhan;
            sheet1.Cell(dong, 2).Value = XLCellValue.FromObject(giaTri);
            dong++;
        }

        TieuDe("I. Thống kê số lượng tài liệu");
        DongSo("Tổng số tài liệu", baoCao.TaiLieu.TongSoTaiLieu);
        DongSo("Số sách vật lý", baoCao.TaiLieu.SoSachVatLi);
        DongSo("Số tài liệu số", baoCao.TaiLieu.SoTaiLieuSo);
        DongSo("Tài liệu bị giới hạn truy cập", baoCao.TaiLieu.SoTaiLieuGioiHan);
        DongSo("Tài liệu không giới hạn", baoCao.TaiLieu.SoTaiLieuKhongGioiHan);
        DongSo("Tổng số bản sách", baoCao.TaiLieu.TongSoBanSach);
        DongSo("Số bản sách khả dụng", baoCao.TaiLieu.TongSoBanKhaDung);
        DongSo("Tổng lượt xem/đọc", baoCao.TaiLieu.TongLuotXem);
        DongSo("Lượt xem tài liệu số", baoCao.TaiLieu.LuotXemTaiLieuSo);
        DongSo("Lượt xem sách vật lý", baoCao.TaiLieu.LuotXemSachVatLi);
        DongSo("Tổng lượt tải tài liệu số", baoCao.TaiLieu.TongLuotTai);
        DongSo("Tài liệu chờ kiểm duyệt", baoCao.TaiLieu.SoTaiLieuChoDuyet);

        dong++;
        TieuDe("II. Thống kê hoạt động mượn trả");
        DongSo("Tổng lượt mượn", baoCao.MuonTra.TongLuotMuon);
        DongSo("Phiếu chờ duyệt", baoCao.MuonTra.ChoDuyet);
        DongSo("Phiếu đang giữ sách", baoCao.MuonTra.DangGiu);
        DongSo("Phiếu đang mượn", baoCao.MuonTra.DangMuon);
        DongSo("Phiếu đã trả", baoCao.MuonTra.DaTra);
        DongSo("Phiếu quá hạn chưa trả", baoCao.MuonTra.QuaHan);
        DongSo("Lượt trả trễ hạn", baoCao.MuonTra.SoLuotQuaHanDaTra);
        DongSo("Tiền phạt trả trễ (VNĐ)", baoCao.MuonTra.TienPhatTraTre);
        DongSo("Tiền phạt hư hỏng (VNĐ)", baoCao.MuonTra.TienPhatHuHong);
        DongSo("Tổng tiền phạt (VNĐ)", baoCao.MuonTra.TongTienPhat);

        dong++;
        TieuDe("III. Thống kê theo danh mục");
        if (baoCao.TaiLieu.TheoDanhMuc.Count == 0)
        {
            DongSo("Không có dữ liệu", 0);
        }
        else
        {
            foreach (var item in baoCao.TaiLieu.TheoDanhMuc)
            {
                DongSo(item.DanhMuc, item.SoLuong);
            }
        }

        sheet1.Columns().AdjustToContents();
        }

        if (xuatMuonTra)
        {
        var sheet2 = workbook.Worksheets.Add("Chi tiết mượn trả");
        string[] tieuDeCot =
        {
            "Mã phiếu", "Độc giả", "Mã sinh viên/cán bộ", "Tên sách",
            "Ngày mượn", "Hạn trả", "Ngày trả", "Trạng thái",
            "Phạt trả trễ (VNĐ)", "Phạt hư hỏng (VNĐ)", "Tổng phạt (VNĐ)"
        };
        for (var i = 0; i < tieuDeCot.Length; i++)
        {
            sheet2.Cell(1, i + 1).Value = tieuDeCot[i];
            sheet2.Cell(1, i + 1).Style.Font.Bold = true;
            sheet2.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        var r = 2;
        foreach (var p in baoCao.MuonTra.ChiTietPhieuMuon)
        {
            sheet2.Cell(r, 1).Value = p.MaPhieu;
            sheet2.Cell(r, 2).Value = p.DocGia.NguoiDung.HoTen;
            sheet2.Cell(r, 3).Value = p.DocGia.MaSinhVienHoacCanBo;
            sheet2.Cell(r, 4).Value = p.Sach.TaiLieu.TenTaiLieu;
            sheet2.Cell(r, 5).Value = p.NgayMuon;
            sheet2.Cell(r, 5).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            sheet2.Cell(r, 6).Value = p.NgayPhaiTra.ToDateTime(TimeOnly.MinValue);
            sheet2.Cell(r, 6).Style.DateFormat.Format = "dd/MM/yyyy";
            if (p.NgayTraThucTe.HasValue)
            {
                sheet2.Cell(r, 7).Value = p.NgayTraThucTe.Value.ToDateTime(TimeOnly.MinValue);
                sheet2.Cell(r, 7).Style.DateFormat.Format = "dd/MM/yyyy";
            }
            sheet2.Cell(r, 8).Value = p.TrangThai;
            sheet2.Cell(r, 9).Value = p.TienPhatTraTre;
            sheet2.Cell(r, 10).Value = p.TienPhatHuHong;
            sheet2.Cell(r, 11).Value = p.TienPhat;
            r++;
        }

        sheet2.Columns().AdjustToContents();
        }

        if (xuatPhat)
        {
        // Sheet 3: Danh sách phạt tiền kèm lý do (hư hỏng / trả trễ)
        var sheet3 = workbook.Worksheets.Add("Danh sách phạt");
        string[] tieuDePhat =
        {
            "Mã phiếu", "Mã độc giả", "Độc giả", "Tên sách",
            "Ngày trả", "Lý do phạt", "Phạt trả trễ (VNĐ)", "Phạt hư hỏng (VNĐ)", "Tổng phạt (VNĐ)"
        };
        for (var i = 0; i < tieuDePhat.Length; i++)
        {
            sheet3.Cell(1, i + 1).Value = tieuDePhat[i];
            sheet3.Cell(1, i + 1).Style.Font.Bold = true;
            sheet3.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        var rp = 2;
        foreach (var p in baoCao.MuonTra.DanhSachPhat)
        {
            sheet3.Cell(rp, 1).Value = p.MaPhieu;
            sheet3.Cell(rp, 2).Value = p.MaNguoiDung ?? string.Empty;
            sheet3.Cell(rp, 3).Value = p.TenDocGia;
            sheet3.Cell(rp, 4).Value = p.TenSach;
            if (p.NgayTra.HasValue)
            {
                sheet3.Cell(rp, 5).Value = p.NgayTra.Value;
                sheet3.Cell(rp, 5).Style.DateFormat.Format = "dd/MM/yyyy";
            }
            sheet3.Cell(rp, 6).Value = p.LyDo;
            sheet3.Cell(rp, 7).Value = p.TienPhatTraTre;
            sheet3.Cell(rp, 8).Value = p.TienPhatHuHong;
            sheet3.Cell(rp, 9).Value = p.TongTien;
            rp++;
        }

        sheet3.Columns().AdjustToContents();

        var sheet3b = workbook.Worksheets.Add("Phạt theo tài khoản");
        string[] tieuDePhatTk =
        {
            "Mã người dùng", "Độc giả", "Email", "Lượt phạt",
            "Phạt trả trễ (VNĐ)", "Phạt hư hỏng (VNĐ)", "Tổng phạt (VNĐ)"
        };
        for (var i = 0; i < tieuDePhatTk.Length; i++)
        {
            sheet3b.Cell(1, i + 1).Value = tieuDePhatTk[i];
            sheet3b.Cell(1, i + 1).Style.Font.Bold = true;
            sheet3b.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        var rpk = 2;
        foreach (var t in baoCao.DanhSachPhatTheoTaiKhoan)
        {
            sheet3b.Cell(rpk, 1).Value = t.MaNguoiDung;
            sheet3b.Cell(rpk, 2).Value = t.TenDocGia;
            sheet3b.Cell(rpk, 3).Value = t.Email ?? string.Empty;
            sheet3b.Cell(rpk, 4).Value = t.SoLuotPhat;
            sheet3b.Cell(rpk, 5).Value = t.TienPhatTraTre;
            sheet3b.Cell(rpk, 6).Value = t.TienPhatHuHong;
            sheet3b.Cell(rpk, 7).Value = t.TongTien;
            rpk++;
        }

        sheet3b.Columns().AdjustToContents();
        }

        if (xuatTaiDoc)
        {
        // Sheet 4: Danh sách lượt tải/đọc tài liệu
        var sheet4 = workbook.Worksheets.Add("Lượt tải - đọc");
        string[] tieuDeTaiDoc = { "Thời điểm", "Mã người dùng", "Người dùng", "Mã tài liệu", "Tên tài liệu", "Hành động" };
        for (var i = 0; i < tieuDeTaiDoc.Length; i++)
        {
            sheet4.Cell(1, i + 1).Value = tieuDeTaiDoc[i];
            sheet4.Cell(1, i + 1).Style.Font.Bold = true;
            sheet4.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        var rt = 2;
        foreach (var t in baoCao.DanhSachTaiDoc)
        {
            sheet4.Cell(rt, 1).Value = t.ThoiDiem;
            sheet4.Cell(rt, 1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            sheet4.Cell(rt, 2).Value = t.MaNguoiDung ?? string.Empty;
            sheet4.Cell(rt, 3).Value = t.TenNguoiDung ?? string.Empty;
            sheet4.Cell(rt, 4).Value = t.MaTaiLieu;
            sheet4.Cell(rt, 5).Value = t.TenTaiLieu;
            sheet4.Cell(rt, 6).Value = t.HanhDong;
            rt++;
        }

        sheet4.Columns().AdjustToContents();

        // Sheet 5: Tài liệu được xem/đọc nhiều nhất
        var sheet5 = workbook.Worksheets.Add("Xem nhiều nhất");
        string[] tieuDeXemNhieu = { "Mã tài liệu", "Tên tài liệu", "Loại", "Lượt xem/đọc" };
        for (var i = 0; i < tieuDeXemNhieu.Length; i++)
        {
            sheet5.Cell(1, i + 1).Value = tieuDeXemNhieu[i];
            sheet5.Cell(1, i + 1).Style.Font.Bold = true;
            sheet5.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        var rx = 2;
        foreach (var t in baoCao.TaiLieu.TaiLieuXemNhieuNhat)
        {
            sheet5.Cell(rx, 1).Value = t.MaTaiLieu;
            sheet5.Cell(rx, 2).Value = t.TenTaiLieu;
            sheet5.Cell(rx, 3).Value = t.LoaiTaiLieu;
            sheet5.Cell(rx, 4).Value = t.SoLuotXem;
            rx++;
        }

        sheet5.Columns().AdjustToContents();
        }

        if (xuatTaiKhoan)
        {
            var sheetTaiKhoan = workbook.Worksheets.Add("Truy cập tài khoản");
            sheetTaiKhoan.Cell(1, 1).Value = "TỔNG HỢP HOẠT ĐỘNG TÀI KHOẢN";
            sheetTaiKhoan.Cell(1, 1).Style.Font.Bold = true;
            sheetTaiKhoan.Cell(1, 1).Style.Font.FontSize = 14;
            sheetTaiKhoan.Range(1, 1, 1, 2).Merge();
            sheetTaiKhoan.Cell(2, 1).Value = "Lượt đăng nhập";
            sheetTaiKhoan.Cell(2, 2).Value = baoCao.TaiKhoan.TongLuotTruyCap;
            sheetTaiKhoan.Cell(3, 1).Value = "Tài khoản đã đăng nhập";
            sheetTaiKhoan.Cell(3, 2).Value = baoCao.TaiKhoan.SoTaiKhoanTruyCap;
            sheetTaiKhoan.Cell(4, 1).Value = "Lượt đổi/đặt lại mật khẩu";
            sheetTaiKhoan.Cell(4, 2).Value = baoCao.TaiKhoan.SoLanDoiMatKhau;
            sheetTaiKhoan.Cell(5, 1).Value = "Lượt cập nhật hồ sơ";
            sheetTaiKhoan.Cell(5, 2).Value = baoCao.TaiKhoan.SoLanCapNhatHoSo;

            string[] tieuDeHoatDong = { "Thời điểm", "Người thực hiện", "Mã tài khoản bị tác động", "Hành động", "Mô tả", "Địa chỉ IP" };
            for (var i = 0; i < tieuDeHoatDong.Length; i++)
            {
                sheetTaiKhoan.Cell(7, i + 1).Value = tieuDeHoatDong[i];
                sheetTaiKhoan.Cell(7, i + 1).Style.Font.Bold = true;
                sheetTaiKhoan.Cell(7, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            var dongHoatDong = 8;
            foreach (var item in baoCao.TaiKhoan.DanhSach)
            {
                sheetTaiKhoan.Cell(dongHoatDong, 1).Value = item.ThoiDiem;
                sheetTaiKhoan.Cell(dongHoatDong, 1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                sheetTaiKhoan.Cell(dongHoatDong, 2).Value = item.TenNguoiThucHien ?? item.MaNguoiThucHien ?? string.Empty;
                sheetTaiKhoan.Cell(dongHoatDong, 3).Value = item.MaTaiKhoanTacDong ?? string.Empty;
                sheetTaiKhoan.Cell(dongHoatDong, 4).Value = item.HanhDong;
                sheetTaiKhoan.Cell(dongHoatDong, 5).Value = item.MoTa ?? string.Empty;
                sheetTaiKhoan.Cell(dongHoatDong, 6).Value = item.DiaChiIp ?? string.Empty;
                dongHoatDong++;
            }

            sheetTaiKhoan.Columns().AdjustToContents();
        }

        if (tatCa)
        {
            var sheet6 = workbook.Worksheets.Add("Thống kê tài liệu");
            string[] tieuDeTl = { "Mã tài liệu", "Tên tài liệu", "Loại", "Lượt xem", "Lượt mượn", "Lượt trả" };
            for (var i = 0; i < tieuDeTl.Length; i++)
            {
                sheet6.Cell(1, i + 1).Value = tieuDeTl[i];
                sheet6.Cell(1, i + 1).Style.Font.Bold = true;
                sheet6.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            var rtl = 2;
            foreach (var t in baoCao.DanhSachThongKeTaiLieu)
            {
                sheet6.Cell(rtl, 1).Value = t.MaTaiLieu;
                sheet6.Cell(rtl, 2).Value = t.TenTaiLieu;
                sheet6.Cell(rtl, 3).Value = t.LoaiTaiLieu;
                sheet6.Cell(rtl, 4).Value = t.LuotXem;
                sheet6.Cell(rtl, 5).Value = t.LuotMuon;
                sheet6.Cell(rtl, 6).Value = t.LuotTra;
                rtl++;
            }

            sheet6.Columns().AdjustToContents();
        }

        using var luong = new MemoryStream();
        workbook.SaveAs(luong);
        return luong.ToArray();
    }
}
