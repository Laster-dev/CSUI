using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CSUI.Markup;

namespace CSUI.Previewer
{
    internal sealed class PreviewLoadResult
    {
        public bool Success { get; init; }
        public UIElement VisualContent { get; init; }
        public ResourceDictionary Resources { get; init; }
        public Brush Background { get; init; }
        public string DocumentTitle { get; init; }
        public double SuggestedWidth { get; init; } = double.NaN;
        public double SuggestedHeight { get; init; } = double.NaN;
        public string ErrorMessage { get; init; }
    }

    internal static class PreviewDocumentLoader
    {
        public static string ReadFileWithRetry(string filePath)
        {
            const int maxAttempts = 5;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
                catch (IOException) when (attempt < maxAttempts - 1)
                {
                    Thread.Sleep(25);
                }
            }
            return File.ReadAllText(filePath);
        }

        public static PreviewLoadResult LoadFromXaml(string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml))
            {
                return new PreviewLoadResult
                {
                    Success = false,
                    ErrorMessage = "XAML file is empty."
                };
            }

            try
            {
                var context = new XamlLoadContext();
                object root = XamlMarkup.Load<object>(xaml, context);

                if (root is Window parsedWindow)
                {
                    object winContent = parsedWindow.Content;
                    parsedWindow.Content = null;

                    UIElement visual = winContent as UIElement ?? new ContentControl { Content = winContent };
                    return new PreviewLoadResult
                    {
                        Success = true,
                        VisualContent = visual,
                        Resources = parsedWindow.Resources,
                        Background = parsedWindow.Background,
                        DocumentTitle = parsedWindow.Title,
                        SuggestedWidth = parsedWindow.Width,
                        SuggestedHeight = parsedWindow.Height
                    };
                }

                if (root is UIElement uie)
                {
                    FrameworkElement fe = uie as FrameworkElement;
                    return new PreviewLoadResult
                    {
                        Success = true,
                        VisualContent = uie,
                        Resources = fe?.Resources,
                        Background = (uie as Control)?.Background ?? (uie as Panel)?.Background,
                        SuggestedWidth = fe != null ? fe.Width : double.NaN,
                        SuggestedHeight = fe != null ? fe.Height : double.NaN
                    };
                }

                if (root is ResourceDictionary rd)
                {
                    return new PreviewLoadResult
                    {
                        Success = true,
                        VisualContent = new TextBlock
                        {
                            Text = $"ResourceDictionary loaded ({rd.Count} entries).",
                            Margin = new Thickness(16),
                            Foreground = Brushes.White
                        },
                        Resources = rd
                    };
                }

                return new PreviewLoadResult
                {
                    Success = false,
                    ErrorMessage = $"Unsupported root element type: {root?.GetType().Name ?? "null"}"
                };
            }
            catch (Exception ex)
            {
                return new PreviewLoadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
