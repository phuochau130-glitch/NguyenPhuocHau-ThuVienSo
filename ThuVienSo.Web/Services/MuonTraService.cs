using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;

namespace ThuVienSo.Web.Services;

public interface IMuonTraService
{
    Task<KetQua> DangKyMuonAsync(string maDocGia, string maSach, int soNgayMuon);
    Task<List<PhieuMuon>> LayPhieuCuaDocGiaAsync(string maDocGia);
    Task<KetQua> HuyPhieuAsync(string maPhieu, string maDocGia);
    Task<List<PhieuMuon>> LayDanhSachAsync(string? trangThai, string? tuKhoa);
    Task<PhieuMuon?> LayTheoMaAsync(string maPhieu);
    Task<KetQua> DuyetGiuSachAsync(string maPhieu, string maThuThu);
    Task<KetQua> TuChoiPhieuAsync(string maPhieu, string maThuThu, string lyDo);
    Task<KetQua> GiaoSachAsync(string maPhieu, string maThuThu);
    Task<KetQua> NhanTraSachAsync(string maPhieu, string maThuThu, decimal phiHuHong, decimal? phiTraTre = null);
    Task<int> XuLyQuaHanNhanSachAsync();
    Task<int> MoKhoaMuonHetHanAsync();
    Task<Dictionary<string, int>> ThongKeNhanhAsync();
}

/// <summary>Nghiệp vụ mượn/trả sách vật lý, giữ sách và xử phạt (PH-06, Quy tắc 5, 6, 7).</summary>
public class MuonTraService : IMuonTraService
{
    private readonly ThuVienDbContext _db;

    public MuonTraService(ThuVienDbContext db) => _db = db;

    public async Task<KetQua> DangKyMuonAsync(string maDocGia, string maSach, int soNgayMuon)
    {
        if (soNgayMuon < QuyDinh.SoNgayMuonToiThieu || soNgayMuon > QuyDinh.SoNgayMuonToiDa)
        {
            return KetQua.Loi($"Thời hạn mượn phải từ {QuyDinh.SoNgayMuonToiThieu} đến {QuyDinh.SoNgayMuonToiDa} ngày.");
        }

        var docGia = await _db.DocGia
            .Include(d => d.NguoiDung)
            .Include(d => d.TheThuVien)
            .FirstOrDefaultAsync(d => d.MaDocGia == maDocGia);

        if (docGia is null) return KetQua.Loi("Không tìm thấy thông tin độc giả.");

        if (docGia.NguoiDung.TrangThai != TrangThaiNguoiDung.HoatDong)
        {
            return KetQua.Loi("Tài khoản của bạn đang bị khóa, không thể đăng ký mượn sách.");
        }

        if (docGia.TheThuVien is null)
        {
            return KetQua.Loi("Bạn chưa có thẻ thư viện. Vui lòng đăng ký thẻ trước khi mượn sách.");
        }

        if (docGia.TheThuVien.TrangThai == TrangThaiThe.ChoDuyet)
        {
            return KetQua.Loi("Bạn cần có thẻ thư viện được phê duyệt trước khi đăng ký mượn sách.");
        }

        if (docGia.TheThuVien.TrangThai == TrangThaiThe.TuChoi)
        {
            return KetQua.Loi("Hồ sơ thẻ thư viện đã bị từ chối. Vui lòng gửi đăng ký lại trước khi mượn sách.");
        }

        if (docGia.TheThuVien.TrangThai == TrangThaiThe.BiKhoa)
        {
            return KetQua.Loi("Thẻ thư viện của bạn đang bị khóa. Vui lòng liên hệ thủ thư.");
        }

        if (docGia.TheThuVien.DaHetHan)
        {
            return KetQua.Loi("Thẻ thư viện đã hết hạn. Vui lòng đăng ký lại trước khi mượn sách.");
        }

        if (!docGia.TheThuVien.ConHieuLuc)
        {
            return KetQua.Loi($"Thẻ thư viện của bạn không còn hiệu lực (trạng thái: {docGia.TheThuVien.TrangThai}).");
        }

        var bayGio = DateTime.Now;
        if (docGia.KhoaMuonDen.HasValue && docGia.KhoaMuonDen.Value > bayGio)
        {
            return KetQua.Loi($"Bạn đang bị khóa quyền đăng ký mượn đến {docGia.KhoaMuonDen.Value:dd/MM/yyyy HH:mm} do không đến nhận sách đúng hạn.");
        }

        var sach = await _db.SachVatLi
            .Include(s => s.TaiLieu)
            .FirstOrDefaultAsync(s => s.MaSach == maSach);

        if (sach is null) return KetQua.Loi("Không tìm thấy sách vật lý cần mượn.");

        if (sach.SoLuongKhaDung <= 0)
        {
            return KetQua.Loi("Sách đã hết bản có thể cho mượn trong kho.");
        }

        var daCoPhieu = await _db.PhieuMuon.AnyAsync(p =>
            p.MaDocGia == maDocGia && p.MaSach == maSach &&
            (p.TrangThai == TrangThaiPhieuMuon.ChoDuyet ||
             p.TrangThai == TrangThaiPhieuMuon.DangGiu ||
             p.TrangThai == TrangThaiPhieuMuon.DangMuon));

        if (daCoPhieu)
        {
            return KetQua.Loi("Bạn đã có một yêu cầu mượn đang xử lý cho cuốn sách này.");
        }

        var maPhieu = MaTuDong.TiepTheo("PM", await _db.PhieuMuon.Select(p => p.MaPhieu).ToListAsync());
        var homNay = DateOnly.FromDateTime(DateTime.Today);

        _db.PhieuMuon.Add(new PhieuMuon
        {
            MaPhieu = maPhieu,
            MaDocGia = maDocGia,
            MaSach = maSach,
            MaThuThu = null,
            NgayMuon = bayGio,
            NgayPhaiTra = homNay.AddDays(soNgayMuon),
            NgayTraThucTe = null,
            TrangThai = TrangThaiPhieuMuon.ChoDuyet,
            SoNgayMuon = soNgayMuon,
            TienPhatTraTre = 0m,
            TienPhatHuHong = 0m
        });

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã gửi yêu cầu mượn sách \"{sach.TaiLieu.TenTaiLieu}\" (phiếu {maPhieu}) với thời hạn {soNgayMuon} ngày. "
            + $"Chúng tôi sẽ giữ sách trong {QuyDinh.SoNgayGiuSach} ngày để bạn đến lấy, vui lòng đến nhận sách.");
    }

    public async Task<List<PhieuMuon>> LayPhieuCuaDocGiaAsync(string maDocGia)
    {
        return await _db.PhieuMuon
            .AsNoTracking()
            .Include(p => p.Sach).ThenInclude(s => s.TaiLieu)
            .Include(p => p.ThuThu).ThenInclude(t => t!.NguoiDung)
            .Where(p => p.MaDocGia == maDocGia)
            .OrderByDescending(p => p.NgayMuon)
            .ToListAsync();
    }

    public async Task<KetQua> HuyPhieuAsync(string maPhieu, string maDocGia)
    {
        var phieu = await _db.PhieuMuon.FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);
        if (phieu is null) return KetQua.Loi("Không tìm thấy phiếu mượn.");
        if (phieu.MaDocGia != maDocGia) return KetQua.Loi("Bạn không có quyền hủy phiếu mượn này.");

        if (phieu.TrangThai != TrangThaiPhieuMuon.ChoDuyet)
        {
            return KetQua.Loi($"Chỉ có thể tự hủy phiếu đang chờ duyệt (trạng thái hiện tại: {phieu.TrangThai}).");
        }

        phieu.TrangThai = TrangThaiPhieuMuon.DaHuy;
        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã hủy phiếu mượn {maPhieu}.");
    }

    public async Task<List<PhieuMuon>> LayDanhSachAsync(string? trangThai, string? tuKhoa)
    {
        var truyVan = _db.PhieuMuon
            .AsNoTracking()
            .Include(p => p.DocGia).ThenInclude(d => d.NguoiDung)
            .Include(p => p.Sach).ThenInclude(s => s.TaiLieu)
            .Include(p => p.ThuThu).ThenInclude(t => t!.NguoiDung)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai))
        {
            truyVan = truyVan.Where(p => p.TrangThai == trangThai);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = tuKhoa.Trim();
            truyVan = truyVan.Where(p =>
                p.MaPhieu.Contains(tk) ||
                p.DocGia.NguoiDung.HoTen.Contains(tk) ||
                p.DocGia.MaSinhVienHoacCanBo.Contains(tk) ||
                p.Sach.TaiLieu.TenTaiLieu.Contains(tk));
        }

        return await truyVan
            .OrderBy(p => p.TrangThai == TrangThaiPhieuMuon.ChoDuyet ? 0 : 1)
            .ThenByDescending(p => p.NgayMuon)
            .ToListAsync();
    }

    public async Task<PhieuMuon?> LayTheoMaAsync(string maPhieu)
    {
        return await _db.PhieuMuon
            .Include(p => p.DocGia).ThenInclude(d => d.NguoiDung)
            .Include(p => p.DocGia).ThenInclude(d => d.TheThuVien)
            .Include(p => p.Sach).ThenInclude(s => s.TaiLieu)
            .Include(p => p.ThuThu).ThenInclude(t => t!.NguoiDung)
            .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);
    }

    public async Task<KetQua> DuyetGiuSachAsync(string maPhieu, string maThuThu)
    {
        var phieu = await _db.PhieuMuon
            .Include(p => p.Sach).ThenInclude(s => s.TaiLieu)
            .Include(p => p.DocGia)
            .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);

        if (phieu is null) return KetQua.Loi("Không tìm thấy phiếu mượn.");

        if (phieu.TrangThai != TrangThaiPhieuMuon.ChoDuyet)
        {
            return KetQua.Loi($"Chỉ có thể duyệt phiếu đang chờ duyệt (trạng thái hiện tại: {phieu.TrangThai}).");
        }

        if (phieu.Sach.SoLuongKhaDung <= 0)
        {
            return KetQua.Loi("Sách đã hết bản khả dụng, không thể giữ sách cho độc giả.");
        }

        phieu.Sach.SoLuongKhaDung -= 1;
        phieu.TrangThai = TrangThaiPhieuMuon.DangGiu;
        phieu.MaThuThu = maThuThu;
        phieu.NgayMuon = DateTime.Now;

        await _db.SaveChangesAsync();

        var hanNhan = phieu.NgayMuon.AddDays(QuyDinh.SoNgayGiuSach);
        return KetQua.Ok($"Đã duyệt phiếu {maPhieu} và giữ sách \"{phieu.Sach.TaiLieu.TenTaiLieu}\" đến {hanNhan:dd/MM/yyyy HH:mm}.");
    }

    public async Task<KetQua> TuChoiPhieuAsync(string maPhieu, string maThuThu, string lyDo)
    {
        if (string.IsNullOrWhiteSpace(lyDo)) return KetQua.Loi("Vui lòng nhập lý do từ chối.");

        var phieu = await _db.PhieuMuon.FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);
        if (phieu is null) return KetQua.Loi("Không tìm thấy phiếu mượn.");

        if (phieu.TrangThai != TrangThaiPhieuMuon.ChoDuyet)
        {
            return KetQua.Loi("Chỉ có thể từ chối phiếu đang chờ duyệt.");
        }

        phieu.TrangThai = TrangThaiPhieuMuon.TuChoi;
        phieu.MaThuThu = maThuThu;
        phieu.NgayTraThucTe = DateOnly.FromDateTime(DateTime.Today);

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã từ chối phiếu mượn {maPhieu}.");
    }

    public async Task<KetQua> GiaoSachAsync(string maPhieu, string maThuThu)
    {
        var phieu = await _db.PhieuMuon
            .Include(p => p.Sach).ThenInclude(s => s.TaiLieu)
            .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);

        if (phieu is null) return KetQua.Loi("Không tìm thấy phiếu mượn.");

        if (phieu.TrangThai != TrangThaiPhieuMuon.DangGiu)
        {
            return KetQua.Loi($"Chỉ có thể giao sách cho phiếu đang giữ (trạng thái hiện tại: {phieu.TrangThai}).");
        }

        var homNay = DateOnly.FromDateTime(DateTime.Today);
        phieu.TrangThai = TrangThaiPhieuMuon.DangMuon;
        phieu.MaThuThu = maThuThu;
        phieu.NgayPhaiTra = homNay.AddDays(phieu.SoNgayMuon > 0 ? phieu.SoNgayMuon : QuyDinh.SoNgayMuon);

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã xác nhận giao sách \"{phieu.Sach.TaiLieu.TenTaiLieu}\". Hạn trả là {phieu.NgayPhaiTra:dd/MM/yyyy}.");
    }

    public async Task<KetQua> NhanTraSachAsync(string maPhieu, string maThuThu, decimal phiHuHong, decimal? phiTraTre = null)
    {
        if (phiHuHong < 0) return KetQua.Loi("Phí hư hỏng không hợp lệ.");
        if (phiTraTre is < 0) return KetQua.Loi("Phí trả trễ không hợp lệ.");

        var phieu = await _db.PhieuMuon
            .Include(p => p.Sach).ThenInclude(s => s.TaiLieu)
            .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu);

        if (phieu is null) return KetQua.Loi("Không tìm thấy phiếu mượn.");

        if (phieu.TrangThai != TrangThaiPhieuMuon.DangMuon)
        {
            return KetQua.Loi($"Chỉ có thể ghi nhận trả sách cho phiếu đang mượn (trạng thái hiện tại: {phieu.TrangThai}).");
        }

        var homNay = DateOnly.FromDateTime(DateTime.Today);
        var soNgayTre = homNay.DayNumber - phieu.NgayPhaiTra.DayNumber;
        if (soNgayTre < 0) soNgayTre = 0;

        var phatQuaHanMacDinh = soNgayTre * QuyDinh.PhatMoiNgay;
        var phatQuaHan = phiTraTre ?? phatQuaHanMacDinh;

        phieu.TienPhatTraTre = phatQuaHan;
        phieu.TienPhatHuHong = phiHuHong;
        phieu.NgayTraThucTe = homNay;
        phieu.TrangThai = TrangThaiPhieuMuon.DaTra;
        phieu.MaThuThu = maThuThu;

        phieu.Sach.SoLuongKhaDung = Math.Min(phieu.Sach.TongSoLuong, phieu.Sach.SoLuongKhaDung + 1);

        await _db.SaveChangesAsync();

        var chiTiet = new List<string>();
        if (phatQuaHan > 0) chiTiet.Add($"trả trễ {soNgayTre} ngày: {phatQuaHan:N0} VNĐ");
        if (phiHuHong > 0) chiTiet.Add($"hư hỏng sách: {phiHuHong:N0} VNĐ");

        var thongBao = chiTiet.Count > 0
            ? $"Đã nhận trả sách. Tiền phạt ({string.Join("; ", chiTiet)}). Tổng {phieu.TienPhat:N0} VNĐ."
            : "Đã nhận trả sách, không phát sinh phí phạt.";

        return KetQua.Ok(thongBao);
    }

    public async Task<int> XuLyQuaHanNhanSachAsync()
    {
        var bayGio = DateTime.Now;

        var danhSach = await _db.PhieuMuon
            .Include(p => p.Sach)
            .Include(p => p.DocGia)
            .Where(p => p.TrangThai == TrangThaiPhieuMuon.DangGiu)
            .ToListAsync();

        var soXuLy = 0;
        foreach (var phieu in danhSach)
        {
            if (phieu.NgayMuon.AddDays(QuyDinh.SoNgayGiuSach) > bayGio) continue;

            phieu.TrangThai = TrangThaiPhieuMuon.ChuaNhan;
            phieu.Sach.SoLuongKhaDung = Math.Min(phieu.Sach.TongSoLuong, phieu.Sach.SoLuongKhaDung + 1);
            phieu.DocGia.KhoaMuonDen = bayGio.AddDays(QuyDinh.SoNgayKhoaMuon);
            soXuLy++;
        }

        if (soXuLy > 0)
        {
            await _db.SaveChangesAsync();
        }

        return soXuLy;
    }

    public async Task<int> MoKhoaMuonHetHanAsync()
    {
        var bayGio = DateTime.Now;
        var danhSach = await _db.DocGia
            .Where(d => d.KhoaMuonDen != null && d.KhoaMuonDen <= bayGio)
            .ToListAsync();

        foreach (var docGia in danhSach)
        {
            docGia.KhoaMuonDen = null;
        }

        if (danhSach.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        return danhSach.Count;
    }

    public async Task<Dictionary<string, int>> ThongKeNhanhAsync()
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);

        return new Dictionary<string, int>
        {
            ["ChoDuyet"] = await _db.PhieuMuon.CountAsync(p => p.TrangThai == TrangThaiPhieuMuon.ChoDuyet),
            ["DangGiu"] = await _db.PhieuMuon.CountAsync(p => p.TrangThai == TrangThaiPhieuMuon.DangGiu),
            ["DangMuon"] = await _db.PhieuMuon.CountAsync(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon),
            ["QuaHan"] = await _db.PhieuMuon.CountAsync(p =>
                p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.NgayPhaiTra < homNay)
        };
    }
}
