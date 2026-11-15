namespace ThuVienSo.Web.Helpers;

/// <summary>
/// Tiện ích hiển thị dùng chung cho giao diện.
/// Ánh xạ trạng thái sang màu tương phản: xanh (hợp lệ) / đỏ (cảnh báo) / vàng (chờ xử lý).
/// </summary>
public static class HienThi
{
    /// <summary>Lớp CSS Bootstrap hiển thị badge theo trạng thái nghiệp vụ.</summary>
    public static string LopTrangThai(string? trangThai) => trangThai switch
    {
        "Hoạt động" => "bg-success",
        "Đã duyệt" => "bg-success",
        "Đã phê duyệt" => "bg-success",
        "Đang mượn" => "bg-success",
        "Đã trả" => "bg-success",
        "Chờ duyệt" => "bg-warning text-dark",
        "Đang giữ" => "bg-warning text-dark",
        "Chưa nhận sách" => "bg-danger",
        "Từ chối" => "bg-danger",
        "Bị khóa" => "bg-danger",
        "Quá hạn" => "bg-danger",
        "Hết hạn" => "bg-secondary",
        "Đã hủy" => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string Ngay(DateOnly? ngay) => ngay?.ToString("dd/MM/yyyy") ?? "—";

    public static string NgayGio(DateTime? thoiDiem) => thoiDiem?.ToString("dd/MM/yyyy HH:mm") ?? "—";

    public static string Tien(decimal soTien) => string.Format("{0:N0} VNĐ", soTien);
}
