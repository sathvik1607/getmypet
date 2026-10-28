using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Petcompanion;

internal sealed class SettingsWindow : Window
{
    private sealed record Choice<T>(string Name, T Value);

    public SettingsWindow(Settings current, Action<Settings> save, Action<BreakKind> preview, Action quit,
        Action<CustomReminder> previewCustom, Action<PetInteraction> interact,
        Action<int>? startFocus = null, Action? endFocus = null, Func<string>? focusStatus = null,
        Func<string>? reminderStatus = null, Action? previewSound = null)
    {
        Ui.ApplyTheme(this);
        Icon = Ui.Logo;
        Title = "Petcompanion - your companion";
        Width = Math.Min(1080, SystemParameters.WorkArea.Width - 30);
        Height = Math.Min(820, SystemParameters.WorkArea.Height - 30);
        MinWidth = Math.Min(820, Width); MinHeight = Math.Min(600, Height);
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        FrameworkElement? focusSection = null;
        var header = new Grid { Margin = new Thickness(28, 22, 24, 16) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new Image { Source = Ui.Logo, Width = 46, Height = 46, Margin = new Thickness(0, 0, 15, 0) });
        var heading = new StackPanel(); Grid.SetColumn(heading, 1);
        heading.Children.Add(Ui.Text("A little company. A better workday.", 24, Ui.Foreground));
        var subheading = Ui.Text("Your companion, your pace.", 13, Ui.Muted); subheading.Margin = new Thickness(0); heading.Children.Add(subheading);
        header.Children.Add(heading);
        var close = Ui.Button("Close settings", Close); close.VerticalAlignment = VerticalAlignment.Center; close.Margin = new Thickness(14, 0, 0, 0);
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var focusShortcut = Ui.Button("Focus", () => focusSection?.BringIntoView());
        focusShortcut.Margin = new Thickness(8, 0, 0, 0);
        headerActions.Children.Add(focusShortcut); headerActions.Children.Add(close);
        Grid.SetColumn(headerActions, 2); header.Children.Add(headerActions); root.Children.Add(header);

        var body = new Grid { Margin = new Thickness(22, 0, 18, 14) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        body.ColumnDefinitions.Add(new ColumnDefinition());
        var sidebar = new StackPanel { Margin = new Thickness(0, 0, 3, 0) };
        var content = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        var sidebarScroll = new ScrollViewer { Content = sidebar, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var contentScroll = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        body.Children.Add(sidebarScroll); Grid.SetColumn(contentScroll, 2); body.Children.Add(contentScroll);
        Grid.SetRow(body, 1); root.Children.Add(body);

        var companion = new StackPanel(); sidebar.Children.Add(Ui.Card(companion));
        companion.Children.Add(Ui.Text("Your companion", 19));
        var previewHost = new Border { Height = 215, CornerRadius = new CornerRadius(14), ClipToBounds = true,
            Background = new RadialGradientBrush(Color.FromRgb(255, 250, 240), Color.FromRgb(239, 224, 221)), Margin = new Thickness(0, 0, 0, 16) };
        companion.Children.Add(previewHost);
        var pet = new ComboBox { ItemsSource = PetCatalog.All, DisplayMemberPath = "DisplayName", SelectedValuePath = "Id",
            SelectedValue = current.PetId, FontSize = 15, Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 12) };
        System.Windows.Automation.AutomationProperties.SetName(pet, "Your companion");
        companion.Children.Add(pet);
        var petDescription = Ui.Text("", 13, Ui.Muted); companion.Children.Add(petDescription);
        var previewNote = Ui.Text("Choose a companion, then Save to bring it to your desktop.", 12, Ui.Muted); companion.Children.Add(previewNote);
        var interactions = new WrapPanel(); companion.Children.Add(interactions);
        string appliedPetId = current.PetId;
        IPetVisual? previewVisual = null;
        var actionButtons = new Dictionary<PetInteraction, Button>();
        foreach (var pair in new[] { ("Pet companion", PetInteraction.Pet), ("Boop", PetInteraction.Boop),
            ("Give a treat", PetInteraction.Treat), ("Play with yarn", PetInteraction.Play),
            ("Take a nap", PetInteraction.Nap), ("Wake up", PetInteraction.Wake) })
        {
            var action = pair.Item2;
            var button = Ui.Button(pair.Item1, () => interact(action));
            button.FontSize = 12; button.Padding = new Thickness(10, 8, 10, 8); button.MinHeight = 34;
            actionButtons[action] = button; interactions.Children.Add(button);
        }
        void RefreshInteractionAvailability()
        {
            interactions.Visibility = previewVisual is IInteractivePetVisual ? Visibility.Visible : Visibility.Collapsed;
            interactions.IsEnabled = appliedPetId == (string?)pet.SelectedValue;
            actionButtons[PetInteraction.Play].Content = (string?)pet.SelectedValue == "owl" ? "Play with a firefly" : "Play with yarn";
        }
        void RefreshPreview()
        {
            var definition = PetCatalog.Find((string?)pet.SelectedValue);
            // Replacing the visual unloads the previous model, stopping its own animation timer.
            previewHost.Child = null;
            var presentation = definition.CreatePresentation(280, false);
            previewVisual = presentation.Visual;
            if (previewVisual is IGroundPetVisual grounded) grounded.Activity = PetActivity.Sitting;
            presentation.Visual.FacingRight = true;
            previewHost.Child = new Viewbox { Child = presentation.View, Stretch = Stretch.Uniform, Margin = new Thickness(2, -8, 2, 0) };
            petDescription.Text = definition.Id switch
            {
                "owl" => "A curious little owl with soft feathers, bright eyes and playful reactions. A calm perch companion with a soft hoot.",
                "cat" => "A soft little tabby for your desktop. Pet, play, share a treat, or settle down for a nap. Full-screen reminders use a gentle mew.",
                "cat-3d" => "The original orange cat in 3D, kept here for comparison. Walks, sits, grooms and naps. Full-screen reminders use a gentle mew.",
                _ => "An animated dragon that glides around the outer screen. Its full-screen reminders include a roar and spoken message."
            };
            RefreshInteractionAvailability();
        }
        pet.SelectionChanged += (_, _) => RefreshPreview(); RefreshPreview();

        var focus = new StackPanel(); var focusCard = Ui.Card(focus); focusSection = focusCard; sidebar.Children.Add(focusCard);
        focus.Children.Add(Ui.Text("A little room to focus", 18));
        focus.Children.Add(Ui.Text("Your pet settles into a corner. Scheduled reminders stay quiet and appear as small popups until the session ends.", 13, Ui.Muted));
        var focusLabel = Ui.Text("", 15, Ui.Accent); focus.Children.Add(focusLabel);
        var focusChoices = new WrapPanel(); focus.Children.Add(focusChoices);
        foreach (int minutes in new[] { 15, 25, 45 })
        {
            int focusMinutes = minutes;
            var start = Ui.Button($"{minutes} min", () => { startFocus?.Invoke(focusMinutes); focusLabel.Text = focusStatus?.Invoke() ?? "Session started."; });
            start.Padding = new Thickness(12, 9, 12, 9); start.FontSize = 13; start.IsEnabled = startFocus != null;
            focusChoices.Children.Add(start);
        }
        var end = Ui.Button("End session", () => { endFocus?.Invoke(); focusLabel.Text = focusStatus?.Invoke() ?? "Ready for your next session."; });
        end.FontSize = 13; end.Padding = new Thickness(12, 8, 12, 8); end.IsEnabled = endFocus != null; focus.Children.Add(end);

        var everyday = new StackPanel(); content.Children.Add(Ui.Card(everyday));
        everyday.Children.Add(Ui.Text("Make yourself at home", 19));
        var sizeLabel = Ui.Text($"Pet size  {current.PetSizePercent}%", 13, Ui.Muted); everyday.Children.Add(sizeLabel);
        var petSize = new Slider { Minimum = 50, Maximum = 200, Value = current.PetSizePercent, TickFrequency = 10,
            IsSnapToTickEnabled = true, Margin = new Thickness(0, 0, 0, 12) };
        petSize.ValueChanged += (_, _) => sizeLabel.Text = $"Pet size  {(int)petSize.Value}%"; everyday.Children.Add(petSize);
        everyday.Children.Add(Ui.Text("Movement", 13, Ui.Muted));
        var movement = new ComboBox { ItemsSource = new[] { new Choice<Movement>("Wander", Movement.Wander),
            new Choice<Movement>("Follow pointer", Movement.Follow), new Choice<Movement>("Stay in place", Movement.Stay) },
            DisplayMemberPath = "Name", SelectedValuePath = "Value", SelectedValue = current.Movement, FontSize = 15, Padding = new Thickness(8) };
        everyday.Children.Add(movement);

        var reminders = new StackPanel(); content.Children.Add(Ui.Card(reminders));
        reminders.Children.Add(Ui.Text("Small pauses, well timed", 19));
        var nextReminder = Ui.Text("", 13, Ui.Accent); reminders.Children.Add(nextReminder);
        TextBox NumberField(Panel target, string label, int value)
        {
            target.Children.Add(Ui.Text(label, 13, Ui.Muted));
            var box = new TextBox { Text = value.ToString(), FontSize = 16, Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 14) };
            System.Windows.Automation.AutomationProperties.SetName(box, label);
            target.Children.Add(box); return box;
        }
        var intervals = new Grid(); intervals.ColumnDefinitions.Add(new ColumnDefinition());
        intervals.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) }); intervals.ColumnDefinitions.Add(new ColumnDefinition());
        var waterColumn = new StackPanel(); intervals.Children.Add(waterColumn);
        var stretchColumn = new StackPanel(); Grid.SetColumn(stretchColumn, 2); intervals.Children.Add(stretchColumn);
        var water = NumberField(waterColumn, "Water - minutes", current.WaterMinutes);
        var stretch = NumberField(stretchColumn, "Movement break - minutes", current.StretchMinutes);
        reminders.Children.Add(intervals);
        reminders.Children.Add(Ui.Text("Reminder style", 13, Ui.Muted));
        ComboBox StylePicker(ReminderStyle selected) => new() { ItemsSource = new[] {
            new Choice<ReminderStyle>("Full-screen moment", ReminderStyle.FullScreen), new Choice<ReminderStyle>("Quiet corner popup", ReminderStyle.Popup) },
            DisplayMemberPath = "Name", SelectedValuePath = "Value", SelectedValue = selected, FontSize = 15, Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 12) };
        var style = StylePicker(current.Style); reminders.Children.Add(style);
        reminders.Children.Add(Ui.Text("Corner popups stay silent and keep keyboard focus in your work. Full-screen reminders can always be skipped or snoozed.", 12, Ui.Muted));
        var duration = NumberField(reminders, "Automatic dismissal - seconds", current.BreakSeconds);

        var custom = new StackPanel(); content.Children.Add(Ui.Card(custom));
        custom.Children.Add(Ui.Text("Remember your own things", 19));
        custom.Children.Add(Ui.Text("A message, a repeat interval, and the style that suits it. Up to 240 characters; repeat every 1 minute to 7 days.", 13, Ui.Muted));
        var rows = new StackPanel(); custom.Children.Add(rows);
        var entries = new List<(CustomReminder Reminder, TextBox Message, TextBox Minutes, CheckBox Enabled, ComboBox Style)>();
        var empty = Ui.Text("No custom reminders yet. Add one for anything you want a gentle nudge about.", 13, Ui.Muted); rows.Children.Add(empty);
        void AddReminder(CustomReminder reminder)
        {
            empty.Visibility = Visibility.Collapsed;
            var row = new StackPanel();
            var card = new Border { Child = row, Background = Ui.Background, CornerRadius = new CornerRadius(10), Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 12) };
            row.Children.Add(Ui.Text("Your message", 13, Ui.Muted));
            var message = new TextBox { Text = reminder.Message, MaxLength = 240, FontSize = 15, Padding = new Thickness(9), Margin = new Thickness(0, 0, 0, 12) };
            row.Children.Add(message);
            var minutes = NumberField(row, "Repeat every - minutes", reminder.Minutes);
            var mode = StylePicker(reminder.Style); row.Children.Add(mode);
            var enabled = new CheckBox { Content = "Reminder enabled", IsChecked = reminder.Enabled, Foreground = Ui.Foreground, Margin = new Thickness(0, 0, 0, 12) };
            row.Children.Add(enabled);
            var entry = (reminder, message, minutes, enabled, mode); entries.Add(entry);
            var actions = new WrapPanel();
            actions.Children.Add(Ui.Button("Test reminder", () =>
            {
                if (!string.IsNullOrWhiteSpace(message.Text)) previewCustom(reminder with { Message = message.Text.Trim(), Style = (ReminderStyle)mode.SelectedValue });
            }));
            actions.Children.Add(Ui.Button("Remove reminder", () => { entries.Remove(entry); rows.Children.Remove(card); empty.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed; }));
            row.Children.Add(actions); rows.Children.Add(card);
            if (IsLoaded) message.Focus();
        }
        foreach (var reminder in current.CustomReminders) AddReminder(reminder with { });
        custom.Children.Add(Ui.Button("Add custom reminder", () => AddReminder(new CustomReminder())));

        var preferences = new StackPanel(); content.Children.Add(Ui.Card(preferences));
        preferences.Children.Add(Ui.Text("Keep it comfortable", 19));
        preferences.Children.Add(Ui.Text("Reminder audio volume", 13, Ui.Muted));
        var volume = new Slider { Minimum = 0, Maximum = 100, Value = current.Volume, TickFrequency = 10, Margin = new Thickness(0, 0, 0, 16) }; preferences.Children.Add(volume);
        var muted = new CheckBox { Content = "Mute all pet and reminder sounds", IsChecked = current.Muted, Foreground = Ui.Foreground, Margin = new Thickness(0, 0, 0, 14) };
        var quiet = new CheckBox { Content = "Quiet hours: 10 pm to 8 am", IsChecked = current.QuietHours, Foreground = Ui.Foreground, Margin = new Thickness(0, 0, 0, 8) };
        preferences.Children.Add(muted); preferences.Children.Add(quiet);
        var clickThrough = new CheckBox { Content = "Let clicks pass through the pet", IsChecked = current.ClickThrough, Foreground = Ui.Foreground, Margin = new Thickness(0, 8, 0, 8) };
        preferences.Children.Add(clickThrough);
        preferences.Children.Add(Ui.Text("Use the tray icon to turn interaction back on.", 12, Ui.Muted));
        preferences.Children.Add(Ui.Text("Quiet hours stop automatic reminders. Manual previews still work. Owl and cat use gentle sounds. Corner popups stay silent.", 12, Ui.Muted));

        var footer = new Border { Background = Ui.CardBackground, BorderBrush = Ui.Outline, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(26, 12, 26, 12) };
        var footerLayout = new StackPanel(); footer.Child = footerLayout;
        var status = Ui.Text("Save applies your changes. Closing settings keeps your companion running.", 12, Ui.Muted); status.Margin = new Thickness(0, 0, 0, 8); footerLayout.Children.Add(status);
        bool Apply()
        {
            if (!int.TryParse(water.Text, out int w) || w < 1 || w > 240 || !int.TryParse(stretch.Text, out int s) || s < 1 || s > 240 ||
                !int.TryParse(duration.Text, out int d) || d < 5 || d > 300)
            { status.Foreground = Ui.Error; status.Text = "Use intervals from 1 to 240 minutes and a dismissal time from 5 to 300 seconds."; return false; }
            var customReminders = new List<CustomReminder>();
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Message.Text) || !int.TryParse(entry.Minutes.Text, out int minutes) || minutes < 1 || minutes > 10080)
                { status.Foreground = Ui.Error; status.Text = "Each custom reminder needs a message and an interval from 1 to 10080 minutes."; return false; }
                customReminders.Add(entry.Reminder with { Message = entry.Message.Text.Trim(), Minutes = minutes,
                    Enabled = entry.Enabled.IsChecked == true, Style = (ReminderStyle)entry.Style.SelectedValue });
            }
            try
            {
                save(current with { PetId = (string?)pet.SelectedValue ?? "dragon", PetSizePercent = (int)petSize.Value,
                    Style = (ReminderStyle)style.SelectedValue, CustomReminders = customReminders, WaterMinutes = w, StretchMinutes = s,
                    BreakSeconds = d, Volume = (int)volume.Value, Muted = muted.IsChecked == true, QuietHours = quiet.IsChecked == true,
                    Movement = (Movement)movement.SelectedValue, ClickThrough = clickThrough.IsChecked == true });
                appliedPetId = (string?)pet.SelectedValue ?? "dragon"; RefreshInteractionAvailability();
                status.Foreground = Ui.Success; status.Text = "Saved. Your companion is ready and reminder timers have restarted."; return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { status.Foreground = Ui.Error; status.Text = "Could not save changes: " + ex.Message; return false; }
        }
        var soundTest = Ui.Button("Test sound", () => { if (Apply()) previewSound?.Invoke(); });
        soundTest.ToolTip = "Plays your companion's sound at the selected volume, even during quiet hours. Corner popups remain silent.";
        void UpdateSoundTest() => soundTest.IsEnabled = previewSound != null && muted.IsChecked != true && volume.Value > 0;
        muted.Checked += (_, _) => UpdateSoundTest(); muted.Unchecked += (_, _) => UpdateSoundTest();
        volume.ValueChanged += (_, _) => UpdateSoundTest(); UpdateSoundTest();

        var footerButtons = new WrapPanel(); footerLayout.Children.Add(footerButtons);
        footerButtons.Children.Add(Ui.Button("Save", () => { Apply(); }, true));
        footerButtons.Children.Add(Ui.Button("Test water", () => { if (Apply()) preview(BreakKind.Water); }));
        footerButtons.Children.Add(Ui.Button("Test stretch", () => { if (Apply()) preview(BreakKind.Stretch); }));
        footerButtons.Children.Add(soundTest);
        var quitButton = Ui.Button("Quit pet", quit); quitButton.Margin = new Thickness(15, 0, 0, 0); footerButtons.Children.Add(quitButton);
        Grid.SetRow(footer, 2); root.Children.Add(footer);
        Content = root;

        void RefreshStatus()
        {
            focusLabel.Text = focusStatus?.Invoke() ?? "Ready for your next session.";
            nextReminder.Text = reminderStatus?.Invoke() ?? "Automatic reminders follow your intervals and quiet hours.";
        }
        var statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        statusTimer.Tick += (_, _) => RefreshStatus();
        Loaded += (_, _) => { RefreshStatus(); statusTimer.Start(); };
        Closed += (_, _) => { statusTimer.Stop(); previewHost.Child = null; previewVisual = null; };
    }
}
