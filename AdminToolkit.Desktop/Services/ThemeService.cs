using System.Windows;
using System.Windows.Media;

namespace AdminToolkit.Desktop.Services;

public static class ThemeService
{
    private static readonly IReadOnlyDictionary<string, string> Light = new Dictionary<string, string>
    {
        ["WindowBackgroundBrush"] = "#F2F2F2", ["PanelBackgroundBrush"] = "#FFFFFF",
        ["RaisedBackgroundBrush"] = "#F4F4F4", ["HeaderBackgroundBrush"] = "#383838",
        ["HeaderSecondaryBrush"] = "#555555", ["PrimaryTextBrush"] = "#252525",
        ["SecondaryTextBrush"] = "#666666", ["HeaderTextBrush"] = "#FFFFFF",
        ["HeaderMutedTextBrush"] = "#DDDDDD", ["BorderBrush"] = "#C7C7C7",
        ["FooterBackgroundBrush"] = "#E3E3E3", ["AccentBrush"] = "#686868",
        ["InputBackgroundBrush"] = "#FFFFFF", ["GridLineBrush"] = "#E1E1E1"
    };

    private static readonly IReadOnlyDictionary<string, string> Dark = new Dictionary<string, string>
    {
        ["WindowBackgroundBrush"] = "#1E1E1E", ["PanelBackgroundBrush"] = "#2A2A2A",
        ["RaisedBackgroundBrush"] = "#343434", ["HeaderBackgroundBrush"] = "#121212",
        ["HeaderSecondaryBrush"] = "#3A3A3A", ["PrimaryTextBrush"] = "#F0F0F0",
        ["SecondaryTextBrush"] = "#B8B8B8", ["HeaderTextBrush"] = "#FFFFFF",
        ["HeaderMutedTextBrush"] = "#C6C6C6", ["BorderBrush"] = "#555555",
        ["FooterBackgroundBrush"] = "#252525", ["AccentBrush"] = "#777777",
        ["InputBackgroundBrush"] = "#333333", ["GridLineBrush"] = "#454545"
    };

    public static void Apply(bool darkMode)
    {
        var colors = darkMode ? Dark : Light;
        foreach (var (key, hex) in colors)
            Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }
}
