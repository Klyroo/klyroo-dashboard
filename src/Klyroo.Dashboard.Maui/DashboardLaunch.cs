namespace Klyroo.Dashboard.Maui;

/// <summary>Opens an address, and copies a text: the link and the copy button of a row.</summary>
internal static class DashboardLaunch
{
    /// <summary>Reads an address a device can open. A path such as <c>/docs</c> is not one: it reads as a <c>file:</c> address on Linux and macOS.</summary>
    public static bool TryParse(string? href, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Uri? uri) =>
        Uri.TryCreate(href, UriKind.Absolute, out uri) && uri.Scheme != Uri.UriSchemeFile;

    /// <summary>Opens <paramref name="href"/>: the browser for http and https, the app registered for the scheme otherwise. An address that cannot be opened does nothing.</summary>
    /// <param name="href">The address.</param>
    /// <param name="opener">Opens the address instead of the platform; for tests.</param>
    public static void Open(string? href, Func<Uri, Task>? opener = null)
    {
        if (!TryParse(href, out var uri)) return;
        _ = Run(() => (opener ?? OpenAsync)(uri));
    }

    /// <summary>Copies <paramref name="text"/> to the clipboard. Nothing happens for an empty text or where the platform has no clipboard.</summary>
    /// <param name="text">The text.</param>
    /// <param name="copier">Writes the text instead of the platform; for tests.</param>
    public static void Copy(string? text, Func<string, Task>? copier = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        _ = Run(() => (copier ?? Clipboard.Default.SetTextAsync)(text));
    }

    private static async Task Run(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception)
        {
            // No browser, no app for the scheme, no clipboard, or no platform at all: a link and a copy button have nowhere to report it, and the page keeps working.
        }
    }

    private static async Task OpenAsync(Uri uri)
    {
        if (uri.Scheme is "http" or "https") await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        else await Launcher.Default.OpenAsync(uri);
    }
}
