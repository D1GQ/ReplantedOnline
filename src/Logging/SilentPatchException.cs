namespace ReplantedOnline.Logging;

/// <summary>
/// An intentional exception used for code flow control during patching.
/// This exception is thrown on purpose to break execution at specific points.
/// </summary>
internal sealed class SilentPatchException : Exception
{
}