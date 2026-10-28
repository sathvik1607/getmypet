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

internal sealed class DragonPet : IPetDefinition
{
    public string Id => "dragon";
    public string DisplayName => "Dragon";
    public string IntroAudio => "Pets/Dragon/Assets/Audio/roar.wav";
    public string ReminderAudio(BreakKind kind) => kind == BreakKind.Water
        ? "Pets/Dragon/Assets/Audio/water.wav" : "Pets/Dragon/Assets/Audio/stretch.wav";
    public PetPresentation CreatePresentation(double width, bool reminder) => Create(width, reminder);

    private static PetPresentation Create(double width, bool fire)
    {
        var grid = new Grid { Width = width, Height = width, IsHitTestVisible = false };
        var image = new DragonModel { RenderTransformOrigin = new Point(.5, .5) };
        var movement = new TransformGroup();
        var rotate = new RotateTransform();
        var translate = new TranslateTransform();
        movement.Children.Add(rotate); movement.Children.Add(translate); image.RenderTransform = movement;
        rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-2, 2, TimeSpan.FromSeconds(2.4)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
        translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-4, 4, TimeSpan.FromSeconds(1.8)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
        grid.Children.Add(image);
        if (fire)
        {
            var fireLayer = new Canvas { Width = width, Height = width, IsHitTestVisible = false };
            var flame = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M 0,50 C 25,30 40,15 65,28 L 100,0 L 90,35 L 150,20 L 120,60 L 160,80 L 90,74 L 110,110 C 50,95 40,65 0,50 Z"),
                Fill = new LinearGradientBrush(Color.FromRgb(255, 243, 137), Color.FromRgb(255, 79, 12), 0),
                Width = width * .23, Height = width * .15, Stretch = Stretch.Fill,
                RenderTransformOrigin = new Point(0, .5)
            };
            Canvas.SetLeft(flame, width * .80); Canvas.SetTop(flame, width * .53);
            var scale = new ScaleTransform(1, 1);
            flame.RenderTransform = scale;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.6, 1.15, TimeSpan.FromSeconds(.27)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.7, 1.2, TimeSpan.FromSeconds(.36)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
            flame.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(.75, 1, TimeSpan.FromSeconds(.23)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
            fireLayer.Children.Add(flame);
            var core = new System.Windows.Shapes.Path
            {
                Data = flame.Data, Fill = new SolidColorBrush(Color.FromRgb(255, 245, 190)),
                Width = width * .15, Height = width * .075, Stretch = Stretch.Fill,
                RenderTransformOrigin = new Point(0, .5), RenderTransform = scale
            };
            Canvas.SetLeft(core, width * .80); Canvas.SetTop(core, width * .565);
            fireLayer.Children.Add(core); grid.Children.Add(fireLayer);
        }
        return new PetPresentation(grid, image);
    }
}
