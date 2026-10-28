using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Petcompanion;

internal static class Ui
{
    public static BitmapImage Logo => new(new Uri(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Brand", "app.ico")));
    // A warm paper-and-plum palette shared by the dashboard and reminders.
    public static Brush Background => Solid(250, 244, 235);
    public static Brush CardBackground => Solid(255, 252, 247);
    public static Brush Foreground => Solid(56, 38, 56);
    public static Brush Accent => Solid(155, 71, 46);
    public static Brush Muted => Solid(116, 97, 107);
    public static Brush Surface => Solid(239, 226, 221);
    public static Brush Outline => Solid(224, 208, 205);
    public static Brush Error => Solid(164, 34, 48);
    public static Brush Success => Solid(62, 104, 75);

    private static Brush Solid(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue)); brush.Freeze(); return brush;
    }

    public static Border Card(UIElement content) => new()
    {
        Child = content, Background = CardBackground, BorderBrush = Outline, BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(16), Padding = new Thickness(18), Margin = new Thickness(0, 0, 0, 14)
    };

    public static void ApplyTheme(Window window)
    {
        window.Background = Background; window.Foreground = Foreground; window.FontFamily = new FontFamily("Segoe UI");
        window.Resources[SystemColors.HighlightBrushKey] = Accent;
        window.Resources[SystemColors.HighlightTextBrushKey] = Brushes.White;
        var input = new Style(typeof(TextBox));
        input.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
        input.Setters.Add(new Setter(Control.ForegroundProperty, Foreground));
        input.Setters.Add(new Setter(Control.BorderBrushProperty, Outline));
        input.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        input.Setters.Add(new Setter(TextBox.CaretBrushProperty, Accent));
        var focusedInput = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
        focusedInput.Setters.Add(new Setter(Control.BorderBrushProperty, Accent)); input.Triggers.Add(focusedInput);
        window.Resources[typeof(TextBox)] = input;
        var choice = new Style(typeof(ComboBox));
        choice.Setters.Add(new Setter(Control.BackgroundProperty, Surface));
        choice.Setters.Add(new Setter(Control.ForegroundProperty, Foreground));
        choice.Setters.Add(new Setter(Control.BorderBrushProperty, Outline));
        window.Resources[typeof(ComboBox)] = choice;
        var check = new Style(typeof(CheckBox)); check.Setters.Add(new Setter(Control.ForegroundProperty, Foreground));
        check.Setters.Add(new Setter(Control.FontSizeProperty, 13d)); window.Resources[typeof(CheckBox)] = check;
    }

    public static TextBlock Text(string text, double size = 16, Brush? color = null) => new()
    {
        Text = text, FontSize = size, Foreground = color ?? Foreground,
        FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10)
    };

    public static Button Button(string title, Action action, bool primary = false)
    {
        var button = new Button
        {
            Content = title, FontSize = 14, Padding = new Thickness(16, 10, 16, 10),
            Margin = new Thickness(0, 0, 8, 8), Cursor = Cursors.Hand,
            Background = primary ? Foreground : Surface, Foreground = primary ? Brushes.White : Foreground,
            BorderThickness = new Thickness(0), MinHeight = 40
        };
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, Accent);
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        var template = new ControlTemplate(typeof(Button)) { VisualTree = border };
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(UIElement.OpacityProperty, .83)); template.Triggers.Add(hover);
        var focus = new Trigger { Property = UIElement.IsKeyboardFocusedProperty, Value = true };
        focus.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(2))); template.Triggers.Add(focus);
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, .45)); template.Triggers.Add(disabled);
        button.Template = template; button.Click += (_, _) => action(); return button;
    }
}
