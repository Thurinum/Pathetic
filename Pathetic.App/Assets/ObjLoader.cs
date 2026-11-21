using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NeoVeldrid;
using Pathetic.App.Components;
using Silk.NET.Assimp;
using File = System.IO.File;
using Material = Pathetic.App.Components.Material;

namespace Pathetic.App.Assets;

public sealed record MeshAsset(MeshTriangle[] Triangles, Material Material);

public sealed class ObjLoader(ILogger<ObjLoader> logger)
{
    private const PostProcessSteps Steps = PostProcessSteps.Triangulate | PostProcessSteps.JoinIdenticalVertices;

    private readonly Dictionary<string, MeshAsset> _cache = [];

    public static string GetModelPath(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Path.IsPathRooted(name)
            ? name
            : Path.Combine(AppContext.BaseDirectory, "Assets", "Models", name);
    }

    public MeshAsset Load(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string filePath = Path.GetFullPath(GetModelPath(name));

        if (_cache.TryGetValue(filePath, out MeshAsset? cached))
            return cached;

        MeshAsset asset = Import(filePath);
        _cache.Add(filePath, asset);
        return asset;
    }

    private unsafe MeshAsset Import(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Model file '{filePath}' was not found.", filePath);

        using Assimp assimp = Assimp.GetApi();
        Scene* scene = assimp.ImportFile(filePath, (uint)Steps);

        if (scene == null)
            throw new InvalidOperationException($"Failed to import '{filePath}': {GetError(assimp)}");

        try
        {
            var triangles = new List<MeshTriangle>();
            CollectTriangles(scene, scene->MRootNode, Matrix4x4.Identity, triangles);

            Material material = Material.Default;

            if (ReadDiffuse(assimp, scene) is { } albedo)
                material = material with { Albedo = albedo };

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Loaded {TriangleCount} triangles from {Path}.", triangles.Count, filePath);

            return new MeshAsset([.. triangles], material);
        }
        finally
        {
            assimp.ReleaseImport(scene);
        }
    }

    private static unsafe void CollectTriangles(Scene* scene, Node* node, Matrix4x4 parentTransform, List<MeshTriangle> output)
    {
        if (node == null)
            return;

        Matrix4x4 local = node->MTransformation;
        Matrix4x4 transform = Matrix4x4.Transpose(local) * parentTransform;

        for (uint i = 0; i < node->MNumMeshes; i++)
            AppendMesh(scene->MMeshes[node->MMeshes[i]], transform, output);

        for (uint i = 0; i < node->MNumChildren; i++)
            CollectTriangles(scene, node->MChildren[i], transform, output);
    }

    private static unsafe void AppendMesh(Mesh* mesh, Matrix4x4 transform, List<MeshTriangle> output)
    {
        if (mesh->MNumVertices == 0 || mesh->MNumFaces == 0)
            return;

        Vector3[] normals = new Vector3[mesh->MNumVertices];

        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            Face face = mesh->MFaces[i];
            if (face.MNumIndices != 3)
                continue;

            int a = (int)face.MIndices[0];
            int b = (int)face.MIndices[1];
            int c = (int)face.MIndices[2];

            Vector3 normal = Vector3.Cross(
                mesh->MVertices[b] - mesh->MVertices[a],
                mesh->MVertices[c] - mesh->MVertices[a]);

            float length = normal.Length();
            if (length <= 1e-12f)
                continue;

            normal /= length;
            normals[a] += normal;
            normals[b] += normal;
            normals[c] += normal;
        }

        for (int i = 0; i < normals.Length; i++)
        {
            float length = normals[i].Length();
            if (length > 1e-12f)
                normals[i] /= length;
        }

        Matrix4x4 normalMatrix = Matrix4x4.Invert(transform, out Matrix4x4 inverse)
            ? Matrix4x4.Transpose(inverse)
            : Matrix4x4.Identity;

        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            Face face = mesh->MFaces[i];
            if (face.MNumIndices != 3)
                continue;

            int a = (int)face.MIndices[0];
            int b = (int)face.MIndices[1];
            int c = (int)face.MIndices[2];

            output.Add(new MeshTriangle(
                Vector3.Transform(mesh->MVertices[a], transform),
                Vector3.Transform(mesh->MVertices[b], transform),
                Vector3.Transform(mesh->MVertices[c], transform),
                TransformNormal(normals[a], normalMatrix),
                TransformNormal(normals[b], normalMatrix),
                TransformNormal(normals[c], normalMatrix)));
        }
    }

    private static Vector3 TransformNormal(Vector3 normal, Matrix4x4 normalMatrix)
    {
        Vector3 transformed = Vector3.TransformNormal(normal, normalMatrix);
        float length = transformed.Length();
        return length > 1e-12f ? transformed / length : Vector3.Zero;
    }

    private static unsafe RgbaByte? ReadDiffuse(Assimp assimp, Scene* scene)
    {
        if (scene->MNumMeshes == 0)
            return null;

        uint materialIndex = scene->MMeshes[0]->MMaterialIndex;

        if (materialIndex >= scene->MNumMaterials)
            return null;

        MaterialProperty* property = null;

        if (assimp.GetMaterialProperty(scene->MMaterials[materialIndex], Assimp.MatkeyColorDiffuse, 0, 0, &property) != Return.Success)
            return null;

        if (property == null || property->MType != PropertyTypeInfo.Float)
            return null;

        if (property->MDataLength < sizeof(float) * 3)
            return null;

        float* data = (float*)property->MData;
        return new RgbaByte(ToByte(data[0]), ToByte(data[1]), ToByte(data[2]), 255);
    }

    private static unsafe string GetError(Assimp assimp) => Marshal.PtrToStringUTF8((nint)assimp.GetErrorString()) ?? "Unknown error.";

    private static byte ToByte(float value) => (byte)MathF.Round(Math.Clamp(value, 0.0f, 1.0f) * 255.0f);
}
