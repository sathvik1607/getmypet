using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Petcompanion;

// Articulated procedural 3D cat. Every part is live geometry, with no sprite swapping.
internal sealed class CatModel : Grid, IGroundPetVisual
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly Model3DGroup _cat = new(), _body = new(), _head = new();
    private readonly Model3DGroup[] _legs = new Model3DGroup[4];
    private readonly GeometryModel3D[] _tail = new GeometryModel3D[10];
    private readonly ScaleTransform3D[] _eyes = new ScaleTransform3D[2];
    private readonly AxisAngleRotation3D _yaw = new(new Vector3D(0, 1, 0), 0);
    private double _time, _sleep, _sit, _groom;
    public bool FacingRight { get; set; } = true;
    public double Bank { get; set; }
    public double TravelHeadingDegrees { get; set; }
    public int PoseUpdates { get; private set; }
    public PetActivity Activity { get; set; } = PetActivity.Walking;
    public bool Reminder { get; set; }
    private static readonly MeshGeometry3D Round = MakeRound();
    private static readonly Material Orange = Paint(224, 139, 61), Stripe = Paint(164, 86, 36), Cream = Paint(255, 224, 175),
        Pink = Paint(207, 125, 130), Dark = Paint(58, 42, 40), Eye = Paint(111, 142, 87);

    public CatModel()
    {
        IsHitTestVisible = false;
        var viewport = new Viewport3D { Camera = new OrthographicCamera(new Point3D(3.5, 3.5, 6),
            new Vector3D(-3.5, -1.6, -6), new Vector3D(0, 1, 0), 4.4) };
        var scene = new Model3DGroup();
        scene.Children.Add(new AmbientLight(Color.FromRgb(145, 137, 129)));
        scene.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-2, -3, -4)));
        _cat.Transform = new RotateTransform3D(_yaw, 0, .8, 0);
        scene.Children.Add(_cat); viewport.Children.Add(new ModelVisual3D { Content = scene }); Children.Add(viewport);
        _cat.Children.Add(_body); _cat.Children.Add(_head);
        Ball(_body, -.22, .78, 0, .72, .46, .39, Orange);
        Ball(_body, .24, .69, .13, .39, .28, .28, Cream);
        Ball(_body, .30, 1.02, 0, .32, .35, .30, Orange);
        for (int i = 0; i < 3; i++)
        {
            Ball(_body, -.62 + i * .28, 1.11, 0, .055, .07, .30, Stripe);
            Ball(_body, -.55 + i * .28, .85, .365, .055, .22, .035, Stripe);
        }
        Ball(_head, .68, 1.27, 0, .51, .45, .42, Orange);
        Ball(_head, 1.02, 1.09, .16, .20, .15, .19, Cream);
        Ball(_head, 1.02, 1.09, -.16, .20, .15, .19, Cream);
        Ball(_head, 1.18, 1.17, 0, .06, .045, .07, Pink);
        for (int side = -1; side <= 1; side += 2)
        {
            Ear(_head, .55, 1.50, side * .29, Orange, 1);
            InnerEar(_head, .55, 1.50, side * .29);
            int index = side == -1 ? 0 : 1;
            var eyeGroup = new Model3DGroup();
            Ball(eyeGroup, 0, 0, 0, .055, .105, .11, Eye);
            Ball(eyeGroup, .044, 0, .018 * side, .027, .085, .031, Dark);
            Ball(eyeGroup, .065, .033, .026 * side, .014, .022, .017, Cream);
            _eyes[index] = new ScaleTransform3D(1, 1, 1);
            var eyeTransform = new Transform3DGroup(); eyeTransform.Children.Add(_eyes[index]);
            eyeTransform.Children.Add(new TranslateTransform3D(1.04, 1.38, side * .27)); eyeGroup.Transform = eyeTransform;
            _head.Children.Add(eyeGroup);
            for (int whisker = 0; whisker < 3; whisker++)
                Segment(_head, new Point3D(1.04, 1.08, side * .20),
                    new Point3D(1.10, 1.04 + whisker * .055, side * .62), .009, Cream);
            Ball(_head, .69, 1.61, side * .12, .04, .085, .055, Stripe);
            Segment(_head, new Point3D(1.185, 1.12, 0), new Point3D(1.16, 1.065, side * .055), .009, Dark);
            Segment(_head, new Point3D(1.16, 1.065, side * .055), new Point3D(1.11, 1.075, side * .09), .009, Dark);
        }
        for (int i = 0; i < 4; i++)
        {
            _legs[i] = new Model3DGroup(); _cat.Children.Add(_legs[i]);
            Ball(_legs[i], 0, -.12, 0, .14, .23, .14, Orange);
            Ball(_legs[i], .045, -.32, .02, .18, .105, .16, Cream);
        }
        for (int i = 0; i < _tail.Length; i++) { _tail[i] = new GeometryModel3D(Round, i % 3 == 0 ? Stripe : Orange); _cat.Children.Add(_tail[i]); }
        UpdatePose(0);
        _timer.Tick += (_, _) => UpdatePose(.033);
        Loaded += (_, _) => _timer.Start(); Unloaded += (_, _) => _timer.Stop();
        IsVisibleChanged += (_, _) => { if (IsVisible) _timer.Start(); else _timer.Stop(); };
    }

    internal void UpdatePose(double seconds)
    {
        _time += Math.Max(0, seconds);
        _sleep += ((Activity == PetActivity.Sleeping ? 1 : 0) - _sleep) * .12;
        _sit += ((Activity is PetActivity.Sitting or PetActivity.Grooming ? 1 : 0) - _sit) * .12;
        _groom += ((Activity == PetActivity.Grooming ? 1 : 0) - _groom) * .12;
        double breathing = Math.Sin(_time * 1.7) * .012;
        _body.Transform = Transform(.9 + .1 * (1 - _sit), 1 - _sleep * .32, 1,
            new Vector3D(0, 0, 1), _sit * 45, .12 * _sit, -.10 * _sleep + breathing, 0, -.22, .78, 0);
        double nod = _groom * (8 + 12 * Math.Sin(_time * 4));
        _head.Transform = Transform(1, 1, 1, new Vector3D(0, 0, 1), -_sleep * 17 - nod,
            -_sleep * .13 - _sit * .20, -_sleep * .58 + _sit * .05 + breathing, 0, .68, 1.27, 0);
        double stride = Activity == PetActivity.Walking ? Math.Sin(_time * 7) * 27 : 0;
        for (int i = 0; i < 4; i++)
        {
            bool front = i < 2; double side = i % 2 == 0 ? -1 : 1;
            double angle = stride * (i is 0 or 3 ? 1 : -1);
            double wave = front && side > 0 ? _groom * (65 + 14 * Math.Sin(_time * 5)) + (Reminder ? 18 + Math.Sin(_time * 3) * 12 : 0) : 0;
            _legs[i].Transform = Transform(1, 1 - _sleep * .65, 1, new Vector3D(0, 0, 1), angle + wave,
                (front ? .32 : -.67) + (front ? 0 : _sit * .22), .45 - _sleep * .19, side * .25);
        }
        for (int i = 0; i < _tail.Length; i++)
        {
            double t = i / (double)(_tail.Length - 1), sway = Math.Sin(_time * 1.8 + t * 2) * .10;
            double x = -.83 - .50 * Math.Sin(t * 2.3), y = .73 + .60 * t, z = sway;
            x += _sleep * (t * 1.8); y -= _sleep * (.42 + .48 * t); z += _sleep * Math.Sin(t * Math.PI) * .50;
            _tail[i].Transform = Transform(.105 - .038 * t, .14, .105 - .038 * t, new Vector3D(0, 0, 1), 20,
                x, y, z);
        }
        double open = _sleep > .5 || _time % 5 < .16 ? .12 : 1;
        foreach (var eye in _eyes) eye.ScaleY += (open - eye.ScaleY) * .35;
        _yaw.Angle += ((FacingRight ? 0 : -155) - _yaw.Angle) * .12;
        PoseUpdates++;
    }

    private static Material Paint(byte r, byte g, byte b) { var m = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(r, g, b))); m.Freeze(); return m; }
    private static Transform3D Transform(double sx, double sy, double sz, Vector3D axis, double angle,
        double x, double y, double z, double cx = 0, double cy = 0, double cz = 0)
    {
        var t = new Transform3DGroup(); t.Children.Add(new ScaleTransform3D(sx, sy, sz));
        t.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(axis, angle), cx, cy, cz));
        t.Children.Add(new TranslateTransform3D(x, y, z)); return t;
    }
    private static void Ball(Model3DGroup group, double x, double y, double z, double sx, double sy, double sz, Material material) =>
        group.Children.Add(new GeometryModel3D(Round, material) { Transform = Transform(sx, sy, sz, new Vector3D(0, 1, 0), 0, x, y, z) });
    private static void Ear(Model3DGroup group, double x, double y, double z, Material material, double size)
    {
        var mesh = new MeshGeometry3D { Positions = new Point3DCollection { new(x - .17 * size, y, z - .14 * size),
            new(x + .17 * size, y, z - .14 * size), new(x + .04 * size, y + .36 * size, z),
            new(x - .17 * size, y, z + .14 * size), new(x + .17 * size, y, z + .14 * size) },
            TriangleIndices = new Int32Collection { 0, 2, 1, 3, 4, 2, 0, 3, 2, 1, 2, 4, 0, 1, 4, 0, 4, 3 } };
        mesh.Freeze(); group.Children.Add(new GeometryModel3D(mesh, material) { BackMaterial = material });
    }
    private static void InnerEar(Model3DGroup group, double x, double y, double z)
    {
        foreach (int side in new[] { -1, 1 })
        {
            var mesh = new MeshGeometry3D { Positions = new Point3DCollection {
                new(x - .105, y + .045, z + side * .14), new(x + .12, y + .045, z + side * .14),
                new(x + .035, y + .28, z + side * .055) }, TriangleIndices = new Int32Collection { 0, 1, 2 } };
            mesh.Freeze(); group.Children.Add(new GeometryModel3D(mesh, Pink) { BackMaterial = Pink });
        }
    }
    private static void Segment(Model3DGroup group, Point3D start, Point3D end, double radius, Material material)
    {
        var direction = end - start; var axis = Vector3D.CrossProduct(new Vector3D(0, 1, 0), direction);
        if (axis.LengthSquared < .00001) axis = new Vector3D(0, 0, 1);
        group.Children.Add(new GeometryModel3D(Round, material) { Transform = Transform(radius, direction.Length / 2, radius,
            axis, Vector3D.AngleBetween(new Vector3D(0, 1, 0), direction), (start.X + end.X) / 2, (start.Y + end.Y) / 2, (start.Z + end.Z) / 2) });
    }
    private static MeshGeometry3D MakeRound()
    {
        var mesh = new MeshGeometry3D(); const int rows = 8, columns = 12;
        for (int row = 0; row <= rows; row++) for (int col = 0; col <= columns; col++)
        {
            double phi = row * Math.PI / rows, theta = col * Math.PI * 2 / columns;
            mesh.Positions.Add(new Point3D(Math.Sin(phi) * Math.Cos(theta), Math.Cos(phi), Math.Sin(phi) * Math.Sin(theta)));
        }
        for (int row = 0; row < rows; row++) for (int col = 0; col < columns; col++)
        { int a = row * (columns + 1) + col, b = a + columns + 1; foreach (int index in new[] { a, a + 1, b, a + 1, b + 1, b }) mesh.TriangleIndices.Add(index); }
        mesh.Freeze(); return mesh;
    }
}