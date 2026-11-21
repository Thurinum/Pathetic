using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Pathetic.Shaders.Generators;

internal static class ShaderSources
{
    private const string Extension = ".slang";

    /// <summary>The text of every Slang file passed to the compilation as an additional file, in a stable order.</summary>
    public static IncrementalValueProvider<ImmutableArray<string>> Read(IncrementalGeneratorInitializationContext context) =>
        context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            .Select(static (file, cancellation) => (file.Path, Text: file.GetText(cancellation)?.ToString() ?? string.Empty))
            .Collect()
            .Select(static (files, _) => files.OrderBy(file => file.Path, StringComparer.Ordinal).Select(file => file.Text).ToImmutableArray());

    public static IncrementalValueProvider<string> AssemblyName(IncrementalGeneratorInitializationContext context) =>
        context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName ?? "Generated");
}
