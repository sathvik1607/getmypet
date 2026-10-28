using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Petcompanion;

// Export the evaluated scene as a portable, colored static GLB pose.
internal static class ModelExport
{
    public static void Save(Model3D root, string path)
    {
        using var buffer = new MemoryStream(); using var binary = new BinaryWriter(buffer);
        var views = new List<object>(); var accessors = new List<object>();
        var primitives = new List<object>(); var materials = new List<object>();
        int View(long offset, int target)
        {
            int index = views.Count; views.Add(new { buffer = 0, byteOffset = offset, byteLength = buffer.Position - offset, target }); return index;
        }
        void Visit(Model3D model, Matrix3D parent)
        {
            var matrix = model.Transform.Value; matrix.Append(parent);
            if (model is Model3DGroup group) { foreach (var child in group.Children) Visit(child, matrix); return; }
            if (model is not GeometryModel3D geometry || geometry.Geometry is not MeshGeometry3D mesh || mesh.Positions.Count == 0) return;
            var points = mesh.Positions.Select(p => matrix.Transform(p)).ToArray();
            // Calculate normals after transforming, including non-uniform bone scaling.
            var normals = new Vector3D[points.Length];
            for (int i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
            {
                int a = mesh.TriangleIndices[i], b = mesh.TriangleIndices[i + 1], c = mesh.TriangleIndices[i + 2];
                var n = Vector3D.CrossProduct(points[b] - points[a], points[c] - points[a]);
                normals[a] += n; normals[b] += n; normals[c] += n;
            }
            long offset = buffer.Position;
            foreach (var p in points) { binary.Write((float)p.X); binary.Write((float)p.Y); binary.Write((float)p.Z); }
            int position = accessors.Count;
            accessors.Add(new { bufferView = View(offset, 34962), componentType = 5126, count = points.Length, type = "VEC3",
                min = new[] { (float)points.Min(p => p.X), (float)points.Min(p => p.Y), (float)points.Min(p => p.Z) },
                max = new[] { (float)points.Max(p => p.X), (float)points.Max(p => p.Y), (float)points.Max(p => p.Z) } });
            offset = buffer.Position;
            foreach (var normal in normals)
            {
                var n = normal; if (n.LengthSquared < 1e-16) n = new Vector3D(0, 1, 0); else n.Normalize();
                binary.Write((float)n.X); binary.Write((float)n.Y); binary.Write((float)n.Z);
            }
            int normalAccessor = accessors.Count;
            accessors.Add(new { bufferView = View(offset, 34962), componentType = 5126, count = normals.Length, type = "VEC3" });
            offset = buffer.Position;
            foreach (int index in mesh.TriangleIndices) binary.Write((uint)index);
            int indices = accessors.Count;
            accessors.Add(new { bufferView = View(offset, 34963), componentType = 5125, count = mesh.TriangleIndices.Count, type = "SCALAR" });
            var diffuse = geometry.Material as DiffuseMaterial ?? (geometry.Material as MaterialGroup)?.Children.OfType<DiffuseMaterial>().FirstOrDefault();
            Color color = (diffuse?.Brush as SolidColorBrush)?.Color ?? Colors.Green;
            int material = materials.Count;
            materials.Add(new { name = "Dragon material " + material, doubleSided = true,
                pbrMetallicRoughness = new { baseColorFactor = new[] { color.ScR, color.ScG, color.ScB, 1f }, metallicFactor = 0, roughnessFactor = .65 } });
            primitives.Add(new { attributes = new { POSITION = position, NORMAL = normalAccessor }, indices, material, mode = 4 });
        }
        Visit(root, Matrix3D.Identity);
        var document = new { asset = new { version = "2.0", generator = "Petcompanion static-pose exporter" },
            scene = 0, scenes = new[] { new { nodes = new[] { 0 } } }, nodes = new[] { new { name = "Stylized dragon - static pose", mesh = 0 } },
            meshes = new[] { new { name = "Dragon", primitives } }, materials, accessors, bufferViews = views,
            buffers = new[] { new { byteLength = buffer.Length } } };
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(document);
        int jsonLength = (json.Length + 3) & ~3;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var output = new BinaryWriter(File.Create(path));
        output.Write(0x46546C67u); output.Write(2u); output.Write((uint)(12 + 8 + jsonLength + 8 + buffer.Length));
        output.Write(jsonLength); output.Write(0x4E4F534Au); output.Write(json);
        for (int i = json.Length; i < jsonLength; i++) output.Write((byte)32);
        output.Write((uint)buffer.Length); output.Write(0x004E4942u); output.Write(buffer.ToArray());
    }
}
