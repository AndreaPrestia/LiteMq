namespace LiteMq.Extensions;

public static class StringExtensions
{
    public static string NormalizeString(this string str) => !string.IsNullOrWhiteSpace(str) ? str.Trim().ToLowerInvariant() : string.Empty;
}