using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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
        var assembly = Assembly.GetExecutingAssembly();
        var location = assembly.Location;
        var dllHash = ComputeDllHash(location);

        Signature = BuildAssemblySignature(assembly, dllHash);
        SignatureHash = ComputeSignatureHash(Signature);
    }

    private static byte[] ComputeDllHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        return sha256.ComputeHash(stream);
    }

    private static uint BuildAssemblySignature(Assembly assembly, byte[] dllHash)
    {
        uint signature = 0;

        foreach (byte b in dllHash)
        {
            signature ^= (uint)(b << 24);
            signature = RotateLeft(signature, 3);
            signature ^= (uint)(b << 16);
            signature = RotateLeft(signature, 3);
            signature ^= (uint)(b << 8);
            signature = RotateLeft(signature, 3);
            signature ^= b;
            signature = RotateLeft(signature, 5);
        }

        foreach (var type in assembly.GetTypes().OrderBy(type => type.FullName))
        {
            signature ^= HashString(type.FullName ?? type.Name);
            signature = RotateLeft(signature, 5);

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                   BindingFlags.Instance | BindingFlags.Static).
                                                   OrderBy(method => method.Name))
            {
                signature ^= HashString(method.Name);
                signature = RotateLeft(signature, 3);

                foreach (var param in method.GetParameters())
                {
                    signature ^= HashString(param.ParameterType.Name);
                    signature = RotateLeft(signature, 2);
                }
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                 BindingFlags.Instance | BindingFlags.Static).
                                                 OrderBy(field => field.Name))
            {
                signature ^= HashString(field.Name);
                signature = RotateLeft(signature, 7);
            }

            foreach (var prop in type.GetProperties().OrderBy(prop => prop.Name))
            {
                signature ^= HashString(prop.Name);
                signature = RotateLeft(signature, 4);
            }
        }

        signature ^= HashString(assembly.FullName);
        signature = RotateLeft(signature, 11);
        signature ^= HashString(assembly.GetName().Version?.ToString() ?? "0.0.0.0");

        signature ^= (uint)(dllHash.Length * 2654435761);
        signature = RotateLeft(signature, 13);

        foreach (byte b in dllHash)
        {
            signature ^= (uint)(b << 24);
            signature = RotateLeft(signature, 3);
            signature ^= (uint)(b << 16);
            signature = RotateLeft(signature, 3);
            signature ^= (uint)(b << 8);
            signature = RotateLeft(signature, 3);
            signature ^= b;
            signature = RotateLeft(signature, 5);
        }

        return signature;
    }

    private static uint HashString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return 0;

        uint hash = 2166136261;
        foreach (byte b in Encoding.UTF8.GetBytes(input))
        {
            hash ^= b;
            hash *= 16777619;
        }
        return hash;
    }

    private static uint RotateLeft(uint value, int bits)
    {
        return (value << bits) | (value >> (32 - bits));
    }

    private static uint ComputeSignatureHash(uint signature)
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
