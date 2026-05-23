using System.Text;

namespace ReplantedOnline.Network.Discord;

/// <summary>
/// Represents a secret used for Discord lobby invites.
/// </summary>
internal sealed class DiscordLobbySecret
{
    /// <summary>
    /// Gets or sets the formatted version string associated with the lobby secret.
    /// </summary>
    public string VersionFormatted { get; set; }

    /// <summary>
    /// Gets or sets the game code to be included in the lobby secret.
    /// </summary>
    public string GameCode { get; set; }

    /// <summary>
    /// Serializes the current lobby secret.
    /// </summary>
    /// <returns>A string representing the serialized lobby secret.</returns>
    internal string Serialize()
    {
        byte[] codeBytes = Encoding.UTF8.GetBytes(GameCode);
        ModInfo.Signature.ScrambleBytes(codeBytes);
        string encodedGameCode = Convert.ToBase64String(codeBytes);
        string payload = $"{VersionFormatted}|{encodedGameCode}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
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
            return null;

        string payload = Encoding.UTF8.GetString(Convert.FromBase64String(data));

        int separatorIndex = payload.IndexOf('|');
        if (separatorIndex <= 0 || separatorIndex >= payload.Length - 1)
            return null;

        string version = payload[..separatorIndex];
        string encodedGameCode = payload[(separatorIndex + 1)..];
        byte[] codeBytes = Convert.FromBase64String(encodedGameCode);
        ModInfo.Signature.ScrambleBytes(codeBytes);

        return new DiscordLobbySecret
        {
            VersionFormatted = version,
            GameCode = Encoding.UTF8.GetString(codeBytes)
        };
    }
}