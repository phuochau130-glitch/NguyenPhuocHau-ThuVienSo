namespace ThuVienSo.Web.Services;

/// <summary>Sinh mã tự tăng theo tiền tố (ví dụ: TL001 -> TL002).</summary>
public static class MaTuDong
{
    public static string TiepTheo(string tienTo, IEnumerable<string> maHienCo, int soChuSo = 3)
    {
        var soLonNhat = 0;
        foreach (var ma in maHienCo)
        {
            if (string.IsNullOrWhiteSpace(ma) || !ma.StartsWith(tienTo, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var phanSo = ma[tienTo.Length..];
            if (int.TryParse(phanSo, out var so) && so > soLonNhat)
            {
                soLonNhat = so;
            }
        }

        return tienTo + (soLonNhat + 1).ToString("D" + soChuSo);
    }
}
