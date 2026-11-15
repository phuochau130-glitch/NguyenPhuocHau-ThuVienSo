using System.Security.Cryptography;

namespace ThuVienSo.Web.Services;

public interface IMatKhauHasher
{
    string Bam(string matKhau);
    bool KiemTra(string matKhau, string maBam);
}

/// <summary>Băm và kiểm tra mật khẩu bằng PBKDF2-SHA256 (100.000 vòng lặp).</summary>
public class MatKhauHasher : IMatKhauHasher
{
    private const int SoVongLap = 100_000;
    private const int KichThuocSalt = 16;
    private const int KichThuocKhoa = 32;
    private const string TienTo = "pbkdf2sha256";

    public string Bam(string matKhau)
    {
        var salt = RandomNumberGenerator.GetBytes(KichThuocSalt);
        var khoa = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, SoVongLap, HashAlgorithmName.SHA256, KichThuocKhoa);
        return $"{TienTo}.{SoVongLap}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(khoa)}";
    }

    public bool KiemTra(string matKhau, string maBam)
    {
        if (string.IsNullOrWhiteSpace(maBam)) return false;

        var phan = maBam.Split('.');
        if (phan.Length != 4 || phan[0] != TienTo) return false;
        if (!int.TryParse(phan[1], out var vongLap)) return false;

        try
        {
            var salt = Convert.FromBase64String(phan[2]);
            var khoaGoc = Convert.FromBase64String(phan[3]);
            var khoaThu = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, vongLap, HashAlgorithmName.SHA256, khoaGoc.Length);
            return CryptographicOperations.FixedTimeEquals(khoaGoc, khoaThu);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Cho biết chuỗi có phải mật khẩu đã băm theo định dạng của hệ thống hay không.</summary>
    public static bool DaBam(string maBam) => maBam.StartsWith(TienTo + ".", StringComparison.Ordinal);
}
