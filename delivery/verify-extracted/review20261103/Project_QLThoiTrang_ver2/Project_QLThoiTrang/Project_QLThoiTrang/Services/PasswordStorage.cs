using System.Buffers.Binary;

namespace Project_QLThoiTrang.Services;

internal static class PasswordStorage
{
    // Identity V2: marker + 16-byte salt + 32-byte subkey.
    // Identity V3: marker + PRF/iterations/salt length (network byte order), salt, subkey.
    public static bool IsIdentityHash(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        byte[] bytes;
        try { bytes = Convert.FromBase64String(value); }
        catch (FormatException) { return false; }
        if (bytes.Length == 49 && bytes[0] == 0) return true;
        if (bytes.Length < 45 || bytes[0] != 1) return false;
        uint prf = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(1, 4));
        uint iterations = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5, 4));
        uint saltLength = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(9, 4));
        return prf <= 2 && iterations > 0 && iterations <= int.MaxValue
            && saltLength >= 16 && saltLength <= bytes.Length - 29;
    }
}
