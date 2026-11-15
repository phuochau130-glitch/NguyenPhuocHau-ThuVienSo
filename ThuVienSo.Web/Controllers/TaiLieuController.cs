using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Controllers;

/// <summary>Tra cứu, xem chi tiết, xem trước và đọc/tải tài liệu (PH-02).</summary>
public class TaiLieuController : Controller
{
    private readonly ITraCuuService _traCuu;
    private readonly IYeuCauService _yeuCau;
    private readonly ITheThuVienService _theThuVien;
    private readonly INhatKyService _nhatKy;
    private readonly IWebHostEnvironment _moiTruong;

    public TaiLieuController(
        ITraCuuService traCuu,
        IYeuCauService yeuCau,
        ITheThuVienService theThuVien,
        INhatKyService nhatKy,
        IWebHostEnvironment moiTruong)
    {
        _traCuu = traCuu;
        _yeuCau = yeuCau;
        _theThuVien = theThuVien;
        _nhatKy = nhatKy;
        _moiTruong = moiTruong;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? tuKhoa, string? danhMuc, string? loai, bool chiConKhaDung = false)
    {
        var model = new TraCuuTaiLieuViewModel
        {
            TuKhoa = tuKhoa,
            DanhMuc = danhMuc,
            Loai = loai,
            ChiConKhaDung = chiConKhaDung,
            KetQua = await _traCuu.TimKiemAsync(tuKhoa, danhMuc, loai, chiConKhaDung),
            DanhSachDanhMuc = await _traCuu.LayDanhMucAsync(),
            DaTraCuu = !string.IsNullOrWhiteSpace(tuKhoa)
                || !string.IsNullOrWhiteSpace(danhMuc)
                || !string.IsNullOrWhiteSpace(loai)
                || chiConKhaDung
        };

        return View(model);
    }

    /// <summary>Trang danh sách tài liệu số (e-book, giáo trình, NCKH) có ô tìm kiếm.</summary>
    [AllowAnonymous]
    public async Task<IActionResult> TaiLieuSo(string? tuKhoa, string? danhMuc, bool chiGioiHan = false)
    {
        var ketQua = await _traCuu.TimKiemAsync(tuKhoa, danhMuc, "so", false);
        if (chiGioiHan) ketQua = ketQua.Where(t => t.LaTaiLieuHanChe).ToList();

        var model = new TraCuuTaiLieuViewModel
        {
            TuKhoa = tuKhoa,
            DanhMuc = danhMuc,
            Loai = "so",
            KetQua = ketQua,
            DanhSachDanhMuc = await _traCuu.LayDanhMucAsync(),
            ChiConKhaDung = chiGioiHan,
            DaTraCuu = !string.IsNullOrWhiteSpace(tuKhoa) || !string.IsNullOrWhiteSpace(danhMuc) || chiGioiHan
        };

        return View(model);
    }

    /// <summary>Trang danh sách sách vật lý (bản cứng) có ô tìm kiếm.</summary>
    [AllowAnonymous]
    public async Task<IActionResult> SachVatLi(string? tuKhoa, string? danhMuc, bool chiConKhaDung = false)
    {
        var ketQua = await _traCuu.TimKiemAsync(tuKhoa, danhMuc, "sach", chiConKhaDung);

        var model = new TraCuuTaiLieuViewModel
        {
            TuKhoa = tuKhoa,
            DanhMuc = danhMuc,
            Loai = "sach",
            KetQua = ketQua,
            DanhSachDanhMuc = await _traCuu.LayDanhMucAsync(),
            ChiConKhaDung = chiConKhaDung,
            DaTraCuu = !string.IsNullOrWhiteSpace(tuKhoa) || !string.IsNullOrWhiteSpace(danhMuc) || chiConKhaDung
        };

        return View(model);
    }

    [AllowAnonymous]
    public async Task<IActionResult> ChiTiet(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var taiLieu = await _traCuu.LayChiTietAsync(id);
        if (taiLieu is null || taiLieu.TrangThaiDuyet != TrangThaiDuyetTaiLieu.DaDuyet) return NotFound();
        var laDocGia = User.IsInRole(VaiTro.SinhVien) || User.IsInRole(VaiTro.GiangVien);

        var model = new ChiTietTaiLieuViewModel
        {
            TaiLieu = taiLieu,
            DaDangNhap = User.Identity?.IsAuthenticated == true,
            DaCoQuyenDoc = !taiLieu.LaTaiLieuHanChe || User.IsInRole(VaiTro.ThuThu)
        };

        var maNguoiDung = User.MaNguoiDung();
        if (!string.IsNullOrWhiteSpace(maNguoiDung) && laDocGia)
        {
            var the = await _theThuVien.LayTheCuaDocGiaAsync(maNguoiDung!);
            model.CoTheThuVienHopLe = the?.ConHieuLuc ?? false;
            model.TrangThaiThe = the?.TrangThai;
            model.DaHetHanThe = the?.DaHetHan ?? false;
            model.NgayHetHanThe = the?.NgayHetHan;
            model.GhiChuThe = the?.GhiChu;

            var khoaMuon = the?.DocGia.KhoaMuonDen;
            model.KhoaMuonDen = khoaMuon;
            model.DangBiKhoaMuon = khoaMuon.HasValue && khoaMuon.Value > DateTime.Now;

            if (taiLieu.BanSo is not null)
            {
                model.YeuCauGanNhat = await _yeuCau.LayYeuCauGanNhatAsync(maNguoiDung, taiLieu.BanSo.MaTaiLieuSo);

                if (taiLieu.LaTaiLieuHanChe)
                {
                    model.DaCoQuyenDoc = await _yeuCau.DaCoQuyenDocAsync(maNguoiDung, taiLieu.BanSo.MaTaiLieuSo);
                }
            }
        }

        return View(model);
    }

    /// <summary>Xem trước sách vật lý, giới hạn số ký tự theo Quy tắc 3.</summary>
    [AllowAnonymous]
    public async Task<IActionResult> XemTruoc(string id)
    {
        var taiLieu = await _traCuu.LayChiTietAsync(id);
        if (taiLieu is null || taiLieu.BanSach is null || taiLieu.TrangThaiDuyet != TrangThaiDuyetTaiLieu.DaDuyet)
        {
            return NotFound();
        }

        var noiDung = XayDungNoiDungXemTruoc(taiLieu);
        var biCatNgan = noiDung.Length > QuyDinh.SoKyTuXemTruoc;
        if (biCatNgan)
        {
            noiDung = noiDung[..QuyDinh.SoKyTuXemTruoc];
        }

        var sach = taiLieu.BanSach;
        var coTep = !string.IsNullOrWhiteSpace(sach.DuongDanTepXemTruoc);

        await _traCuu.TangLuotXemAsync(taiLieu.MaTaiLieu);
        await _nhatKy.GhiAsync(User.MaNguoiDung(), HanhDongNhatKy.XemTaiLieu,
            taiLieu.MaTaiLieu, taiLieu.TenTaiLieu, DiaChiIp());

        return View(new XemTruocSachViewModel
        {
            TaiLieu = taiLieu,
            Sach = sach,
            NoiDungXemTruoc = noiDung,
            BiCatNgan = biCatNgan,
            UrlTepXemTruoc = coTep ? Url.Action(nameof(XemTruocTep), new { id }) : null,
            DinhDangTepXemTruoc = sach.DinhDangTepXemTruoc
        });
    }

    /// <summary>
    /// Trả về tệp PDF/Word đọc thử của sách vật lý để nhúng trực tiếp vào trình duyệt.
    /// Nội dung bị khóa 1 nửa phía máy chủ; phần còn lại yêu cầu đăng ký mượn bản cứng.
    /// </summary>
    [AllowAnonymous]
    public async Task<IActionResult> XemTruocTep(string id)
    {
        var taiLieu = await _traCuu.LayChiTietAsync(id);
        if (taiLieu?.BanSach is null
            || string.IsNullOrWhiteSpace(taiLieu.BanSach.DuongDanTepXemTruoc)
            || taiLieu.TrangThaiDuyet != TrangThaiDuyetTaiLieu.DaDuyet)
        {
            return NotFound();
        }

        var duongDanVatLy = Path.Combine(
            _moiTruong.WebRootPath,
            taiLieu.BanSach.DuongDanTepXemTruoc.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(duongDanVatLy))
        {
            return NotFound();
        }

        var kieuNoiDung = KieuNoiDung(taiLieu.BanSach.DinhDangTepXemTruoc, duongDanVatLy);
        Stream luongKhoa;
        try
        {
            luongKhoa = await TaoLuongKhoaMotNuaAsync(duongDanVatLy, kieuNoiDung);
        }
        catch (NotSupportedException loi)
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, loi.Message);
        }
        catch (PdfReaderException)
        {
            return UnprocessableEntity("Tệp PDF không hợp lệ hoặc không thể tạo bản xem trước.");
        }

        return File(luongKhoa, kieuNoiDung, enableRangeProcessing: false);
    }

    /// <summary>
    /// Tạo luồng tệp đọc thử chỉ chứa khoảng một nửa nội dung gốc (khóa nửa nội dung).
    /// PDF: giữ nửa đầu tệp. Word (.docx): cắt bớt một nửa số đoạn văn nhưng vẫn là tệp hợp lệ.
    /// </summary>
    private static async Task<Stream> TaoLuongKhoaMotNuaAsync(string duongDanVatLy, string kieuNoiDung)
    {
        if (kieuNoiDung.StartsWith("application/vnd.openxmlformats-officedocument", StringComparison.OrdinalIgnoreCase))
        {
            return await CatNuaDocxAsync(duongDanVatLy);
        }

        if (string.Equals(kieuNoiDung, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return TaoPdfXemTruocMotNua(duongDanVatLy);
        }

        throw new NotSupportedException("Chỉ hỗ trợ xem trước tệp PDF và DOCX.");
    }

    /// <summary>Tạo PDF hợp lệ chỉ gồm nửa đầu số trang, không cắt giữa cấu trúc nhị phân.</summary>
    private static Stream TaoPdfXemTruocMotNua(string duongDanVatLy)
    {
        var ketQua = new MemoryStream();
        using var pdfNguon = PdfReader.Open(duongDanVatLy, PdfDocumentOpenMode.Import);
        using var pdfXemTruoc = new PdfDocument();
        var soTrangGiu = Math.Max(1, (int)Math.Ceiling(pdfNguon.PageCount * QuyDinh.TyLeKhoaXemTruoc));

        for (var i = 0; i < soTrangGiu; i++)
        {
            pdfXemTruoc.AddPage(pdfNguon.Pages[i]);
        }

        pdfXemTruoc.Save(ketQua, closeStream: false);
        ketQua.Position = 0;
        return ketQua;
    }

    /// <summary>Giữ lại nửa đầu số đoạn văn trong tệp .docx và đóng gói lại thành tệp Word hợp lệ.</summary>
    private static async Task<Stream> CatNuaDocxAsync(string duongDanVatLy)
    {
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var ketQua = new MemoryStream();

        using (var docxGoc = ZipFile.OpenRead(duongDanVatLy))
        using (var docxMoi = new ZipArchive(ketQua, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var muc in docxGoc.Entries)
            {
                var mucMoi = docxMoi.CreateEntry(muc.FullName, CompressionLevel.Fastest);
                using var luongGhi = mucMoi.Open();

                if (string.Equals(muc.FullName, "word/document.xml", StringComparison.OrdinalIgnoreCase))
                {
                    using var luongDoc = muc.Open();
                    var taiLieuXml = await XDocument.LoadAsync(luongDoc, LoadOptions.PreserveWhitespace, CancellationToken.None);
                    CatNuaDoanVan(taiLieuXml, w);
                    await taiLieuXml.SaveAsync(luongGhi, SaveOptions.DisableFormatting, CancellationToken.None);
                }
                else
                {
                    using var luongDoc = muc.Open();
                    await luongDoc.CopyToAsync(luongGhi);
                }
            }
        }

        ketQua.Position = 0;
        return ketQua;
    }

    /// <summary>Loại bỏ nửa sau các đoạn văn bản trong phần thân tài liệu Word.</summary>
    private static void CatNuaDoanVan(XDocument taiLieuXml, XNamespace w)
    {
        var than = taiLieuXml.Root?.Element(w + "body");
        if (than is null) return;

        var doanVan = than.Elements(w + "p").ToList();
        if (doanVan.Count <= 1) return;

        var soDoanGiu = (int)Math.Ceiling(doanVan.Count * QuyDinh.TyLeKhoaXemTruoc);
        for (var i = soDoanGiu; i < doanVan.Count; i++)
        {
            doanVan[i].Remove();
        }
    }

    /// <summary>Trang đọc tài liệu số trực tuyến trên trình duyệt (giao diện đọc giống e-book reader).</summary>
    [Authorize]
    public async Task<IActionResult> Doc(string id)
    {
        var taiLieu = await _traCuu.LayChiTietAsync(id);
        if (taiLieu?.BanSo is null || taiLieu.TrangThaiDuyet != TrangThaiDuyetTaiLieu.DaDuyet)
        {
            return NotFound();
        }

        var banSo = taiLieu.BanSo;
        var laThuThu = User.IsInRole(VaiTro.ThuThu);
        var daCoQuyenDoc = !taiLieu.LaTaiLieuHanChe || laThuThu;

        if (taiLieu.LaTaiLieuHanChe && !laThuThu)
        {
            var maNguoiDung = User.MaNguoiDung();
            daCoQuyenDoc = !string.IsNullOrWhiteSpace(maNguoiDung)
                && await _yeuCau.DaCoQuyenDocAsync(maNguoiDung, banSo.MaTaiLieuSo);

            if (!daCoQuyenDoc)
            {
                TempData["Loi"] = "Tài liệu này bị giới hạn truy cập. Bạn cần gửi yêu cầu và được thủ thư phê duyệt để đọc toàn văn.";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }
        }

        var duongDanVatLy = Path.Combine(
            _moiTruong.WebRootPath,
            banSo.DuongDanFile.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(duongDanVatLy))
        {
            TempData["Loi"] = "Tệp tài liệu chưa có sẵn trên hệ thống. Vui lòng liên hệ thủ thư.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        var model = new DocTaiLieuViewModel
        {
            TaiLieu = taiLieu,
            BanSo = banSo,
            UrlTaiNguyen = Url.Action(nameof(TaiNguyen), new { id }) ?? string.Empty,
            DaCoQuyenDoc = daCoQuyenDoc
        };

        await _traCuu.TangLuotXemAsync(taiLieu.MaTaiLieu);
        await _nhatKy.GhiAsync(User.MaNguoiDung(), HanhDongNhatKy.XemTaiLieu,
            taiLieu.MaTaiLieu, taiLieu.TenTaiLieu, DiaChiIp());

        return View(model);
    }

    /// <summary>Trả về luồng tệp tài liệu số để nhúng vào trang đọc (yêu cầu đăng nhập).</summary>
    [Authorize]
    public Task<IActionResult> TaiNguyen(string id) => XuLyTepSoAsync(id, taiVe: false);

    /// <summary>Tải tài liệu số về máy (yêu cầu đăng nhập).</summary>
    [Authorize]
    public Task<IActionResult> Tai(string id) => XuLyTepSoAsync(id, taiVe: true);

    private async Task<IActionResult> XuLyTepSoAsync(string id, bool taiVe)
    {
        var taiLieu = await _traCuu.LayChiTietAsync(id);
        if (taiLieu?.BanSo is null || taiLieu.TrangThaiDuyet != TrangThaiDuyetTaiLieu.DaDuyet)
        {
            return NotFound();
        }

        var banSo = taiLieu.BanSo;

        if (taiLieu.LaTaiLieuHanChe && !User.IsInRole(VaiTro.ThuThu))
        {
            var maNguoiDung = User.MaNguoiDung();
            var coQuyen = !string.IsNullOrWhiteSpace(maNguoiDung)
                && await _yeuCau.DaCoQuyenDocAsync(maNguoiDung, banSo.MaTaiLieuSo);

            if (!coQuyen)
            {
                TempData["Loi"] = "Tài liệu này bị giới hạn truy cập. Bạn cần gửi yêu cầu và được thủ thư phê duyệt để đọc toàn văn.";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }
        }

        var duongDanVatLy = Path.Combine(
            _moiTruong.WebRootPath,
            banSo.DuongDanFile.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(duongDanVatLy))
        {
            TempData["Loi"] = "Tệp tài liệu chưa có sẵn trên hệ thống. Vui lòng liên hệ thủ thư.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        await _traCuu.TangLuotTaiAsync(banSo.MaTaiLieuSo);

        if (taiVe)
        {
            await _nhatKy.GhiAsync(User.MaNguoiDung(), HanhDongNhatKy.TaiTaiLieu,
                taiLieu.MaTaiLieu, taiLieu.TenTaiLieu, DiaChiIp());
        }

        var kieuNoiDung = KieuNoiDung(banSo.DinhDang, duongDanVatLy);

        var tenTep = $"{taiLieu.MaTaiLieu}{Path.GetExtension(duongDanVatLy)}";
        var luong = System.IO.File.OpenRead(duongDanVatLy);

        return taiVe
            ? File(luong, kieuNoiDung, tenTep, enableRangeProcessing: true)
            : File(luong, kieuNoiDung, enableRangeProcessing: true);
    }

    /// <summary>Địa chỉ IP của máy khách (dùng cho nhật ký hoạt động).</summary>
    private string? DiaChiIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>Xác định kiểu nội dung MIME theo định dạng tệp để trình duyệt hiển thị trực tiếp thay vì tải về.</summary>
    private static string KieuNoiDung(string? dinhDang, string duongDanVatLy)
    {
        var dd = string.IsNullOrWhiteSpace(dinhDang)
            ? Path.GetExtension(duongDanVatLy).TrimStart('.').ToUpperInvariant()
            : dinhDang.ToUpperInvariant();

        return dd switch
        {
            "PDF" => "application/pdf",
            "TXT" => "text/plain; charset=utf-8",
            "DOC" => "application/msword",
            "DOCX" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }

    /// <summary>Ghép nội dung xem trước từ thông tin mô tả của sách vật lý.</summary>
    private static string XayDungNoiDungXemTruoc(TaiLieu taiLieu)
    {
        var sach = taiLieu.BanSach!;
        var bo = new StringBuilder();

        bo.AppendLine($"TÊN TÀI LIỆU: {taiLieu.TenTaiLieu}");
        if (!string.IsNullOrWhiteSpace(taiLieu.TacGia)) bo.AppendLine($"Tác giả: {taiLieu.TacGia}");
        if (!string.IsNullOrWhiteSpace(taiLieu.NhaXuatBan)) bo.AppendLine($"Nhà xuất bản: {taiLieu.NhaXuatBan}");
        if (taiLieu.NamXuatBan.HasValue) bo.AppendLine($"Năm xuất bản: {taiLieu.NamXuatBan}");
        if (!string.IsNullOrWhiteSpace(taiLieu.DanhMuc)) bo.AppendLine($"Danh mục: {taiLieu.DanhMuc}");
        if (!string.IsNullOrWhiteSpace(sach.Isbn)) bo.AppendLine($"ISBN: {sach.Isbn}");
        if (!string.IsNullOrWhiteSpace(sach.ViTriKeSach)) bo.AppendLine($"Vị trí kệ sách: {sach.ViTriKeSach}");
        bo.AppendLine();
        bo.AppendLine("TÓM TẮT NỘI DUNG:");
        bo.AppendLine(string.IsNullOrWhiteSpace(taiLieu.GhiChu)
            ? "Tài liệu chưa có phần tóm tắt nội dung. Bạn có thể đăng ký mượn bản cứng để đọc toàn văn tại thư viện."
            : taiLieu.GhiChu);

        return bo.ToString();
    }
}
