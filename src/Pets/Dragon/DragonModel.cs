using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Petcompanion;

// Live geometry: a curved spine, a tube skin, articulated legs, and a turning head.
// No sprite frames or image textures are used to render the dragon.
internal sealed class DragonModel : Grid, IPetVisual
{
    private const int Rings = 64, Sides = 12;
    private readonly MeshGeometry3D _greenSkin = new(), _bellySkin = new(), _bellyRibs = new();
    private readonly Point3D[] _spine = new Point3D[Rings];
    private readonly Model3DGroup _head = new(), _dragon = new();
    private readonly Bone[,] _legs = new Bone[4, 5];
    private readonly Bone[] _spikes = new Bone[16];
    private readonly AxisAngleRotation3D _turn = new(new Vector3D(0, 1, 0), 0);
    private readonly AxisAngleRotation3D _bank = new(new Vector3D(0, 0, 1), 0);
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly Stopwatch _time = Stopwatch.StartNew();
    public bool FacingRight { get; set; } = true;
    public double Bank { get; set; }
    public double TravelHeadingDegrees { get; set; }
    private readonly RotateTransform _flightHeading = new();
    public int PoseUpdates { get; private set; }
    public bool Paused { get; set; }
    public OrthographicCamera Camera { get; }
    public Model3DGroup Geometry => _dragon;
    private static readonly Material Green = Material(Color.FromRgb(43, 158, 94));
    private static readonly Material DarkGreen = Material(Color.FromRgb(15, 78, 44));
    private static readonly Material Gold = Material(Color.FromRgb(239, 192, 98));
    private static readonly Material BellyLine = Material(Color.FromRgb(177, 122, 43));
    private static readonly Material Horn = Material(Color.FromRgb(159, 102, 49));
    private static readonly Material Ivory = Material(Color.FromRgb(249, 237, 195));
    private static readonly MeshGeometry3D SphereMesh = Sphere();
    private static readonly MeshGeometry3D CylinderMesh = Cylinder();

    public DragonModel()
    {
        IsHitTestVisible = false;
        Camera = new OrthographicCamera(new Point3D(0, 2.2, 8), new Vector3D(0, -1.8, -8), new Vector3D(0, 1, 0), 6.6);
        var view = new Viewport3D
        {
            Camera = Camera,
            ClipToBounds = false,
            RenderTransform = _flightHeading, RenderTransformOrigin = new Point(.5, .5)
        };
        Children.Add(view);
        var scene = new Model3DGroup();
        scene.Children.Add(new AmbientLight(Color.FromRgb(105, 115, 120)));
        scene.Children.Add(new DirectionalLight(Color.FromRgb(255, 244, 217), new Vector3D(-2, -3, -4)));
        scene.Children.Add(new DirectionalLight(Color.FromRgb(109, 204, 179), new Vector3D(2, 1, 3)));
        _dragon.Children.Add(new GeometryModel3D(_greenSkin, Green) { BackMaterial = Green });
        _dragon.Children.Add(new GeometryModel3D(_bellySkin, Gold) { BackMaterial = Gold });
        _dragon.Children.Add(new GeometryModel3D(_bellyRibs, BellyLine) { BackMaterial = BellyLine });
        BuildIndices(); BuildHead(); _dragon.Children.Add(_head);
        for (int leg = 0; leg < 4; leg++)
            for (int bone = 0; bone < 5; bone++) _legs[leg, bone] = new Bone(_dragon, bone < 2 ? Green : Ivory);
        for (int i = 0; i < _spikes.Length; i++) _spikes[i] = new Bone(_dragon, DarkGreen);
        var rotation = new Transform3DGroup();
        rotation.Children.Add(new RotateTransform3D(_turn)); rotation.Children.Add(new RotateTransform3D(_bank));
        _dragon.Transform = rotation; scene.Children.Add(_dragon);
        view.Children.Add(new ModelVisual3D { Content = scene });
        Update();
        _timer.Tick += (_, _) => { if (!Paused) Update(); };
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
        IsVisibleChanged += (_, _) => { if (IsVisible) _timer.Start(); else _timer.Stop(); };
    }

    private void BuildIndices()
    {
        for (int ring = 0; ring < Rings - 1; ring++)
            for (int side = 0; side < Sides; side++)
            {
                var mesh = side >= 7 && side <= 9 ? (ring % 4 == 0 ? _bellyRibs : _bellySkin) : _greenSkin;
                int a = ring * Sides + side, b = ring * Sides + (side + 1) % Sides;
                mesh.TriangleIndices.Add(a); mesh.TriangleIndices.Add(b); mesh.TriangleIndices.Add(a + Sides);
                mesh.TriangleIndices.Add(b); mesh.TriangleIndices.Add(b + Sides); mesh.TriangleIndices.Add(a + Sides);
            }
    }

    private void Update()
    {
        double time = _time.Elapsed.TotalSeconds;
        for (int ring = 0; ring < Rings; ring++)
        {
            double u = ring / (double)(Rings - 1);
            double wave = time * 2.1 - u * Math.PI * 3.4;
            _spine[ring] = new Point3D(1.75 - u * 4.4,
                Math.Sin(wave) * (.12 + .54 * Math.Sin(u * Math.PI * .85)),
                Math.Cos(time * 1.5 - u * Math.PI * 2.8) * .34 * Math.Sin(u * Math.PI));
        }
        var skin = new Point3DCollection(Rings * Sides);
        for (int ring = 0; ring < Rings; ring++)
        {
            double u = ring / (double)(Rings - 1);
            Vector3D tangent = _spine[Math.Min(Rings - 1, ring + 1)] - _spine[Math.Max(0, ring - 1)]; tangent.Normalize();
            Vector3D normal = Vector3D.CrossProduct(tangent, new Vector3D(0, 0, 1)); normal.Normalize();
            Vector3D side = Vector3D.CrossProduct(tangent, normal); side.Normalize();
            double radius = .235 * Math.Pow(1 - u, .55) + .008;
            // Subtle rings suggest scales without textures or hundreds of separate objects.
            radius *= ring % 3 == 0 ? 1.04 : 1;
            for (int segment = 0; segment < Sides; segment++)
            {
                double angle = segment * Math.PI * 2 / Sides;
                skin.Add(_spine[ring] + normal * (Math.Cos(angle) * radius) + side * (Math.Sin(angle) * radius));
            }
        }
        skin.Freeze(); _greenSkin.Positions = skin; _bellySkin.Positions = skin; _bellyRibs.Positions = skin;
        Vector3D forward = _spine[0] - _spine[2]; forward.Normalize();
        _head.Transform = Orient(new Vector3D(1, 0, 0), forward, _spine[0]);
        for (int leg = 0; leg < 4; leg++)
        {
            int ring = leg < 2 ? 15 : 36; double side = leg % 2 == 0 ? 1 : -1;
            double stride = Math.Sin(time * 2.4 + leg * Math.PI * .8);
            Point3D hip = _spine[ring] + new Vector3D(0, -.08, side * .16);
            Point3D elbow = hip + new Vector3D(-.15 + stride * .12, -.26, side * .29);
            Point3D foot = elbow + new Vector3D(.20 + stride * .12, -.16 + stride * .07, side * .18);
            _legs[leg, 0].Place(hip, elbow, .075); _legs[leg, 1].Place(elbow, foot, .055);
            for (int claw = 0; claw < 3; claw++)
                _legs[leg, claw + 2].Place(foot + new Vector3D(0, 0, (claw - 1) * .055), foot + new Vector3D(.14, -.05, (claw - 1) * .075), .018);
        }
        for (int i = 0; i < _spikes.Length; i++)
        {
            int ring = 3 + i * 3;
            double radius = .235 * Math.Pow(1 - ring / (double)Rings, .55);
            Point3D start = _spine[ring] + new Vector3D(0, radius * .8, 0);
            _spikes[i].Place(start, start + new Vector3D(-.13, .18 * (1 - i / 20d), 0), .035);
        }
        _turn.Angle += ((FacingRight ? 0 : 180) - _turn.Angle) * .07;
        _bank.Angle += (Math.Clamp(Bank, -28, 28) - _bank.Angle) * .08;
        double relativeHeading = PetHeading.Relative(TravelHeadingDegrees, FacingRight);
        _flightHeading.Angle += PetHeading.Delta(_flightHeading.Angle, relativeHeading) * .18;
        PoseUpdates++;
    }

    private void BuildHead()
    {
        Ellipsoid(_head, new Point3D(0, .02, 0), .43, .29, .28, Green);
        Ellipsoid(_head, new Point3D(.34, -.07, 0), .40, .18, .24, Green);
        Ellipsoid(_head, new Point3D(.34, -.19, 0), .32, .055, .21, Material(Color.FromRgb(78, 16, 23)));
        Ellipsoid(_head, new Point3D(.34, -.255, 0), .33, .065, .21, Gold);
        var red = Material(Color.FromRgb(247, 32, 34));
        for (int side = -1; side <= 1; side += 2)
        {
            Ellipsoid(_head, new Point3D(.12, .13, side * .245), .125, .066, .032, red);
            new Bone(_head, DarkGreen).Place(new Point3D(-.045, .225, side * .25), new Point3D(.25, .19, side * .265), .040);
            Ellipsoid(_head, new Point3D(.17, .13, side * .279), .023, .050, .014, DarkGreen);
            Ellipsoid(_head, new Point3D(.185, .151, side * .291), .010, .015, .007, Ivory);
            Fin(_head, new Point3D(-.17, .09, side * .25), new Point3D(-.60, .27, side * .34),
                new Point3D(-.30, -.13, side * .33), Gold);
            Ellipsoid(_head, new Point3D(.58, .025, side * .12), .045, .025, .025, DarkGreen);
            var root = new Point3D(-.17, .24, side * .19);
            var tip = root + new Vector3D(-.23, .65, side * .12);
            new Bone(_head, Horn).Place(root, tip, .048);
            new Bone(_head, Horn).Place(root + new Vector3D(-.10, .30, side * .05), root + new Vector3D(.12, .47, side * .13), .03);
            new Bone(_head, Horn).Place(root + new Vector3D(-.17, .46, side * .07), root + new Vector3D(-.42, .58, side * .18), .024);
            Point3D previous = new(.48, -.07, side * .23);
            for (int i = 1; i <= 10; i++)
            {
                double t = i / 10d;
                Point3D next = new(.48 - t * .9, -.07 + Math.Sin(t * Math.PI * 1.6) * .2, side * (.23 + t * .5));
                new Bone(_head, Gold).Place(previous, next, .015 * (1 - t * .65)); previous = next;
            }
            for (int beard = 0; beard < 3; beard++)
                new Bone(_head, Gold).Place(new Point3D(.13 - beard * .08, -.24, side * .09),
                    new Point3D(-.08 - beard * .10, -.50 + beard * .03, side * (.13 + beard * .04)), .018);
            for (int tooth = 0; tooth < 3; tooth++)
                new Bone(_head, Ivory).Place(new Point3D(.22 + tooth * .15, -.15, side * .175), new Point3D(.24 + tooth * .15, -.23, side * .16), .024);
            for (int spike = 0; spike < 5; spike++)
                new Bone(_head, DarkGreen).Place(new Point3D(-.20, .05 + spike * .04, side * .18), new Point3D(-.65, .12 + spike * .075, side * (.22 + spike * .025)), .045);
        }
    }

    private static void Fin(Model3DGroup group, Point3D a, Point3D b, Point3D c, Material material)
    {
        var mesh = new MeshGeometry3D { Positions = new Point3DCollection { a, b, c },
            TriangleIndices = new Int32Collection { 0, 1, 2 } };
        mesh.Freeze(); group.Children.Add(new GeometryModel3D(mesh, material) { BackMaterial = material });
    }

    private static Material Material(Color color)
    {
        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
        material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromRgb(155, 180, 155)), 30));
        material.Freeze(); return material;
    }
    private static void Ellipsoid(Model3DGroup group, Point3D point, double x, double y, double z, Material material)
    {
        var transform = new Transform3DGroup(); transform.Children.Add(new ScaleTransform3D(x, y, z));
        transform.Children.Add(new TranslateTransform3D(point.X, point.Y, point.Z));
        group.Children.Add(new GeometryModel3D(SphereMesh, material) { Transform = transform });
    }
    private static Transform3D Orient(Vector3D from, Vector3D to, Point3D point)
    {
        var axis = Vector3D.CrossProduct(from, to);
        double angle = Vector3D.AngleBetween(from, to);
        if (axis.LengthSquared < .00001) axis = new Vector3D(0, 0, 1);
        var transform = new Transform3DGroup();
        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(axis, angle)));
        transform.Children.Add(new TranslateTransform3D(point.X, point.Y, point.Z)); return transform;
    }
    private sealed class Bone
    {
        private readonly GeometryModel3D _model;
        public Bone(Model3DGroup group, Material material) { _model = new GeometryModel3D(CylinderMesh, material) { BackMaterial = material }; group.Children.Add(_model); }
        public void Place(Point3D start, Point3D end, double radius)
        {
            Vector3D direction = end - start; double length = direction.Length; direction.Normalize();
            var transform = new Transform3DGroup(); transform.Children.Add(new ScaleTransform3D(radius, length, radius));
            transform.Children.Add(Orient(new Vector3D(0, 1, 0), direction, start)); _model.Transform = transform;
        }
    }
    private static MeshGeometry3D Sphere()
    {
        var mesh = new MeshGeometry3D(); const int rows = 12, cols = 18;
        for (int row = 0; row <= rows; row++)
            for (int col = 0; col <= cols; col++)
            {
                double phi = row * Math.PI / rows, theta = col * 2 * Math.PI / cols;
                var p = new Point3D(Math.Sin(phi) * Math.Cos(theta), Math.Cos(phi), Math.Sin(phi) * Math.Sin(theta));
                mesh.Positions.Add(p); mesh.Normals.Add(new Vector3D(p.X, p.Y, p.Z));
            }
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                int a = row * (cols + 1) + col, b = a + cols + 1;
                foreach (int index in new[] { a, a + 1, b, a + 1, b + 1, b }) mesh.TriangleIndices.Add(index);
            }
        mesh.Freeze(); return mesh;
    }
    private static MeshGeometry3D Cylinder()
    {
        var mesh = new MeshGeometry3D(); const int sides = 8;
        for (int i = 0; i < sides; i++)
        {
            double angle = i * Math.PI * 2 / sides;
            mesh.Positions.Add(new Point3D(Math.Cos(angle), 0, Math.Sin(angle)));
            mesh.Positions.Add(new Point3D(Math.Cos(angle) * .65, 1, Math.Sin(angle) * .65));
        }
        for (int i = 0; i < sides; i++)
        {
            int a = i * 2, b = ((i + 1) % sides) * 2;
            foreach (int index in new[] { a, b, a + 1, b, b + 1, a + 1 }) mesh.TriangleIndices.Add(index);
        }
        mesh.Freeze(); return mesh;
    }
}
