using System.Text;

namespace ReplantedOnline.Network.Discord;

/// <summary>
/// Represents a secret used for Discord lobby invites.
/// </summary>
internal sealed class DiscordLobbySecret
{
    /// <summary>
    /// Gets the hash of the mod signature used for validation.
    /// </summary>
    internal uint ModSignatureHash { get; init; }

    /// <summary>
    /// Gets the formatted version string associated with the lobby secret.
    /// </summary>
    internal string VersionFormatted { get; init; }

    /// <summary>
    /// Gets the game code to be included in the lobby secret.
    /// </summary>
    internal string GameCode { get; init; }

    /// <summary>
    /// Gets a value indicating whether a format error occurred during deserialization.
    /// </summary>
    internal bool FormatError { get; init; }

    /// <summary>
    /// Serializes the current lobby secret.
    /// </summary>
    /// <returns>A string representing the serialized lobby secret.</returns>
    internal string Serialize()
    {
        byte[] codeBytes = Encoding.UTF8.GetBytes(GameCode);

        ModInfo.ModSignature.ScrambleBytes(codeBytes);

        string encodedGameCode = Convert.ToBase64String(codeBytes);
        string payload = $"{VersionFormatted}|{encodedGameCode}";
        string encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

        return $"{ModSignatureHash}->{encodedPayload}";
    }

    /// <summary>
    /// Deserializes a string back into a DiscordLobbySecret object.
    /// </summary>
    /// <param name="data">The string to deserialize.</param>
    /// <returns>
    /// A new DiscordLobbySecret instance if deserialization succeeds; otherwise, <c>null</c>.
    /// </returns>
    internal static DiscordLobbySecret Deserialize(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return new DiscordLobbySecret
            {
                FormatError = true
            };
        }

        int hashSeparatorIndex = data.IndexOf("->", StringComparison.Ordinal);

        if (hashSeparatorIndex <= 0 || hashSeparatorIndex >= data.Length - 2)
        {
            return new DiscordLobbySecret
            {
                FormatError = true
            };
        }

        string hashString = data[..hashSeparatorIndex];
        string encodedPayload = data[(hashSeparatorIndex + 2)..];

        if (!uint.TryParse(hashString, out uint modSignatureHash))
        {
            return new DiscordLobbySecret
            {
                FormatError = true
            };
        }

        string payload;

        try
        {
            payload = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPayload));
        }
        catch
        {
            return new DiscordLobbySecret
            {
                FormatError = true
            };
        }

        int separatorIndex = payload.IndexOf('|');

        if (separatorIndex <= 0 || separatorIndex >= payload.Length - 1)
        {
            return new DiscordLobbySecret
            {
                FormatError = true
            };
        }

        string version = payload[..separatorIndex];
        string encodedGameCode = payload[(separatorIndex + 1)..];

        byte[] codeBytes;

        try
        {
            codeBytes = Convert.FromBase64String(encodedGameCode);
        }
        catch
        {
            return new DiscordLobbySecret
            {
                FormatError = true
            };
        }

        ModInfo.ModSignature.ScrambleBytes(codeBytes);

        return new DiscordLobbySecret
        {
            ModSignatureHash = modSignatureHash,
            VersionFormatted = version,
            GameCode = Encoding.UTF8.GetString(codeBytes),
            FormatError = false
        };
    }
}