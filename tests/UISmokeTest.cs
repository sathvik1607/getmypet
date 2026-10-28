using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Petcompanion;

internal sealed partial class PetController
{
    private void SmokeTick()
    {
        try
        {
            _smokeStep++;
            if (_smokeStep == 2)
            {
                if (_overlay == null) throw new Exception("Water overlay missing");
                if (FindModel(_overlay).PoseUpdates < 3) throw new Exception("Dragon geometry is not animating");
                Capture(_overlay, "water-preview.png");
                FindButton(_overlay, "Snooze 5 min").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (_overlay != null || (_schedule.WaterDue - DateTimeOffset.Now).TotalMinutes < 4.9) throw new Exception("Snooze did not close and reschedule");
            }
            if (_smokeStep == 3) ShowBreak(BreakKind.Stretch);
            if (_smokeStep == 5)
            {
                if (_overlay == null) throw new Exception("Stretch overlay missing");
                Capture(_overlay, "stretch-preview.png");
                var key = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(_overlay), 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
                _overlay.RaiseEvent(key);
                if (_overlay != null) throw new Exception("Escape did not dismiss");
            }
            if (_smokeStep == 6)
            {
                if (!_pet.IsVisible) throw new Exception("Pet did not return");
                OpenSettings();
            }
            if (_smokeStep == 7)
            {
                if (_settingsWindow == null) throw new Exception("Settings missing");
                Capture(_settingsWindow, "settings-preview.png");
                OrientPet(0, -100);
                if (_pet.Model.TravelHeadingDegrees != -90) throw new Exception("Upward heading not applied");
                OrientPet(0, 100);
                if (_pet.Model.TravelHeadingDegrees != 90) throw new Exception("Downward heading not applied");
                OrientPet(100, 100);
                if (_pet.Model.TravelHeadingDegrees != 45) throw new Exception("Diagonal heading not applied");
                _settings.Movement = Movement.Stay;
                _settings.PetSizePercent = 50; ResizePet();
                GetWindowRect(new WindowInteropHelper(_pet).Handle, out var small);
                _settings.PetSizePercent = 200; ResizePet();
                GetWindowRect(new WindowInteropHelper(_pet).Handle, out var large);
                if (large.Right - large.Left <= small.Right - small.Left || large.Bottom - large.Top <= small.Bottom - small.Top)
                    throw new Exception("Pet size did not change");
                var area = Forms.Screen.FromHandle(new WindowInteropHelper(_pet).Handle).WorkingArea;
                if (large.Left < area.Left || large.Top < area.Top || large.Right > area.Right || large.Bottom > area.Bottom)
                    throw new Exception("Resized pet escaped working area");
                Capture(_pet, "large-pet-preview.png");
                _settings.PetSizePercent = 100; ResizePet();
                _settingsWindow.Close();
                _settings.Movement = Movement.Wander; _wander.Reset();
                GetWindowRect(new WindowInteropHelper(_pet).Handle, out _smokePosition);
            }
            if (_smokeStep == 9)
            {
                GetWindowRect(new WindowInteropHelper(_pet).Handle, out var current);
                if (current.Left == _smokePosition.Left && current.Top == _smokePosition.Top) throw new Exception("Edge wandering did not move the pet");
                Capture(_pet, "pet-preview.png");
                _paused = true; _smokePosition = current;
            }
            if (_smokeStep == 10)
            {
                GetWindowRect(new WindowInteropHelper(_pet).Handle, out var current);
                if (current.Left != _smokePosition.Left || current.Top != _smokePosition.Top) throw new Exception("Pause did not stop flight");
                _paused = false; _settings.Movement = Movement.Follow;
            }
            if (_smokeStep == 12)
            {
                GetWindowRect(new WindowInteropHelper(_pet).Handle, out var current);
                if (current.Left == _smokePosition.Left && current.Top == _smokePosition.Top) throw new Exception("Follow did not move the pet");
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test.log"), "PASS: continuously animated 3D geometry; water overlay; Snooze click; five-minute reschedule; stretch overlay; Escape dismissal; pet restored; settings opened; pet size 50/200 percent and screen bounds; edge wandering; pause; pointer-follow movement. Audio muted during UI test.\n");
            }
            if (_smokeStep == 13)
            {
                var reminder = new CustomReminder { Message = "Look away from the screen", Minutes = 10 };
                _settings.CustomReminders.Add(reminder);
                _schedule.Reset(_settings, DateTimeOffset.Now);
                ShowReminder(new(null, reminder.Id, "A little reminder", reminder.Message, ReminderStyle.Popup));
            }
            if (_smokeStep == 15)
            {
                if (_overlay == null || _overlay.IsActive || !_pet.IsVisible || _overlay.ShowActivated)
                    throw new Exception($"Popup state: missing={_overlay == null}, active={_overlay?.IsActive}, petVisible={_pet.IsVisible}, showActivated={_overlay?.ShowActivated}");
                GetWindowRect(new WindowInteropHelper(_overlay).Handle, out var popupRect);
                var area = Forms.Screen.FromHandle(new WindowInteropHelper(_pet).Handle).WorkingArea;
                if (popupRect.Left < area.Left || popupRect.Top < area.Top || popupRect.Right > area.Right || popupRect.Bottom > area.Bottom)
                    throw new Exception("Popup outside working area");
                Capture(_overlay, "popup-preview.png");
                FindButton(_overlay, "Snooze 5 min").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (_overlay != null) throw new Exception("Popup snooze failed");
                if (_schedule.Next(_settings, DateTimeOffset.Now.AddMinutes(4)) != null) throw new Exception("Popup snooze too short");
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test.log"), "PASS: custom popup; no activation; pet stays visible; monitor working-area placement; popup snooze.\n");
                _paused = true;
                OrientPet(0, -100);
            }
            if (_smokeStep == 17)
            {
                if (_pet.Model is not DragonModel model) throw new Exception("Expected dragon for rendering check");
                var viewport = (Viewport3D)model.Children[0];
                double actual = ((RotateTransform)viewport.RenderTransform).Angle;
                double expected = PetHeading.Relative(-90, model.FacingRight);
                if (Math.Abs(PetHeading.Delta(actual, expected)) > 2) throw new Exception("Rendered dragon did not turn vertically");
                AssertPetClearance();
                Capture(_pet, "vertical-pet-preview.png");
                OrientPet(100, 100);
            }
            if (_smokeStep == 19)
            {
                var model = (DragonModel)_pet.Model;
                var viewport = (Viewport3D)model.Children[0];
                double actual = ((RotateTransform)viewport.RenderTransform).Angle;
                if (Math.Abs(PetHeading.Delta(actual, 45)) > 2) throw new Exception("Rendered dragon did not turn diagonally");
                AssertPetClearance();
                Capture(_pet, "diagonal-pet-preview.png");
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test.log"), "PASS: rendered vertical and diagonal travel orientation with fixed scale and unclipped host.\n");
            }
            if (_smokeStep == 20) { _paused = false; OpenSettings(); }
            if (_smokeStep == 21)
            {
                if (_settingsWindow == null) throw new Exception("Settings missing for pet switch");
                var oldHandle = new WindowInteropHelper(_pet).Handle;
                FindPetPicker(_settingsWindow).SelectedValue = "cat";
                FindButton(_settingsWindow, "Save").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (_settings.PetId != "cat" || _pet.Model is not IGroundPetVisual || oldHandle != new WindowInteropHelper(_pet).Handle)
                    throw new Exception("Live cat selection failed");
                Capture(_settingsWindow, "cat-settings-preview.png");
                _settingsWindow.Close();
                _settings.Movement = Movement.Wander; _ground.Reset();
                GetWindowRect(oldHandle, out _smokePosition);
            }
            if (_smokeStep == 24)
            {
                GetWindowRect(new WindowInteropHelper(_pet).Handle, out var position);
                if (position.Left == _smokePosition.Left) throw new Exception("Cat did not walk");
                if (((IGroundPetVisual)_pet.Model).PoseUpdates < 5) throw new Exception("Cat is not animating");
                Capture(_pet, "cat-walking-preview.png");
                _paused = true; ((IGroundPetVisual)_pet.Model).Activity = PetActivity.Sitting;
            }
            if (_smokeStep == 26) { Capture(_pet, "cat-sitting-preview.png"); ((IGroundPetVisual)_pet.Model).Activity = PetActivity.Grooming; }
            if (_smokeStep == 28) { Capture(_pet, "cat-grooming-preview.png"); ((IGroundPetVisual)_pet.Model).Activity = PetActivity.Sleeping; }
            if (_smokeStep == 30) Capture(_pet, "cat-sleeping-preview.png");
            if (_smokeStep == 31) ShowBreak(BreakKind.Water);
            if (_smokeStep == 32)
            {
                if (_overlay == null || FindModel(_overlay) is not IGroundPetVisual || _nextAudio != null) throw new Exception("Cat reminder model/audio incorrect");
                Capture(_overlay, "cat-reminder-preview.png");
                FindButton(_overlay, "Snooze 5 min").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (_overlay != null) throw new Exception("Cat reminder snooze failed");
            }
            if (_smokeStep == 33)
            {
                ShowReminder(new(null, null, "Cat reminder", "Time to check your task", ReminderStyle.Popup));
                if (_overlay == null || _overlay.ShowActivated || !_pet.IsVisible) throw new Exception("Cat popup state incorrect");
                Capture(_overlay, "cat-popup-preview.png");
                FindButton(_overlay, "Done").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                OpenSettings();
                FindPetPicker(_settingsWindow!).SelectedValue = "cat-3d";
                FindButton(_settingsWindow!, "Save").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (_pet.Model is not CatModel) throw new Exception("Preserved 3D preview unavailable");
                FindPetPicker(_settingsWindow!).SelectedValue = "dragon";
                FindButton(_settingsWindow!, "Save").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (_pet.Model is not DragonModel) throw new Exception("Switching back to dragon failed");
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test.log"), "PASS: live cat selection through settings; stable host; walking; sitting/grooming/sleeping previews; cat full-screen reminder and snooze; cat popup; switch back to dragon.\n");
            }
            if (_smokeStep == 34)
            {
                FindPetPicker(_settingsWindow!).SelectedValue = "cat";
                FindButton(_settingsWindow!, "Save").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (_pet.Model is not IInteractivePetVisual) throw new Exception("2D cat interaction capability missing");
                Capture(_settingsWindow!, "cat-interaction-settings.png");
                _settingsWindow!.Close();
                _paused = false; _settings.Movement = Movement.Wander; _ground.Reset();
                var item = ContextMenu().Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Give a treat"));
                item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                GetWindowRect(new WindowInteropHelper(_pet).Handle, out _smokePosition);
            }
            if (_smokeStep == 35)
            {
                var cat = (IInteractivePetVisual)_pet.Model;
                if (cat.CurrentInteraction != PetInteraction.Treat) throw new Exception("Treat menu action failed");
                GetWindowRect(new WindowInteropHelper(_pet).Handle, out var position);
                if (position.Left != _smokePosition.Left || position.Top != _smokePosition.Top) throw new Exception("Cat moved away during interaction");
                Capture(_pet, "cat-treat-preview.png"); InteractWithPet(PetInteraction.Play);
            }
            if (_smokeStep == 36) { Capture(_pet, "cat-play-preview.png"); InteractWithPet(PetInteraction.Pet); }
            if (_smokeStep == 37) { Capture(_pet, "cat-pet-preview.png"); InteractWithPet(PetInteraction.Nap); }
            if (_smokeStep == 39)
            {
                if (((IInteractivePetVisual)_pet.Model).CurrentInteraction != PetInteraction.Nap) throw new Exception("Requested nap did not stay active");
                Capture(_pet, "cat-nap-preview.png"); InteractWithPet(PetInteraction.Wake);
            }
            if (_smokeStep == 40) Capture(_pet, "cat-wake-preview.png");
            if (_smokeStep == 41)
            {
                OpenSettings();
                FindButton(_settingsWindow!, "Play with yarn").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (((IInteractivePetVisual)_pet.Model).CurrentInteraction != PetInteraction.Play) throw new Exception("Settings interaction action failed");
                ((IInteractivePetVisual)_pet.Model).CancelInteraction();
                if (((IInteractivePetVisual)_pet.Model).IsInteracting) throw new Exception("Cancel reaction failed");
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test.log"), "PASS: treat through context menu; stationary interaction; yarn, petting, nap and wake previews; settings action; cancel reaction.\n");
                FindPetPicker(_settingsWindow!).SelectedValue = "owl";
                FindButton(_settingsWindow!, "Save").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (_settings.PetId != "owl" || _pet.Model is not Owl2D) throw new Exception("Owl selection failed");
                Capture(_settingsWindow!, "owl-dashboard-preview.png");
                _settingsWindow!.Close(); _paused = false; _ground.Reset();
            }
            if (_smokeStep == 43) { Capture(_pet, "owl-hopping-preview.png"); InteractWithPet(PetInteraction.Pet); }
            if (_smokeStep == 44) { Capture(_pet, "owl-pet-preview.png"); InteractWithPet(PetInteraction.Treat); }
            if (_smokeStep == 45) { Capture(_pet, "owl-treat-preview.png"); InteractWithPet(PetInteraction.Play); }
            if (_smokeStep == 46) { Capture(_pet, "owl-play-preview.png"); InteractWithPet(PetInteraction.Nap); }
            if (_smokeStep == 48) { Capture(_pet, "owl-sleep-preview.png"); InteractWithPet(PetInteraction.Wake); }
            if (_smokeStep == 50) ShowBreak(BreakKind.Water);
            if (_smokeStep == 51)
            {
                if (_overlay == null || FindModel(_overlay) is not Owl2D || _nextAudio != null) throw new Exception("Owl reminder model/audio incorrect");
                Capture(_overlay, "owl-reminder-preview.png");
                FindButton(_overlay, "Snooze 5 min").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                OpenSettings();
                var movement = _settings.Movement; var style = _settings.Style;
                FindButton(_settingsWindow!, "25 min").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (!_focus.IsActive || movement != _settings.Movement || style != _settings.Style) throw new Exception("Focus changed saved preferences");
                GetWindowRect(new WindowInteropHelper(_pet).Handle, out _smokePosition);
                FindButton(_settingsWindow!, "Focus").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Capture(_settingsWindow!, "focus-dashboard-preview.png");
            }
            if (_smokeStep == 52)
            {
                GetWindowRect(new WindowInteropHelper(_pet).Handle, out var position);
                if (position.Left != _smokePosition.Left || position.Top != _smokePosition.Top) throw new Exception("Focus pet did not settle");
                SetSessionLocked(true); SetPowerSuspended(true); SetPowerSuspended(false);
                if (!_focus.IsPaused || _pet.IsVisible || !_suspended) throw new Exception("Power resume bypassed session lock");
                SetSessionLocked(false);
                if (_focus.IsPaused || !_pet.IsVisible) throw new Exception("Unlocked focus did not resume");
                ToggleClickThrough();
                if (!_settings.ClickThrough || (GetWindowLong(new WindowInteropHelper(_pet).Handle, -20) & 0x20) == 0) throw new Exception("Click-through style not applied");
            }
            if (_smokeStep == 53)
            {
                ToggleClickThrough();
                if (_settings.ClickThrough || (GetWindowLong(new WindowInteropHelper(_pet).Handle, -20) & 0x20) != 0) throw new Exception("Tray did not restore interaction");
                FindButton(_settingsWindow!, "End session").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (_focus.IsActive) throw new Exception("End focus button failed");
                ShowReminder(new(null, null, "A little win.", "Your focus session is complete.", ReminderStyle.Popup));
                bool snoozePresent = false;
                try { FindButton(_overlay!, "Snooze 5 min"); snoozePresent = true; } catch (InvalidOperationException) { }
                if (snoozePresent) throw new Exception("Informational completion offers a nonfunctional snooze");
                FindButton(_overlay!, "Done").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test.log"), "PASS: owl selection/hopping/reactions/sleep and reminder; focus parking and unchanged preferences; combined lock/suspend recovery; native click-through and tray restoration; focus controls and completion popup.\n");
                Quit();
            }
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test.log"), "FAIL: " + ex);
            Quit();
        }
    }
    private static ComboBox FindPetPicker(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is ComboBox box && box.SelectedValuePath == "Id") return box;
            try { return FindPetPicker(child); } catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException("Pet picker missing");
    }

    private void AssertPetClearance()
    {
        // Rotation alone must neither resize the host nor clip rendered pixels at its edges.
        if (Math.Abs(_pet.ActualWidth - _pet.ActualHeight) > 1) throw new Exception("Rotation host must remain square");
        int width = (int)_pet.ActualWidth, height = (int)_pet.ActualHeight;
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(_pet);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if ((x < 2 || y < 2 || x >= width - 2 || y >= height - 2) && pixels[(y * width + x) * 4 + 3] > 8)
                    throw new Exception("Rotated dragon reaches the host boundary");
    }

    private static Button FindButton(DependencyObject parent, string title)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Button button && Equals(button.Content, title)) return button;
            try { return FindButton(child, title); } catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException("Button missing: " + title);
    }
    private static IPetVisual FindModel(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is IPetVisual model) return model;
            try { return FindModel(child); } catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException("Dragon model missing");
    }
    private static void Capture(Window window, string filename)
    {
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(AppContext.BaseDirectory, filename)); png.Save(file);
    }
}
