using System.Text;

namespace ReplantedOnline.Network.Discord;

/// <summary>
/// Represents a secret used for Discord lobby invites.
/// </summary>
internal sealed class DiscordLobbySecret
{
    private const string HashSeparator = "->";
    private const char PayloadSeparator = '|';

    /// <summary>
    /// Gets the hash of the mod signature used for validation.
    /// </summary>
    internal uint ModSignatureHash { get; init; }

    /// <summary>
    /// Gets the formatted version string associated with the lobby secret.
    /// </summary>
    internal string VersionFormatted { get; init; } = string.Empty;

    /// <summary>
    /// Gets the game code to be included in the lobby secret.
    /// </summary>
    internal string GameCode { get; init; } = string.Empty;

    /// <summary>
    /// Serializes this lobby secret to the format <c>{hash}->{base64("{version}|{base64(scrambled code)}")}</c>.
    /// </summary>
    internal string Serialize()
    {
        byte[] codeBytes = Encoding.UTF8.GetBytes(GameCode);
        ReplantedOnlineMod.ModInfo.ModSignature.ScrambleBytes(codeBytes);

        string payload = $"{VersionFormatted}{PayloadSeparator}{Convert.ToBase64String(codeBytes)}";
        string encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

        return $"{ModSignatureHash}{HashSeparator}{encodedPayload}";
    }

    /// <summary>
    /// Attempts to deserialize a string into a <see cref="DiscordLobbySecret"/>.
    /// </summary>
    /// <param name="data">The serialized secret.</param>
    /// <param name="secret">The parsed secret if successful; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if the data was well-formed; otherwise <c>false</c>.</returns>
    internal static bool TryDeserialize(string? data, out DiscordLobbySecret secret)
    {
        secret = null!;

        if (string.IsNullOrWhiteSpace(data))
            return false;

        int hashEnd = data.IndexOf(HashSeparator, StringComparison.Ordinal);
        if (hashEnd <= 0 || hashEnd >= data.Length - HashSeparator.Length)
            return false;

        if (!uint.TryParse(data[..hashEnd], out uint modSignatureHash))
            return false;

        if (!TryDecodeBase64(data[(hashEnd + HashSeparator.Length)..], out byte[] payloadBytes))
            return false;

        string payload = Encoding.UTF8.GetString(payloadBytes);

        int versionEnd = payload.IndexOf(PayloadSeparator);
        if (versionEnd <= 0 || versionEnd >= payload.Length - 1)
            return false;

        if (!TryDecodeBase64(payload[(versionEnd + 1)..], out byte[] codeBytes))
            return false;

        ReplantedOnlineMod.ModInfo.ModSignature.ScrambleBytes(codeBytes);

        secret = new DiscordLobbySecret
        {
            ModSignatureHash = modSignatureHash,
            VersionFormatted = payload[..versionEnd],
            GameCode = Encoding.UTF8.GetString(codeBytes)
        };

        return true;
    }

    private static bool TryDecodeBase64(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}