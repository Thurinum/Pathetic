using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Pathetic.Slang.Generator;
using Xunit;

namespace Pathetic.Tests;

public sealed class GeneratorTests
{
    private const string ModuleJson = """
        {
          "module": "Primitives",
          "namespace": "Pathetic.Generated.Slang",
          "bindings": [ { "name": "meshTransforms", "kind": "BufferRead" } ],
          "types": [
            {
              "name": "Sphere",
              "size": 20,
              "hasGpuLayout": true,
              "fields": [
                { "name": "Center", "offset": 0, "csType": "global::System.Numerics.Vector3" },
                { "name": "Radius", "offset": 12, "csType": "float" },
                { "name": "MaterialId", "offset": 16, "csType": "uint" }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void EmitsExplicitOffsetsFromJson()
    {
        string code = Generate("Primitives.json", ModuleJson);

        Assert.Contains("namespace Pathetic.Generated.Slang.Primitives;", code);
        Assert.Contains("[StructLayout(LayoutKind.Explicit, Size = 20)]", code);
        Assert.Contains("[FieldOffset(12)] public float Radius;", code);
        Assert.Contains("[FieldOffset(16)] public uint MaterialId;", code);
        Assert.Contains("public static readonly BufferRef meshTransforms = new(\"meshTransforms\");", code);
        Assert.Contains("public const int SizeInBytes = 20;", code);
    }

    [Fact]
    public void EmitsSequentialLayoutWithoutSize()
    {
        const string json = """
            {
              "module": "Camera",
              "namespace": "Pathetic.Generated.Slang",
              "types": [
                {
                  "name": "Ray",
                  "hasGpuLayout": false,
                  "fields": [
                    { "name": "Origin", "offset": 0, "csType": "global::System.Numerics.Vector3" },
                    { "name": "Direction", "offset": 0, "csType": "global::System.Numerics.Vector3" }
                  ]
                }
              ]
            }
            """;

        string code = Generate("Camera.json", json);

        Assert.Contains("[StructLayout(LayoutKind.Sequential)]", code);
        Assert.DoesNotContain("SizeInBytes", code);
        Assert.Contains("public global::System.Numerics.Vector3 Origin;", code);
    }

    [Fact]
    public void ReportsUnparsableJson()
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new SlangGenerator().AsSourceGenerator()],
            additionalTexts: [new InMemoryAdditionalText("Broken.json", "{ not json")]);

        GeneratorDriverRunResult result = driver.RunGenerators(CSharpCompilation.Create("Test")).GetRunResult();

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "PATSLANG001" && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void EscapesKeywordIdentifiers()
    {
        const string json = """
            {
              "module": "Circles",
              "namespace": "Pathetic.Generated.Slang",
              "bindings": [ { "name": "params", "kind": "UniformBuffer" } ]
            }
            """;

        string code = Generate("Circles.json", json);

        Assert.Contains("public static readonly UniformBufferRef @params = new(\"params\");", code);
    }

    private static string Generate(string path, string json)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new SlangGenerator().AsSourceGenerator()],
            additionalTexts: [new InMemoryAdditionalText(path, json)]);

        GeneratorDriverRunResult result = driver.RunGenerators(CSharpCompilation.Create("Test")).GetRunResult();

        Assert.Empty(result.Diagnostics);

        StringBuilder code = new();

        foreach (SyntaxTree tree in result.GeneratedTrees)
            code.AppendLine(tree.ToString());

        return code.ToString();
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(text, Encoding.UTF8);
    }
}
