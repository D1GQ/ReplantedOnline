using System.Security.Cryptography;

namespace ReplantedOnline.Structs;

/// <summary>
/// Represents a signature for the mod assembly.
/// </summary>
internal readonly struct ModSignature
{
    /// <summary>
    /// Primary signature.
    /// </summary>
    internal readonly uint Signature;

    /// <summary>
    /// Secondary hash of the primary signature.
    /// </summary>
    internal readonly uint SignatureHash;

    public ModSignature()
    {
        var assemblyBytes = File.ReadAllBytes(ModInfo.Assembly.Location);

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(assemblyBytes);

        Signature = BitConverter.ToUInt32(hash, 0);
        SignatureHash = GenerateSignatureHash(Signature);
    }

    private static uint GenerateSignatureHash(uint signature)
    {
        using var sha256 = SHA256.Create();
        var bytes = BitConverter.GetBytes(signature);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToUInt32(hash, 0);
    }

    /// <summary>
    /// Scrambles or unscrambles byte data in-place.
    /// </summary>
    /// <param name="data">The byte array to scramble/unscramble in-place.</param>
    internal void ScrambleBytes(Span<byte> data)
    {
        if (data.Length == 0)
            return;

        uint s1 = Signature;
        uint s2 = SignatureHash;

        for (int i = 0; i < data.Length; i++)
        {
            s1 ^= s1 << 13;
            s1 ^= s1 >> 17;
            s1 ^= s1 << 5;

            uint x = s1 + (s2 * 0x9E3779B9u);

            x ^= x >> 16;
            x ^= x >> 8;

            data[i] ^= (byte)x;
        }
    }
}
