namespace MyRunshaw.Application.Common;

public static class StringExtensions
{
    public static string ToStudentId(this string? input)
    // this helper ensures student IDs are consistently formatted.
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input.Trim().ToLowerInvariant(); // remove whitespace and convert to lowercase
    }
}