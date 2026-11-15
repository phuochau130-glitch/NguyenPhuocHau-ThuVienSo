using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Services;

public interface ITaiKhoanService
{
    Task<NguoiDung?> DangNhapAsync(string email, string matKhau);
    Task<KetQua> DoiMatKhauAsync(string maNguoiDung, string matKhauCu, string matKhauMoi);
    Task<HoSoCaNhanViewModel?> LayHoSoAsync(string maNguoiDung);
    Task<KetQua> CapNhatHoSoAsync(string maNguoiDung, CapNhatHoSoViewModel model);
}

/// <summary>Nghiệp vụ xác thực và tài khoản cá nhân (PH-01).</summary>
public class TaiKhoanService : ITaiKhoanService
{
    private readonly ThuVienDbContext _db;
    private readonly IMatKhauHasher _bamMatKhau;

    public TaiKhoanService(ThuVienDbContext db, IMatKhauHasher bamMatKhau)
    {
        _db = db;
        _bamMatKhau = bamMatKhau;
    }

    public async Task<NguoiDung?> DangNhapAsync(string email, string matKhau)
    {
        var nguoiDung = await _db.NguoiDung
            .Include(n => n.HoSoDocGia)
            .Include(n => n.HoSoThuThu)
            .Include(n => n.HoSoQuanTri)
            .FirstOrDefaultAsync(n => n.Email == email);

        if (nguoiDung is null) return null;
        if (nguoiDung.TrangThai != TrangThaiNguoiDung.HoatDong) return null;

        // Hỗ trợ nâng cấp mật khẩu dữ liệu mẫu: nếu mật khẩu lưu chưa được băm
        // và trùng khớp, tiến hành băm lại để bảo mật.
        if (!MatKhauHasher.DaBam(nguoiDung.MatKhau))
        {
            if (!string.Equals(nguoiDung.MatKhau, matKhau, StringComparison.Ordinal))
            {
                return null;
            }

            nguoiDung.MatKhau = _bamMatKhau.Bam(matKhau);
            await _db.SaveChangesAsync();
            return nguoiDung;
        }

        return _bamMatKhau.KiemTra(matKhau, nguoiDung.MatKhau) ? nguoiDung : null;
    }

    public async Task<KetQua> DoiMatKhauAsync(string maNguoiDung, string matKhauCu, string matKhauMoi)
    {
        var nguoiDung = await _db.NguoiDung.FirstOrDefaultAsync(n => n.MaNguoiDung == maNguoiDung);
        if (nguoiDung is null) return KetQua.Loi("Không tìm thấy tài khoản.");

        var dung = MatKhauHasher.DaBam(nguoiDung.MatKhau)
            ? _bamMatKhau.KiemTra(matKhauCu, nguoiDung.MatKhau)
            : nguoiDung.MatKhau == matKhauCu;

        if (!dung) return KetQua.Loi("Mật khẩu hiện tại không chính xác.");

        nguoiDung.MatKhau = _bamMatKhau.Bam(matKhauMoi);
        await _db.SaveChangesAsync();
        return KetQua.Ok("Đã đổi mật khẩu thành công.");
    }

    public async Task<HoSoCaNhanViewModel?> LayHoSoAsync(string maNguoiDung)
    {
        var nguoiDung = await _db.NguoiDung
            .AsNoTracking()
            .Include(n => n.HoSoDocGia)
            .Include(n => n.HoSoThuThu)
            .Include(n => n.HoSoQuanTri)
            .FirstOrDefaultAsync(n => n.MaNguoiDung == maNguoiDung);

        if (nguoiDung is null) return null;

        return new HoSoCaNhanViewModel
        {
            MaNguoiDung = nguoiDung.MaNguoiDung,
            HoTen = nguoiDung.HoTen,
            Email = nguoiDung.Email,
            SoDienThoai = nguoiDung.SoDienThoai,
            VaiTro = nguoiDung.VaiTro,
            TrangThai = nguoiDung.TrangThai,
            MaSinhVienHoacCanBo = nguoiDung.HoSoDocGia?.MaSinhVienHoacCanBo,
            Khoa = nguoiDung.HoSoDocGia?.Khoa,
            MaNhanVien = nguoiDung.HoSoThuThu?.MaNhanVien,
            BoPhan = nguoiDung.HoSoThuThu?.BoPhan,
            KhoaMuonDen = nguoiDung.HoSoDocGia?.KhoaMuonDen
        };
    }

    public async Task<KetQua> CapNhatHoSoAsync(string maNguoiDung, CapNhatHoSoViewModel model)
    {
        var nguoiDung = await _db.NguoiDung
            .Include(n => n.HoSoDocGia)
            .Include(n => n.HoSoThuThu)
            .FirstOrDefaultAsync(n => n.MaNguoiDung == maNguoiDung);

        if (nguoiDung is null) return KetQua.Loi("Không tìm thấy tài khoản.");

        var email = model.Email.Trim().ToLowerInvariant();
        if (await _db.NguoiDung.AnyAsync(n => n.Email == email && n.MaNguoiDung != maNguoiDung))
        {
            return KetQua.Loi("Email đã được sử dụng cho tài khoản khác.");
        }

        nguoiDung.HoTen = model.HoTen.Trim();
        nguoiDung.Email = email;
        nguoiDung.SoDienThoai = string.IsNullOrWhiteSpace(model.SoDienThoai)
            ? null
            : model.SoDienThoai.Trim();

        if (nguoiDung.HoSoDocGia is not null)
        {
            nguoiDung.HoSoDocGia.Khoa = string.IsNullOrWhiteSpace(model.Khoa)
                ? null
                : model.Khoa.Trim();
        }

        if (nguoiDung.HoSoThuThu is not null)
        {
            nguoiDung.HoSoThuThu.BoPhan = string.IsNullOrWhiteSpace(model.BoPhan)
                ? null
                : model.BoPhan.Trim();
        }

        await _db.SaveChangesAsync();
        return KetQua.Ok("Đã cập nhật thông tin cá nhân.");
    }
}

public interface IQuanTriNguoiDungService
{
    Task<DanhSachNguoiDungViewModel> LayDanhSachAsync(string? vaiTro, string? tuKhoa, string? trangThai = null);
    Task<SuaNguoiDungViewModel?> LayDeSuaAsync(string maNguoiDung);
    Task<KetQua> ThemAsync(ThemNguoiDungViewModel model);
    Task<KetQua> SuaAsync(SuaNguoiDungViewModel model);
    Task<KetQua> DatLaiMatKhauAsync(string maNguoiDung, string matKhauMoi);
    Task<KetQua> DoiTrangThaiAsync(string maNguoiDung, string trangThai);
    Task<KetQua> DoiVaiTroAsync(string maNguoiDung, string vaiTroMoi);
    Task<KetQua> XoaAsync(string maNguoiDung);
    Task<int> DemTaiKhoanAsync();
    Task<int> DemTaiKhoanBiKhoaAsync();
}

/// <summary>Nghiệp vụ quản trị tài khoản và phân quyền (PH-07, UC-14).</summary>
public class QuanTriNguoiDungService : IQuanTriNguoiDungService
{
    private readonly ThuVienDbContext _db;
    private readonly IMatKhauHasher _bamMatKhau;

    public QuanTriNguoiDungService(ThuVienDbContext db, IMatKhauHasher bamMatKhau)
    {
        _db = db;
        _bamMatKhau = bamMatKhau;
    }

    public async Task<DanhSachNguoiDungViewModel> LayDanhSachAsync(string? vaiTro, string? tuKhoa, string? trangThai = null)
    {
        var truyVan = _db.NguoiDung
            .AsNoTracking()
            .Include(n => n.HoSoDocGia).ThenInclude(d => d!.TheThuVien)
            .Include(n => n.HoSoThuThu)
            .Include(n => n.HoSoQuanTri)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = tuKhoa.Trim();
            truyVan = truyVan.Where(n =>
                n.MaNguoiDung.Contains(tk) ||
                n.HoTen.Contains(tk) ||
                n.Email.Contains(tk));
        }

        if (!string.IsNullOrWhiteSpace(trangThai))
        {
            var tt = trangThai == "khoa" ? TrangThaiNguoiDung.BiKhoa : TrangThaiNguoiDung.HoatDong;
            truyVan = truyVan.Where(n => n.TrangThai == tt);
        }

        var danhSachNguoiDung = await truyVan.OrderBy(n => n.MaNguoiDung).ToListAsync();

        var soPhieuTheoDocGia = await _db.PhieuMuon
            .GroupBy(p => p.MaDocGia)
            .Select(g => new { MaDocGia = g.Key, SoLuong = g.Count() })
            .ToDictionaryAsync(x => x.MaDocGia, x => x.SoLuong);

        var soPhieuTheoThuThu = await _db.PhieuMuon
            .Where(p => p.MaThuThu != null)
            .GroupBy(p => p.MaThuThu!)
            .Select(g => new { MaThuThu = g.Key, SoLuong = g.Count() })
            .ToDictionaryAsync(x => x.MaThuThu, x => x.SoLuong);

        var ketQua = new DanhSachNguoiDungViewModel { VaiTro = vaiTro, TuKhoa = tuKhoa, TrangThai = trangThai };

        foreach (var n in danhSachNguoiDung)
        {
            var vaiTroHienTai = n.VaiTro;
            if (!string.IsNullOrWhiteSpace(vaiTro) && vaiTroHienTai != vaiTro) continue;

            var soPhieu = n.HoSoDocGia is not null
                ? soPhieuTheoDocGia.GetValueOrDefault(n.HoSoDocGia.MaDocGia)
                : n.HoSoThuThu is not null
                    ? soPhieuTheoThuThu.GetValueOrDefault(n.HoSoThuThu.MaThuThu)
                    : 0;

            ketQua.DanhSach.Add(new NguoiDungAdminItem
            {
                MaNguoiDung = n.MaNguoiDung,
                HoTen = n.HoTen,
                Email = n.Email,
                SoDienThoai = n.SoDienThoai,
                VaiTro = vaiTroHienTai,
                TrangThai = n.TrangThai,
                MaSinhVienHoacCanBo = n.HoSoDocGia?.MaSinhVienHoacCanBo,
                Khoa = n.HoSoDocGia?.Khoa,
                MaNhanVien = n.HoSoThuThu?.MaNhanVien,
                BoPhan = n.HoSoThuThu?.BoPhan,
                SoPhieuMuon = soPhieu,
                CoThe = n.HoSoDocGia?.TheThuVien is not null,
                CoTheXoa = soPhieu == 0
            });
        }

        ketQua.TongTaiKhoan = await _db.NguoiDung.CountAsync();
        ketQua.DangHoatDong = await _db.NguoiDung.CountAsync(n => n.TrangThai == TrangThaiNguoiDung.HoatDong);
        ketQua.BiKhoa = await _db.NguoiDung.CountAsync(n => n.TrangThai == TrangThaiNguoiDung.BiKhoa);

        return ketQua;
    }

    public async Task<SuaNguoiDungViewModel?> LayDeSuaAsync(string maNguoiDung)
    {
        var n = await _db.NguoiDung
            .AsNoTracking()
            .Include(x => x.HoSoDocGia)
            .Include(x => x.HoSoThuThu)
            .Include(x => x.HoSoQuanTri)
            .FirstOrDefaultAsync(x => x.MaNguoiDung == maNguoiDung);

        if (n is null) return null;

        return new SuaNguoiDungViewModel
        {
            MaNguoiDung = n.MaNguoiDung,
            VaiTro = n.VaiTro,
            HoTen = n.HoTen,
            Email = n.Email,
            SoDienThoai = n.SoDienThoai,
            MaSinhVienHoacCanBo = n.HoSoDocGia?.MaSinhVienHoacCanBo,
            Khoa = n.HoSoDocGia?.Khoa,
            MaNhanVien = n.HoSoThuThu?.MaNhanVien,
            BoPhan = n.HoSoThuThu?.BoPhan
        };
    }

    public async Task<KetQua> ThemAsync(ThemNguoiDungViewModel model)
    {
        var email = model.Email.Trim().ToLowerInvariant();
        if (await _db.NguoiDung.AnyAsync(n => n.Email == email))
        {
            return KetQua.Loi("Email đã tồn tại trong hệ thống.");
        }

        if (!VaiTroHopLe(model.VaiTro)) return KetQua.Loi("Vai trò không hợp lệ.");

        if (model.VaiTro is VaiTro.SinhVien or VaiTro.GiangVien && string.IsNullOrWhiteSpace(model.MaSinhVienHoacCanBo))
        {
            return KetQua.Loi("Vui lòng nhập mã sinh viên / cán bộ cho độc giả.");
        }

        if (model.VaiTro == VaiTro.ThuThu && string.IsNullOrWhiteSpace(model.MaNhanVien))
        {
            return KetQua.Loi("Vui lòng nhập mã nhân viên cho thủ thư.");
        }

        var maNguoiDung = MaTuDong.TiepTheo("ND", await _db.NguoiDung.Select(n => n.MaNguoiDung).ToListAsync());

        if (model.VaiTro is VaiTro.SinhVien or VaiTro.GiangVien)
        {
            var maSo = ChuanHoaMaSinhVien(model.MaSinhVienHoacCanBo!, model.VaiTro);
            if (await _db.DocGia.AnyAsync(d => d.MaSinhVienHoacCanBo == maSo))
            {
                return KetQua.Loi("Mã sinh viên / cán bộ đã tồn tại.");
            }
        }

        if (model.VaiTro == VaiTro.ThuThu)
        {
            var maNv = model.MaNhanVien!.Trim();
            if (await _db.ThuThu.AnyAsync(t => t.MaNhanVien == maNv))
            {
                return KetQua.Loi("Mã nhân viên đã tồn tại.");
            }
        }

        _db.NguoiDung.Add(new NguoiDung
        {
            MaNguoiDung = maNguoiDung,
            HoTen = model.HoTen.Trim(),
            Email = email,
            SoDienThoai = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai.Trim(),
            MatKhau = _bamMatKhau.Bam(model.MatKhau),
            TrangThai = TrangThaiNguoiDung.HoatDong
        });

        TaoHoSoTheoVaiTro(maNguoiDung, model.VaiTro,
            model.MaSinhVienHoacCanBo, model.Khoa, model.MaNhanVien, model.BoPhan);

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã tạo tài khoản {maNguoiDung} với vai trò {VaiTro.TenHienThi(model.VaiTro)}.");
    }

    public async Task<KetQua> SuaAsync(SuaNguoiDungViewModel model)
    {
        var n = await _db.NguoiDung
            .Include(x => x.HoSoDocGia)
            .Include(x => x.HoSoThuThu)
            .FirstOrDefaultAsync(x => x.MaNguoiDung == model.MaNguoiDung);

        if (n is null) return KetQua.Loi("Không tìm thấy tài khoản.");

        var email = model.Email.Trim().ToLowerInvariant();
        if (await _db.NguoiDung.AnyAsync(x => x.Email == email && x.MaNguoiDung != model.MaNguoiDung))
        {
            return KetQua.Loi("Email đã được dùng cho tài khoản khác.");
        }

        n.HoTen = model.HoTen.Trim();
        n.Email = email;
        n.SoDienThoai = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai.Trim();

        if (n.HoSoDocGia is not null && !string.IsNullOrWhiteSpace(model.MaSinhVienHoacCanBo))
        {
            var maSo = ChuanHoaMaSinhVien(model.MaSinhVienHoacCanBo, model.VaiTro);
            if (await _db.DocGia.AnyAsync(d => d.MaSinhVienHoacCanBo == maSo && d.MaDocGia != model.MaNguoiDung))
            {
                return KetQua.Loi("Mã sinh viên / cán bộ đã được sử dụng.");
            }
            n.HoSoDocGia.MaSinhVienHoacCanBo = maSo;
            n.HoSoDocGia.Khoa = string.IsNullOrWhiteSpace(model.Khoa) ? null : model.Khoa.Trim();
        }

        if (n.HoSoThuThu is not null)
        {
            if (!string.IsNullOrWhiteSpace(model.MaNhanVien))
            {
                var maNv = model.MaNhanVien.Trim();
                if (await _db.ThuThu.AnyAsync(t => t.MaNhanVien == maNv && t.MaThuThu != model.MaNguoiDung))
                {
                    return KetQua.Loi("Mã nhân viên đã được sử dụng.");
                }
                n.HoSoThuThu.MaNhanVien = maNv;
            }
            n.HoSoThuThu.BoPhan = string.IsNullOrWhiteSpace(model.BoPhan) ? null : model.BoPhan.Trim();
        }

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã cập nhật tài khoản {model.MaNguoiDung}.");
    }

    public async Task<KetQua> DatLaiMatKhauAsync(string maNguoiDung, string matKhauMoi)
    {
        var n = await _db.NguoiDung.FirstOrDefaultAsync(x => x.MaNguoiDung == maNguoiDung);
        if (n is null) return KetQua.Loi("Không tìm thấy tài khoản.");

        n.MatKhau = _bamMatKhau.Bam(matKhauMoi);
        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã đặt lại mật khẩu cho tài khoản {maNguoiDung}.");
    }

    public async Task<KetQua> DoiTrangThaiAsync(string maNguoiDung, string trangThai)
    {
        if (trangThai != TrangThaiNguoiDung.HoatDong && trangThai != TrangThaiNguoiDung.BiKhoa)
        {
            return KetQua.Loi("Trạng thái tài khoản không hợp lệ.");
        }

        var n = await _db.NguoiDung.FirstOrDefaultAsync(x => x.MaNguoiDung == maNguoiDung);
        if (n is null) return KetQua.Loi("Không tìm thấy tài khoản.");

        if (trangThai == TrangThaiNguoiDung.BiKhoa && n.VaiTro == VaiTro.Admin)
        {
            return KetQua.Loi("Không thể khóa tài khoản quản trị viên.");
        }

        n.TrangThai = trangThai;
        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã chuyển tài khoản {maNguoiDung} sang trạng thái \"{trangThai}\".");
    }

    public async Task<KetQua> DoiVaiTroAsync(string maNguoiDung, string vaiTroMoi)
    {
        if (!VaiTroHopLe(vaiTroMoi)) return KetQua.Loi("Vai trò mới không hợp lệ.");

        var n = await _db.NguoiDung
            .Include(x => x.HoSoDocGia)
            .Include(x => x.HoSoThuThu)
            .Include(x => x.HoSoQuanTri)
            .FirstOrDefaultAsync(x => x.MaNguoiDung == maNguoiDung);

        if (n is null) return KetQua.Loi("Không tìm thấy tài khoản.");

        var vaiTroHienTai = n.VaiTro;

        // Sinh viên <-> Giảng viên: chỉ đổi tiền tố mã số.
        if (vaiTroHienTai is VaiTro.SinhVien or VaiTro.GiangVien &&
            vaiTroMoi is VaiTro.SinhVien or VaiTro.GiangVien)
        {
            if (vaiTroHienTai == vaiTroMoi) return KetQua.Ok("Vai trò không thay đổi.");

            var docGia = n.HoSoDocGia!;
            docGia.MaSinhVienHoacCanBo = ChuanHoaMaSinhVien(docGia.MaSinhVienHoacCanBo, vaiTroMoi);
            await _db.SaveChangesAsync();
            return KetQua.Ok($"Đã đổi vai trò tài khoản thành {VaiTro.TenHienThi(vaiTroMoi)}.");
        }

        if (vaiTroHienTai == vaiTroMoi) return KetQua.Ok("Vai trò không thay đổi.");

        // Chuyển sang Admin: kiểm tra ràng buộc dữ liệu nghiệp vụ.
        if (vaiTroHienTai is VaiTro.SinhVien or VaiTro.GiangVien)
        {
            var coDuLieu = await _db.PhieuMuon.AnyAsync(p => p.MaDocGia == maNguoiDung)
                || await _db.YeuCauTaiLieu.AnyAsync(y => y.MaDocGia == maNguoiDung)
                || await _db.TaiLieu.AnyAsync(t => t.MaNguoiDang == maNguoiDung);
            if (coDuLieu)
            {
                return KetQua.Loi("Không thể đổi vai trò: tài khoản đã phát sinh dữ liệu mượn/trả hoặc yêu cầu tài liệu.");
            }

            var the = await _db.TheThuVien.FirstOrDefaultAsync(t => t.MaDocGia == maNguoiDung);
            if (the is not null) _db.TheThuVien.Remove(the);
            _db.DocGia.Remove(n.HoSoDocGia!);
        }
        else if (vaiTroHienTai == VaiTro.ThuThu)
        {
            var coDuLieu = await _db.PhieuMuon.AnyAsync(p => p.MaThuThu == maNguoiDung)
                || await _db.YeuCauTaiLieu.AnyAsync(y => y.MaThuThu == maNguoiDung);
            if (coDuLieu)
            {
                return KetQua.Loi("Không thể đổi vai trò: thủ thư đã xử lý phiếu mượn hoặc yêu cầu tài liệu.");
            }
            _db.ThuThu.Remove(n.HoSoThuThu!);
        }
        else if (vaiTroHienTai == VaiTro.Admin)
        {
            _db.QuanTriVien.Remove(n.HoSoQuanTri!);
        }

        await _db.SaveChangesAsync();

        // Tạo hồ sơ mới tương ứng vai trò mới.
        var maNhanVien = MaTuDong.TiepTheo("NV", await _db.ThuThu.Select(t => t.MaNhanVien).ToListAsync());
        var phanSo = new string(maNguoiDung.Where(char.IsDigit).ToArray());
        var maSinhVien = ChuanHoaMaSinhVien(string.IsNullOrEmpty(phanSo) ? maNguoiDung : phanSo, vaiTroMoi);

        TaoHoSoTheoVaiTro(maNguoiDung, vaiTroMoi, maSinhVien, null, maNhanVien, null);

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã phân quyền tài khoản {maNguoiDung} thành {VaiTro.TenHienThi(vaiTroMoi)}.");
    }

    public async Task<KetQua> XoaAsync(string maNguoiDung)
    {
        var n = await _db.NguoiDung
            .Include(x => x.HoSoDocGia)
            .Include(x => x.HoSoThuThu)
            .FirstOrDefaultAsync(x => x.MaNguoiDung == maNguoiDung);

        if (n is null) return KetQua.Loi("Không tìm thấy tài khoản.");

        if (n.VaiTro == VaiTro.Admin)
        {
            return KetQua.Loi("Không thể xóa tài khoản quản trị viên.");
        }

        var coGiaoDich = await _db.PhieuMuon.AnyAsync(p => p.MaDocGia == maNguoiDung || p.MaThuThu == maNguoiDung)
            || await _db.YeuCauTaiLieu.AnyAsync(y => y.MaDocGia == maNguoiDung || y.MaThuThu == maNguoiDung)
            || await _db.TaiLieu.AnyAsync(t => t.MaNguoiDang == maNguoiDung);

        if (coGiaoDich)
        {
            return KetQua.Loi("Không thể xóa tài khoản đang có dữ liệu giao dịch mượn/trả. Hãy chuyển sang \"Khóa tài khoản\".");
        }

        var the = await _db.TheThuVien.FirstOrDefaultAsync(t => t.MaDocGia == maNguoiDung);
        if (the is not null) _db.TheThuVien.Remove(the);

        await _db.SaveChangesAsync();

        _db.NguoiDung.Remove(n);
        await _db.SaveChangesAsync();

        return KetQua.Ok($"Đã xóa tài khoản {maNguoiDung}.");
    }

    public async Task<int> DemTaiKhoanAsync() => await _db.NguoiDung.CountAsync();

    public async Task<int> DemTaiKhoanBiKhoaAsync() =>
        await _db.NguoiDung.CountAsync(n => n.TrangThai == TrangThaiNguoiDung.BiKhoa);

    private static bool VaiTroHopLe(string vaiTro) =>
        vaiTro is VaiTro.SinhVien or VaiTro.GiangVien or VaiTro.ThuThu or VaiTro.Admin;

    private static string ChuanHoaMaSinhVien(string ma, string vaiTro)
    {
        var maChuan = ma.Trim();
        if (vaiTro == VaiTro.GiangVien)
        {
            return maChuan.StartsWith("GV", StringComparison.OrdinalIgnoreCase) ? maChuan : "GV" + maChuan;
        }

        if (maChuan.StartsWith("GV", StringComparison.OrdinalIgnoreCase))
        {
            return "SV" + maChuan[2..];
        }

        return maChuan;
    }

    private void TaoHoSoTheoVaiTro(string maNguoiDung, string vaiTro,
        string? maSinhVienHoacCanBo, string? khoa, string? maNhanVien, string? boPhan)
    {
        switch (vaiTro)
        {
            case VaiTro.SinhVien:
            case VaiTro.GiangVien:
                _db.DocGia.Add(new DocGia
                {
                    MaDocGia = maNguoiDung,
                    MaSinhVienHoacCanBo = ChuanHoaMaSinhVien(
                        string.IsNullOrWhiteSpace(maSinhVienHoacCanBo) ? maNguoiDung : maSinhVienHoacCanBo, vaiTro),
                    Khoa = string.IsNullOrWhiteSpace(khoa) ? null : khoa.Trim()
                });
                break;
            case VaiTro.ThuThu:
                _db.ThuThu.Add(new ThuThu
                {
                    MaThuThu = maNguoiDung,
                    MaNhanVien = string.IsNullOrWhiteSpace(maNhanVien) ? maNguoiDung : maNhanVien.Trim(),
                    BoPhan = string.IsNullOrWhiteSpace(boPhan) ? null : boPhan.Trim()
                });
                break;
            case VaiTro.Admin:
                _db.QuanTriVien.Add(new QuanTriVien { MaAdmin = maNguoiDung });
                break;
        }
    }
}
