using System.Text.RegularExpressions;

namespace AdminToolkit.Desktop.Services;

public static partial class ComputerNameParser
{
    public static IReadOnlyList<string> Parse(string input) =>
        Separators().Split(input ?? string.Empty).Select(name => name.Trim()).Where(name => name.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    [GeneratedRegex("[,;\\s]+")]
    private static partial Regex Separators();
}
