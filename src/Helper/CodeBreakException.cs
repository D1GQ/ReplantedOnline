namespace ReplantedOnline.Helper;

/// <summary>
/// An intentional exception used for code flow control during patching.
/// This exception is thrown on purpose to break execution at specific points.
/// It is completely safe to ignore and should never be reported as an error.
/// </summary>
internal sealed class CodeBreakException : Exception
{
    internal CodeBreakException() : base("INTENTIONAL BREAK - USED FOR CODE FLOW CONTROL - DO NOT REPORT THIS") { }
}