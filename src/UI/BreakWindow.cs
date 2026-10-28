using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Petcompanion;

internal sealed class BreakWindow : Window
{
    private readonly TextBlock _countdown;
    public BreakWindow(ReminderRequest request, Action<bool> finish, Action mute, IPetDefinition definition)
    {
        bool water = request.Kind == BreakKind.Water;
        bool canSnooze = request.Kind != null || request.CustomId != null;
        Ui.ApplyTheme(this);
        Icon = Ui.Logo;
        if (request.Style == ReminderStyle.Popup)
        {
            Title = "Petcompanion reminder"; Width = 420; Height = 330;
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            Topmost = true; ShowInTaskbar = false; ShowActivated = false; Background = Ui.Background;
            var panel = new StackPanel { Margin = new Thickness(22) };
            panel.Children.Add(Ui.Text(definition.DisplayName.ToUpperInvariant() + " / A LITTLE NUDGE", 12, Ui.Accent));
            panel.Children.Add(Ui.Text(request.Title, 23));
            panel.Children.Add(Ui.Text(request.Message, 16, Ui.Muted));
            _countdown = Ui.Text("", 14, Ui.Accent); panel.Children.Add(_countdown);
            var popupActions = new WrapPanel();
            popupActions.Children.Add(Ui.Button("Done", () => finish(false), true));
            if (canSnooze) popupActions.Children.Add(Ui.Button("Snooze 5 min", () => finish(true)));
            popupActions.Children.Add(Ui.Button("Skip", () => finish(false)));
            panel.Children.Add(popupActions);
            Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { e.Handled = true; finish(false); } };
            return;
        }
        Title = water ? "Petcompanion — water break" : "Petcompanion — movement break";
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; Topmost = true;
        Background = Ui.Background; ShowInTaskbar = true;
        var root = new Grid { Background = new RadialGradientBrush(Color.FromRgb(255, 249, 238), Color.FromRgb(232, 215, 221)) };
        var content = new Grid { Width = 1050, Height = 650, Margin = new Thickness(35) };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(520) });
        content.ColumnDefinitions.Add(new ColumnDefinition());
        content.Children.Add(definition.CreatePresentation(500, true).View);
        var copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(30, 0, 0, 0) };
        Grid.SetColumn(copy, 1);
        copy.Children.Add(Ui.Text("P E T C O M P A N I O N", 14, Ui.Accent));
        copy.Children.Add(Ui.Text(request.Title, 54));
        copy.Children.Add(Ui.Text(request.Message, 21, Ui.Muted));
        _countdown = Ui.Text("00:30", 38, Ui.Accent); _countdown.Margin = new Thickness(0, 24, 0, 26);
        copy.Children.Add(_countdown);
        var actions = new WrapPanel();
        actions.Children.Add(Ui.Button("Done", () => finish(false), true));
        if (canSnooze) actions.Children.Add(Ui.Button("Snooze 5 min", () => finish(true)));
        actions.Children.Add(Ui.Button("Skip", () => finish(false)));
        copy.Children.Add(actions);
        if (request.Kind is BreakKind audioKind && (definition.IntroAudio != null || definition.ReminderAudio(audioKind) != null))
            copy.Children.Add(Ui.Button("Mute sound", mute));
        copy.Children.Add(Ui.Text("Esc always dismisses this reminder.", 13, Ui.Muted));
        content.Children.Add(copy);
        root.Children.Add(new Viewbox { Child = content, Stretch = Stretch.Uniform, Margin = new Thickness(12) });
        Content = root;
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { e.Handled = true; finish(false); } };
    }
    public void Update(int seconds) => _countdown.Text = TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(@"mm\:ss");
}
